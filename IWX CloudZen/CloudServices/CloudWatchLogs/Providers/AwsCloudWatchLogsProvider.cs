using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Interfaces;
using AwsCreateLogGroupRequest = Amazon.CloudWatchLogs.Model.CreateLogGroupRequest;
using AwsCreateLogStreamRequest = Amazon.CloudWatchLogs.Model.CreateLogStreamRequest;
using AwsFilterLogEventsRequest = Amazon.CloudWatchLogs.Model.FilterLogEventsRequest;
using AwsPutLogEventsRequest = Amazon.CloudWatchLogs.Model.PutLogEventsRequest;

namespace IWX_CloudZen.CloudServices.CloudWatchLogs.Providers
{
    public class AwsCloudWatchLogsProvider : ICloudWatchLogsProvider
    {
        private static AmazonCloudWatchLogsClient GetClient(CloudConnectionSecrets account)
        {
            return new AmazonCloudWatchLogsClient(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));
        }

        // ---- Helpers ----

        private static long ToEpochMs(DateTime dt)
        {
            return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        private static CloudLogGroupInfo MapLogGroup(LogGroup lg) => new()
        {
            LogGroupName = lg.LogGroupName,
            Arn = lg.Arn,
            RetentionInDays = lg.RetentionInDays is > 0 ? lg.RetentionInDays : null,
            StoredBytes = lg.StoredBytes ?? 0,
            MetricFilterCount = lg.MetricFilterCount ?? 0,
            KmsKeyId = lg.KmsKeyId,
            DataProtectionStatus = lg.DataProtectionStatus?.Value,
            LogGroupClass = lg.LogGroupClass?.Value,
            CreationTimeUtc = lg.CreationTime
        };

#pragma warning disable CS0618 // LogStream.StoredBytes is obsolete
        private static CloudLogStreamInfo MapLogStream(LogStream ls, string logGroupName) => new()
        {
            LogStreamName = ls.LogStreamName,
            Arn = ls.Arn,
            LogGroupName = logGroupName,
            FirstEventTimestamp = ls.FirstEventTimestamp,
            LastEventTimestamp = ls.LastEventTimestamp,
            LastIngestionTime = ls.LastIngestionTime,
            StoredBytes = ls.StoredBytes ?? 0
        };
#pragma warning restore CS0618

        // ---- Log Groups ----

        public async Task<List<CloudLogGroupInfo>> FetchAllLogGroups(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var result = new List<CloudLogGroupInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeLogGroupsAsync(new DescribeLogGroupsRequest
                {
                    NextToken = nextToken,
                    Limit = 50
                });

                foreach (var lg in response.LogGroups)
                {
                    result.Add(MapLogGroup(lg));
                }

                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            return result;
        }

        public async Task<CloudLogGroupInfo> CreateLogGroup(
            CloudConnectionSecrets account,
            string logGroupName,
            int? retentionInDays,
            string? kmsKeyId,
            string? logGroupClass)
        {
            var client = GetClient(account);

            var createRequest = new AwsCreateLogGroupRequest
            {
                LogGroupName = logGroupName
            };

            if (!string.IsNullOrWhiteSpace(kmsKeyId))
                createRequest.KmsKeyId = kmsKeyId;

            if (!string.IsNullOrWhiteSpace(logGroupClass))
                createRequest.LogGroupClass = new LogGroupClass(logGroupClass);

            await client.CreateLogGroupAsync(createRequest);

            // Set retention policy if specified
            if (retentionInDays.HasValue && retentionInDays.Value > 0)
            {
                await client.PutRetentionPolicyAsync(new PutRetentionPolicyRequest
                {
                    LogGroupName = logGroupName,
                    RetentionInDays = retentionInDays.Value
                });
            }

            // Fetch the created log group to return full info
            var describeResponse = await client.DescribeLogGroupsAsync(new DescribeLogGroupsRequest
            {
                LogGroupNamePrefix = logGroupName,
                Limit = 1
            });

            var created = describeResponse.LogGroups.FirstOrDefault(g => g.LogGroupName == logGroupName)
                ?? throw new System.InvalidOperationException($"Log group '{logGroupName}' was created but could not be retrieved.");

            return MapLogGroup(created);
        }

        public async Task<CloudLogGroupInfo> UpdateLogGroup(
            CloudConnectionSecrets account,
            string logGroupName,
            int? retentionInDays,
            string? kmsKeyId)
        {
            var client = GetClient(account);

            // Update retention policy
            if (retentionInDays.HasValue)
            {
                if (retentionInDays.Value > 0)
                {
                    await client.PutRetentionPolicyAsync(new PutRetentionPolicyRequest
                    {
                        LogGroupName = logGroupName,
                        RetentionInDays = retentionInDays.Value
                    });
                }
                else
                {
                    // Setting to 0 or negative means "never expire"
                    await client.DeleteRetentionPolicyAsync(new DeleteRetentionPolicyRequest
                    {
                        LogGroupName = logGroupName
                    });
                }
            }

            // Update KMS key association
            if (kmsKeyId is not null)
            {
                if (!string.IsNullOrWhiteSpace(kmsKeyId))
                {
                    await client.AssociateKmsKeyAsync(new AssociateKmsKeyRequest
                    {
                        LogGroupName = logGroupName,
                        KmsKeyId = kmsKeyId
                    });
                }
                else
                {
                    await client.DisassociateKmsKeyAsync(new DisassociateKmsKeyRequest
                    {
                        LogGroupName = logGroupName
                    });
                }
            }

            // Fetch updated state
            var describeResponse = await client.DescribeLogGroupsAsync(new DescribeLogGroupsRequest
            {
                LogGroupNamePrefix = logGroupName,
                Limit = 5
            });

            var updated = describeResponse.LogGroups.FirstOrDefault(g => g.LogGroupName == logGroupName)
                ?? throw new KeyNotFoundException($"Log group '{logGroupName}' not found in AWS.");

            return MapLogGroup(updated);
        }

        public async Task DeleteLogGroup(CloudConnectionSecrets account, string logGroupName)
        {
            var client = GetClient(account);

            await client.DeleteLogGroupAsync(new DeleteLogGroupRequest
            {
                LogGroupName = logGroupName
            });
        }

        // ---- Log Streams ----

        public async Task<List<CloudLogStreamInfo>> FetchLogStreams(CloudConnectionSecrets account, string logGroupName)
        {
            var client = GetClient(account);
            var result = new List<CloudLogStreamInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
                {
                    LogGroupName = logGroupName,
                    OrderBy = OrderBy.LastEventTime,
                    Descending = true,
                    NextToken = nextToken,
                    Limit = 50
                });

                foreach (var ls in response.LogStreams)
                {
                    result.Add(MapLogStream(ls, logGroupName));
                }

                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            return result;
        }

        public async Task<CloudLogStreamInfo> CreateLogStream(
            CloudConnectionSecrets account,
            string logGroupName,
            string logStreamName)
        {
            var client = GetClient(account);

            await client.CreateLogStreamAsync(new AwsCreateLogStreamRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName
            });

            // Fetch the created stream
            var response = await client.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
            {
                LogGroupName = logGroupName,
                LogStreamNamePrefix = logStreamName,
                Limit = 1
            });

            var created = response.LogStreams.FirstOrDefault(s => s.LogStreamName == logStreamName)
                ?? throw new System.InvalidOperationException($"Log stream '{logStreamName}' was created but could not be retrieved.");

            return MapLogStream(created, logGroupName);
        }

        public async Task DeleteLogStream(CloudConnectionSecrets account, string logGroupName, string logStreamName)
        {
            var client = GetClient(account);

            await client.DeleteLogStreamAsync(new DeleteLogStreamRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName
            });
        }

        // ---- Log Events ----

        public async Task<LogEventsListResponse> GetLogEvents(
            CloudConnectionSecrets account,
            string logGroupName,
            string logStreamName,
            int limit,
            string? nextToken)
        {
            var client = GetClient(account);

            var request = new GetLogEventsRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName,
                Limit = Math.Clamp(limit, 1, 10000),
                StartFromHead = true
            };

            if (!string.IsNullOrEmpty(nextToken))
                request.NextToken = nextToken;

            var response = await client.GetLogEventsAsync(request);

            return new LogEventsListResponse
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName,
                Events = response.Events.Select(e => new LogEventResponse
                {
                    LogStreamName = logStreamName,
                    LogGroupName = logGroupName,
                    Timestamp = e.Timestamp ?? DateTime.UtcNow,
                    Message = e.Message,
                    IngestionTime = e.IngestionTime
                }).ToList(),
                NextForwardToken = response.NextForwardToken,
                NextBackwardToken = response.NextBackwardToken
            };
        }

        public async Task PutLogEvents(
            CloudConnectionSecrets account,
            string logGroupName,
            string logStreamName,
            List<LogEventItem> events)
        {
            var client = GetClient(account);

            var inputLogEvents = events.Select(e => new InputLogEvent
            {
                Message = e.Message,
                Timestamp = e.Timestamp ?? DateTime.UtcNow
            })
            .OrderBy(e => e.Timestamp)
            .ToList();

            await client.PutLogEventsAsync(new AwsPutLogEventsRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName,
                LogEvents = inputLogEvents
            });
        }

        public async Task<LogEventsListResponse> FilterLogEvents(
            CloudConnectionSecrets account,
            string logGroupName,
            DTOs.FilterLogEventsRequest filter)
        {
            var client = GetClient(account);

            var request = new AwsFilterLogEventsRequest
            {
                LogGroupName = logGroupName,
                Limit = Math.Clamp(filter.Limit, 1, 10000)
            };

            if (!string.IsNullOrWhiteSpace(filter.LogStreamName))
                request.LogStreamNames = [filter.LogStreamName];

            if (!string.IsNullOrWhiteSpace(filter.FilterPattern))
                request.FilterPattern = filter.FilterPattern;

            if (filter.StartTime.HasValue)
                request.StartTime = ToEpochMs(filter.StartTime.Value);

            if (filter.EndTime.HasValue)
                request.EndTime = ToEpochMs(filter.EndTime.Value);

            var response = await client.FilterLogEventsAsync(request);

            return new LogEventsListResponse
            {
                LogGroupName = logGroupName,
                Events = response.Events.Select(e => new LogEventResponse
                {
                    LogStreamName = e.LogStreamName,
                    LogGroupName = logGroupName,
                    Timestamp = e.Timestamp.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(e.Timestamp.Value).UtcDateTime
                        : DateTime.UtcNow,
                    Message = e.Message,
                    IngestionTime = e.IngestionTime.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(e.IngestionTime.Value).UtcDateTime
                        : null
                }).ToList()
            };
        }
    }
}
