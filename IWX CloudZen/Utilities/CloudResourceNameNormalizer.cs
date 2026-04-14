using System.Text.RegularExpressions;

namespace IWX_CloudZen.Utilities
{
    /// <summary>
    /// Global utility for normalizing cloud resource names to satisfy AWS naming constraints.
    /// Ensures names are valid before sending to AWS APIs, preventing creation errors.
    /// </summary>
    public static class CloudResourceNameNormalizer
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Normalizes a general AWS resource name (ECS clusters, ECS services, key pairs,
        /// security groups, EC2 instances, VPCs, subnets, CloudWatch log groups, etc.).
        /// Rules: letters, digits, hyphens, underscores allowed; 3–255 characters.
        /// If the result is too short, random digits are appended.
        /// </summary>
        public static string NormalizeGeneralName(string name, int minLength = 3, int maxLength = 255)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource name cannot be empty.");

            var normalized = name.Trim();

            // Replace whitespace with hyphen
            normalized = Regex.Replace(normalized, @"\s+", "-");

            // Replace any character that is not a letter, digit, hyphen, or underscore
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9\-_]", "-");

            // Collapse consecutive hyphens/underscores into a single hyphen
            normalized = Regex.Replace(normalized, @"[\-_]{2,}", "-");

            // Trim leading and trailing hyphens or underscores
            normalized = normalized.Trim('-', '_');

            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException($"'{name}' could not be normalized to a valid resource name.");

            // If too short, pad with random digits
            if (normalized.Length < minLength)
            {
                var padding = minLength - normalized.Length;
                normalized += "-" + GenerateRandomDigits(padding);
            }

            // Truncate if too long
            if (normalized.Length > maxLength)
                normalized = normalized[..maxLength].TrimEnd('-', '_');

            return normalized;
        }

        /// <summary>
        /// Normalizes an S3 bucket name.
        /// Rules: lowercase only, a-z/0-9/hyphens, 3–63 characters, no IP-like names.
        /// If the result is too short, random digits are appended.
        /// </summary>
        public static string NormalizeBucketName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bucket name cannot be empty.");

            var normalized = name.Trim().ToLowerInvariant();

            // Replace spaces, underscores, dots with hyphens
            normalized = Regex.Replace(normalized, @"[\s_.]", "-");

            // Remove any character that is not lowercase letter, digit, or hyphen
            normalized = Regex.Replace(normalized, @"[^a-z0-9\-]", "");

            // Collapse consecutive hyphens
            normalized = Regex.Replace(normalized, @"-{2,}", "-");

            // Trim leading/trailing hyphens
            normalized = normalized.Trim('-');

            // If too short, pad with random digits
            if (normalized.Length < 3)
                normalized = (normalized.Length > 0 ? normalized + "-" : "") + GenerateRandomDigits(6);

            if (normalized.Length > 63)
                normalized = normalized[..63].TrimEnd('-');

            // Reject IP-address-like names
            if (Regex.IsMatch(normalized, @"^\d+\-\d+\-\d+\-\d+$"))
                normalized += "-" + GenerateRandomDigits(4);

            return normalized;
        }

        /// <summary>
        /// Normalizes an ECR repository name.
        /// Rules: lowercase, a-z/0-9/dots/hyphens/underscores/slashes, path segments must start and end with alphanumeric.
        /// If the result is too short, random digits are appended.
        /// </summary>
        public static string NormalizeRepositoryName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Repository name cannot be empty.");

            var normalized = name.ToLowerInvariant();

            // Replace whitespace with dash
            normalized = Regex.Replace(normalized, @"\s+", "-");

            // Replace any character not valid in ECR names with dash
            normalized = Regex.Replace(normalized, @"[^a-z0-9._/\-]", "-");

            // Collapse multiple consecutive dashes into one
            normalized = Regex.Replace(normalized, @"-{2,}", "-");

            // Clean up each path segment
            var segments = normalized
                .Split('/')
                .Select(seg => seg.Trim('-', '.', '_'))
                .Where(seg => seg.Length > 0)
                .ToArray();

            if (segments.Length == 0)
                throw new ArgumentException($"'{name}' could not be normalized to a valid ECR repository name.");

            normalized = string.Join("/", segments);

            // Ensure starts and ends with alphanumeric
            normalized = Regex.Replace(normalized, @"^[^a-z0-9]+", "");
            normalized = Regex.Replace(normalized, @"[^a-z0-9]+$", "");

            // If too short, pad with random digits
            if (normalized.Length < 2)
                normalized = (normalized.Length > 0 ? normalized + "-" : "repo-") + GenerateRandomDigits(6);

            return normalized;
        }

        /// <summary>
        /// Normalizes a CloudWatch log group name.
        /// Rules: 1–512 characters; a-zA-Z0-9, '.', '-', '_', '/' allowed.
        /// If the result is too short, random digits are appended.
        /// </summary>
        public static string NormalizeLogGroupName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Log group name cannot be empty.");

            var normalized = name.Trim();

            // Replace whitespace with hyphen
            normalized = Regex.Replace(normalized, @"\s+", "-");

            // Replace any invalid character with hyphen
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9.\-_/]", "-");

            // Collapse consecutive hyphens
            normalized = Regex.Replace(normalized, @"-{2,}", "-");

            // Trim leading/trailing hyphens
            normalized = normalized.Trim('-', '_');

            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException($"'{name}' could not be normalized to a valid log group name.");

            // If too short, pad with random digits
            if (normalized.Length < 1)
                normalized = "loggroup-" + GenerateRandomDigits(6);

            if (normalized.Length > 512)
                normalized = normalized[..512].TrimEnd('-', '_');

            return normalized;
        }

        /// <summary>
        /// Generates a string of random digits of the specified length.
        /// </summary>
        private static string GenerateRandomDigits(int count)
        {
            var digits = new char[count];
            for (int i = 0; i < count; i++)
                digits[i] = (char)('0' + _random.Next(0, 10));
            return new string(digits);
        }
    }
}
