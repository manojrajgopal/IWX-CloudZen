namespace IWX_CloudZen.CloudServices.ECS.DTOs
{
    /// <summary>Represents a container port mapping.</summary>
    public class PortMappingDto
    {
        public int ContainerPort { get; set; }
        public int? HostPort { get; set; }

        /// <summary>tcp | udp</summary>
        public string Protocol { get; set; } = "tcp";
    }

    /// <summary>A name/value environment variable pair.</summary>
    public class EnvironmentVariableDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Container log driver configuration.</summary>
    public class LogConfigurationDto
    {
        /// <summary>awslogs | splunk | firelens | json-file | etc.</summary>
        public string LogDriver { get; set; } = "awslogs";
        public Dictionary<string, string> Options { get; set; } = new();
    }

    /// <summary>
    /// Full container definition as used in task definition registration and sync.
    /// </summary>
    public class ContainerDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;

        /// <summary>CPU units to reserve for the container (optional for Fargate).</summary>
        public int? Cpu { get; set; }

        /// <summary>Hard memory limit in MB (optional for Fargate).</summary>
        public int? Memory { get; set; }

        /// <summary>Soft memory limit in MB.</summary>
        public int? MemoryReservation { get; set; }

        public bool Essential { get; set; } = true;
        public List<PortMappingDto> PortMappings { get; set; } = new();
        public List<EnvironmentVariableDto> Environment { get; set; } = new();
        public LogConfigurationDto? LogConfiguration { get; set; }
    }

    /// <summary>AWS VPC network configuration for Fargate tasks and services.</summary>
    public class NetworkConfigurationDto
    {
        public List<string> Subnets { get; set; } = new();
        public List<string> SecurityGroups { get; set; } = new();
        public bool AssignPublicIp { get; set; } = true;
    }

    /// <summary>Environment variable overrides for a specific container at run time.</summary>
    public class ContainerEnvironmentOverrideDto
    {
        public string ContainerName { get; set; } = string.Empty;
        public List<EnvironmentVariableDto> Environment { get; set; } = new();
    }
}
