using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.KeyPair.DTOs
{
    /// <summary>Request to create a new key pair — AWS generates both keys and returns the private key once.</summary>
    public class CreateKeyPairRequest
    {
        /// <summary>
        /// Name for the key pair. Must be unique within the account/region.
        /// Allowed: letters, digits, spaces, underscores, hyphens, parentheses.
        /// </summary>
        [Required, MaxLength(256)]
        public string KeyName { get; set; } = string.Empty;

        /// <summary>Key algorithm. Accepted values: rsa, ed25519. Defaults to rsa.</summary>
        [MaxLength(20)]
        public string KeyType { get; set; } = "rsa";

        /// <summary>Optional tags to apply to the key pair.</summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>Request to import an existing public key into AWS.</summary>
    public class ImportKeyPairRequest
    {
        /// <summary>Name for the key pair in AWS.</summary>
        [Required, MaxLength(256)]
        public string KeyName { get; set; } = string.Empty;

        /// <summary>
        /// The base64-encoded public key material.
        /// Supported formats: OpenSSH, SSH2, PEM.
        /// Example value: "ssh-rsa AAAAB3Nz..."
        /// </summary>
        [Required]
        public string PublicKeyMaterial { get; set; } = string.Empty;

        /// <summary>Optional tags to apply to the key pair.</summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>Request to update key pair tags.</summary>
    public class UpdateKeyPairRequest
    {
        /// <summary>Tags to set on the key pair. Replaces/adds only the supplied keys.</summary>
        [Required]
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}
