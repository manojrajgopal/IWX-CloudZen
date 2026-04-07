namespace IWX_CloudZen.Permissions.DTOs
{
    public record CheckPermissionRequest(
        List<string> Actions,
        List<string>? ResourceArns = null
    );

    public class CheckPermissionResult
    {
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;

        /// <summary>"allowed" | "explicitDeny" | "implicitDeny"</summary>
        public string EvalDecision { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
    }

    public class PermissionCheckResponse
    {
        public int AllowedCount { get; set; }
        public int DeniedCount { get; set; }
        public List<CheckPermissionResult> Results { get; set; } = new();
    }
}
