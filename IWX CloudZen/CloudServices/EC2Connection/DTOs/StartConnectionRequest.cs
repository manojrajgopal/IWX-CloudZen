namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>
    /// Request to start a connection to an EC2 instance.
    /// All data (IP, key, user) is resolved from the database — no manual input required.
    /// </summary>
    public record StartConnectionRequest(
        /// <summary>Database ID of the EC2 instance (from Ec2InstanceRecords).</summary>
        int InstanceDbId,

        /// <summary>
        /// OS user to connect as (e.g. "ec2-user", "ubuntu", "admin").
        /// If omitted, the service will attempt to infer from the instance platform.
        /// Only used for SSH connections.
        /// </summary>
        string? OsUser = null,

        /// <summary>
        /// Connection method: "SSM" (default) or "SSH".
        /// SSM uses AWS Systems Manager — works through the AWS API, no port 22 needed.
        /// SSH uses direct SSH — requires port 22 open and key pair with stored private key.
        /// </summary>
        string ConnectionMethod = "SSM",

        /// <summary>
        /// Optional PEM private key content for SSH connections.
        /// If provided, this takes precedence over the private key stored in the database.
        /// Useful when the key pair was created outside this application (e.g. AWS Console)
        /// and the private key is not stored in the database.
        /// </summary>
        string? PrivateKeyContent = null
    );
}
