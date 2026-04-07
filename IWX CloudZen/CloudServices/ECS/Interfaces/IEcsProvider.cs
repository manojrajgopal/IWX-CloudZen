using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.ECS.DTOs;

namespace IWX_CloudZen.CloudServices.ECS.Interfaces
{
    public interface IEcsProvider
    {
        // ---- Task Definitions ----

        /// <summary>Fetches all active task definition ARNs and their details from the cloud.</summary>
        Task<List<CloudTaskDefinitionInfo>> FetchAllTaskDefinitions(CloudConnectionSecrets account);

        /// <summary>Registers a new task definition (or new revision of an existing family).</summary>
        Task<CloudTaskDefinitionInfo> RegisterTaskDefinition(
            CloudConnectionSecrets account,
            RegisterTaskDefinitionRequest request);

        /// <summary>Deregisters a specific task definition revision, marking it INACTIVE.</summary>
        Task<CloudTaskDefinitionInfo> DeregisterTaskDefinition(
            CloudConnectionSecrets account,
            string taskDefinitionArn);

        /// <summary>Permanently deletes an INACTIVE task definition revision from the cloud.</summary>
        Task DeleteTaskDefinition(CloudConnectionSecrets account, string taskDefinitionArn);

        // ---- Services ----

        /// <summary>Fetches all services in the specified cluster.</summary>
        Task<List<CloudEcsServiceInfo>> FetchAllServices(
            CloudConnectionSecrets account,
            string clusterName);

        /// <summary>Creates a new ECS service in the specified cluster.</summary>
        Task<CloudEcsServiceInfo> CreateService(
            CloudConnectionSecrets account,
            CreateEcsServiceRequest request);

        /// <summary>Updates service configuration (desired count and/or task definition).</summary>
        Task<CloudEcsServiceInfo> UpdateService(
            CloudConnectionSecrets account,
            string clusterName,
            string serviceName,
            int? desiredCount,
            string? taskDefinition);

        /// <summary>Scales the service to zero then deletes it from the cloud.</summary>
        Task DeleteService(
            CloudConnectionSecrets account,
            string clusterName,
            string serviceName);

        // ---- Tasks ----

        /// <summary>Fetches all running and recently stopped tasks in the specified cluster.</summary>
        Task<List<CloudEcsTaskInfo>> FetchAllTasks(
            CloudConnectionSecrets account,
            string clusterName);

        /// <summary>Runs one or more task instances from a task definition.</summary>
        Task<List<CloudEcsTaskInfo>> RunTask(
            CloudConnectionSecrets account,
            RunTaskRequest request);

        /// <summary>Sends a stop signal to a running task.</summary>
        Task StopTask(
            CloudConnectionSecrets account,
            string clusterName,
            string taskArn,
            string? reason);
    }
}
