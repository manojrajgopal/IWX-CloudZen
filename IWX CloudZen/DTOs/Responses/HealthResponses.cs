namespace IWX_CloudZen.DTOs.Responses
{
    public class ApiStatusResponse
    {
        public int StatusCode { get; set; }
        public string Service { get; set; }
    }

    public class HealthResponses
    {
        public int StatusCode { get; set; }
        public string Status {  get; set; }
        public string Service { get; set; }
        public DateTime Time { get; set; }
    }
}
