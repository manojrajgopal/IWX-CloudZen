using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.Permissions.DTOs;

namespace IWX_CloudZen.Permissions.Interfaces
{
    public interface IPermissionsProvider
    {
        /// <summary>
        /// Returns every policy attached to the IAM identity (user + groups),
        /// including full statement breakdown for each policy.
        /// </summary>
        Task<PolicyListResponse> GetAllPolicies(CloudConnectionSecrets account);

        /// <summary>
        /// Returns a lightweight summary: policy counts, groups, and policy names
        /// without full statement detail.
        /// </summary>
        Task<PermissionSummaryResponse> GetSummary(CloudConnectionSecrets account);

        /// <summary>
        /// Simulates whether specified IAM actions are allowed for the identity,
        /// returning per-action decision ("allowed" / "explicitDeny" / "implicitDeny").
        /// </summary>
        Task<PermissionCheckResponse> CheckPermissions(
            CloudConnectionSecrets account,
            List<string> actions,
            List<string>? resourceArns);

        /// <summary>
        /// Attaches an AWS managed or customer-managed policy to the IAM user.
        /// </summary>
        Task AttachPolicy(CloudConnectionSecrets account, string policyArn);

        /// <summary>
        /// Detaches a managed policy from the IAM user.
        /// </summary>
        Task DetachPolicy(CloudConnectionSecrets account, string policyArn);

        /// <summary>
        /// Lists available AWS managed or customer-managed policies,
        /// optionally filtered by name fragment.
        /// scope: "AWS" | "Local" | "All"
        /// </summary>
        Task<AvailablePoliciesListResponse> ListAvailablePolicies(
            CloudConnectionSecrets account,
            string scope,
            string? search);
    }
}
