using System.Text.Json;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Entities;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Factory;
using IWX_CloudZen.CloudServices.KeyPair.Entities;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Services
{
    public class Ec2InstanceConnectService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public Ec2InstanceConnectService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ---- Endpoint Mappers ----

        private static Ec2InstanceConnectEndpointResponse MapEndpoint(Ec2InstanceConnectEndpointRecord r) => new()
        {
            Id = r.Id,
            EndpointId = r.EndpointId,
            SubnetId = r.SubnetId,
            VpcId = r.VpcId,
            State = r.State,
            DnsName = r.DnsName,
            NetworkInterfaceId = r.NetworkInterfaceId,
            AvailabilityZone = r.AvailabilityZone,
            FipsDnsName = r.FipsDnsName,
            PreserveClientIp = r.PreserveClientIp,
            SecurityGroupIds = DeserializeSecurityGroupIds(r.SecurityGroupIdsJson),
            Tags = DeserializeTags(r.TagsJson),
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static Ec2InstanceConnectSessionResponse MapSession(Ec2InstanceConnectSessionRecord r) => new()
        {
            Id = r.Id,
            InstanceId = r.InstanceId,
            InstanceOsUser = r.InstanceOsUser,
            AvailabilityZone = r.AvailabilityZone,
            SessionType = r.SessionType,
            Status = r.Status,
            RequestId = r.RequestId,
            ErrorMessage = r.ErrorMessage,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt
        };

        // ---- JSON Serialization Helpers ----

        private static string? SerializeSecurityGroupIds(List<string> ids)
            => ids.Count > 0 ? JsonSerializer.Serialize(ids) : null;

        private static List<string> DeserializeSecurityGroupIds(string? json)
            => string.IsNullOrWhiteSpace(json)
                ? new()
                : JsonSerializer.Deserialize<List<string>>(json) ?? new();

        private static string? SerializeTags(Dictionary<string, string> tags)
            => tags.Count > 0 ? JsonSerializer.Serialize(tags) : null;

        private static Dictionary<string, string> DeserializeTags(string? json)
            => string.IsNullOrWhiteSpace(json)
                ? new()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

        // ---- Cloud-to-Record Mappers ----

        private Ec2InstanceConnectEndpointRecord MapFromCloud(
            CloudEc2InstanceConnectEndpointInfo cloud, string provider, int accountId, string user) => new()
        {
            EndpointId = cloud.EndpointId,
            SubnetId = cloud.SubnetId,
            VpcId = cloud.VpcId,
            State = cloud.State,
            DnsName = cloud.DnsName,
            NetworkInterfaceId = cloud.NetworkInterfaceId,
            AvailabilityZone = cloud.AvailabilityZone,
            FipsDnsName = cloud.FipsDnsName,
            PreserveClientIp = cloud.PreserveClientIp,
            SecurityGroupIdsJson = SerializeSecurityGroupIds(cloud.SecurityGroupIds),
            TagsJson = SerializeTags(cloud.Tags),
            Provider = provider,
            CloudAccountId = accountId,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };

        private static void UpdateRecordFromCloud(Ec2InstanceConnectEndpointRecord record, CloudEc2InstanceConnectEndpointInfo cloud)
        {
            record.SubnetId = cloud.SubnetId;
            record.VpcId = cloud.VpcId;
            record.State = cloud.State;
            record.DnsName = cloud.DnsName;
            record.NetworkInterfaceId = cloud.NetworkInterfaceId;
            record.AvailabilityZone = cloud.AvailabilityZone;
            record.FipsDnsName = cloud.FipsDnsName;
            record.PreserveClientIp = cloud.PreserveClientIp;
            record.SecurityGroupIdsJson = SerializeSecurityGroupIds(cloud.SecurityGroupIds);
            record.TagsJson = SerializeTags(cloud.Tags);
            record.UpdatedAt = DateTime.UtcNow;
        }

        private static bool HasEndpointChanges(Ec2InstanceConnectEndpointRecord record, CloudEc2InstanceConnectEndpointInfo cloud)
        {
            return record.State != cloud.State
                || record.DnsName != cloud.DnsName
                || record.NetworkInterfaceId != cloud.NetworkInterfaceId
                || record.AvailabilityZone != cloud.AvailabilityZone
                || record.FipsDnsName != cloud.FipsDnsName
                || record.PreserveClientIp != cloud.PreserveClientIp
                || record.SubnetId != cloud.SubnetId
                || record.VpcId != cloud.VpcId;
        }

        // ==============================
        // ENDPOINT CRUD OPERATIONS
        // ==============================

        public async Task<Ec2InstanceConnectEndpointListResponse> ListEndpoints(string user, int accountId)
        {
            var records = await _db.Ec2InstanceConnectEndpointRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            return new Ec2InstanceConnectEndpointListResponse
            {
                Endpoints = records.Select(MapEndpoint).ToList()
            };
        }

        public async Task<Ec2InstanceConnectEndpointResponse> GetEndpoint(string user, int accountId, int endpointDbId)
        {
            var record = await _db.Ec2InstanceConnectEndpointRecords
                .FirstOrDefaultAsync(x => x.Id == endpointDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 Instance Connect Endpoint not found.");

            return MapEndpoint(record);
        }

        public async Task<Ec2InstanceConnectEndpointResponse> CreateEndpoint(
            string user, int accountId, CreateEc2InstanceConnectEndpointRequest request)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.CreateEndpoint(
                account,
                request.SubnetId,
                request.SecurityGroupIds,
                request.PreserveClientIp,
                request.Tags);

            var record = MapFromCloud(info, account.Provider!, accountId, user);
            _db.Ec2InstanceConnectEndpointRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapEndpoint(record);
        }

        public async Task DeleteEndpoint(string user, int accountId, int endpointDbId)
        {
            var record = await _db.Ec2InstanceConnectEndpointRecords
                .FirstOrDefaultAsync(x => x.Id == endpointDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 Instance Connect Endpoint not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteEndpoint(account, record.EndpointId);

            _db.Ec2InstanceConnectEndpointRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ==============================
        // SEND SSH PUBLIC KEY
        // ==============================

        public async Task<SendSshPublicKeyResponse> SendSshPublicKey(
            string user, int accountId, SendSshPublicKeyRequest request)
        {
            // Resolve the public key from KeyPair stored in DB
            var keyPairRecord = await _db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.Id == request.KeyPairDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Key pair not found.");

            if (string.IsNullOrWhiteSpace(keyPairRecord.PublicKeyMaterial))
                throw new InvalidOperationException(
                    "Public key material is not available for this key pair. " +
                    "Please sync your key pairs or re-create the key pair to populate the public key.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            SendSshPublicKeyResponse result;
            string errorMessage = string.Empty;
            string status;

            try
            {
                result = await provider.SendSshPublicKey(
                    account,
                    request.InstanceId,
                    request.InstanceOsUser,
                    keyPairRecord.PublicKeyMaterial,
                    request.AvailabilityZone);

                status = result.Success ? "Success" : "Failed";
            }
            catch (Exception ex)
            {
                result = new SendSshPublicKeyResponse
                {
                    Success = false,
                    RequestId = string.Empty,
                    InstanceId = request.InstanceId,
                    OsUser = request.InstanceOsUser
                };
                status = "Failed";
                errorMessage = ex.Message;
            }

            // Record the session in the database
            var session = new Ec2InstanceConnectSessionRecord
            {
                InstanceId = request.InstanceId,
                InstanceOsUser = request.InstanceOsUser,
                AvailabilityZone = request.AvailabilityZone ?? string.Empty,
                SessionType = "SSH",
                Status = status,
                RequestId = result.RequestId,
                ErrorMessage = errorMessage,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Ec2InstanceConnectSessionRecords.Add(session);
            await _db.SaveChangesAsync();

            if (!result.Success && !string.IsNullOrEmpty(errorMessage))
                throw new InvalidOperationException($"Failed to send SSH public key: {errorMessage}");

            return result;
        }

        // ==============================
        // SEND SERIAL CONSOLE SSH PUBLIC KEY
        // ==============================

        public async Task<SendSshPublicKeyResponse> SendSerialConsoleSshPublicKey(
            string user, int accountId, SendSerialConsoleSshPublicKeyRequest request)
        {
            // Resolve the public key from KeyPair stored in DB
            var keyPairRecord = await _db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.Id == request.KeyPairDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Key pair not found.");

            if (string.IsNullOrWhiteSpace(keyPairRecord.PublicKeyMaterial))
                throw new InvalidOperationException(
                    "Public key material is not available for this key pair. " +
                    "Please sync your key pairs or re-create the key pair to populate the public key.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            SendSshPublicKeyResponse result;
            string errorMessage = string.Empty;
            string status;

            try
            {
                result = await provider.SendSerialConsoleSshPublicKey(
                    account,
                    request.InstanceId,
                    keyPairRecord.PublicKeyMaterial,
                    request.SerialPort);

                status = result.Success ? "Success" : "Failed";
            }
            catch (Exception ex)
            {
                result = new SendSshPublicKeyResponse
                {
                    Success = false,
                    RequestId = string.Empty,
                    InstanceId = request.InstanceId,
                    OsUser = "serial-console"
                };
                status = "Failed";
                errorMessage = ex.Message;
            }

            // Record the session in the database
            var session = new Ec2InstanceConnectSessionRecord
            {
                InstanceId = request.InstanceId,
                InstanceOsUser = "serial-console",
                AvailabilityZone = string.Empty,
                SessionType = "SerialConsole",
                Status = status,
                RequestId = result.RequestId,
                ErrorMessage = errorMessage,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Ec2InstanceConnectSessionRecords.Add(session);
            await _db.SaveChangesAsync();

            if (!result.Success && !string.IsNullOrEmpty(errorMessage))
                throw new InvalidOperationException($"Failed to send serial console SSH public key: {errorMessage}");

            return result;
        }

        // ==============================
        // SESSION HISTORY
        // ==============================

        public async Task<Ec2InstanceConnectSessionListResponse> ListSessions(string user, int accountId)
        {
            var records = await _db.Ec2InstanceConnectSessionRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new Ec2InstanceConnectSessionListResponse
            {
                Sessions = records.Select(MapSession).ToList()
            };
        }

        public async Task<Ec2InstanceConnectSessionListResponse> ListSessionsByInstance(
            string user, int accountId, string instanceId)
        {
            var records = await _db.Ec2InstanceConnectSessionRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user && x.InstanceId == instanceId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new Ec2InstanceConnectSessionListResponse
            {
                Sessions = records.Select(MapSession).ToList()
            };
        }

        public async Task DeleteSession(string user, int accountId, int sessionDbId)
        {
            var record = await _db.Ec2InstanceConnectSessionRecords
                .FirstOrDefaultAsync(x => x.Id == sessionDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 Instance Connect session not found.");

            _db.Ec2InstanceConnectSessionRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        public async Task ClearSessionHistory(string user, int accountId)
        {
            var records = await _db.Ec2InstanceConnectSessionRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            _db.Ec2InstanceConnectSessionRecords.RemoveRange(records);
            await _db.SaveChangesAsync();
        }

        // ==============================
        // SYNC ENDPOINTS
        // ==============================

        public async Task<SyncEc2InstanceConnectEndpointsResult> SyncEndpoints(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudEndpoints = await provider.FetchAllEndpoints(account);

            var dbRecords = await _db.Ec2InstanceConnectEndpointRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudEndpoints)
            {
                var existing = dbRecords.FirstOrDefault(r => r.EndpointId == cloud.EndpointId);
                if (existing is null)
                {
                    _db.Ec2InstanceConnectEndpointRecords.Add(MapFromCloud(cloud, account.Provider!, accountId, user));
                    added++;
                }
                else if (HasEndpointChanges(existing, cloud))
                {
                    UpdateRecordFromCloud(existing, cloud);
                    updated++;
                }
            }

            var cloudIds = cloudEndpoints.Select(e => e.EndpointId).ToHashSet();
            var toRemove = dbRecords.Where(r => !cloudIds.Contains(r.EndpointId)).ToList();
            _db.Ec2InstanceConnectEndpointRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.Ec2InstanceConnectEndpointRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            return new SyncEc2InstanceConnectEndpointsResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Endpoints = finalRecords.Select(MapEndpoint).ToList()
            };
        }
    }
}
