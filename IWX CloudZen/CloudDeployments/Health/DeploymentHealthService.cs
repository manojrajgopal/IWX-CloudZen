using System.Net;

namespace IWX_CloudZen.CloudDeployments.Health
{
    public class DeploymentHealthService
    {
        public async Task<string> Check(string url)
        {
            var client = new HttpClient();

            try
            {
                var res = await client.GetAsync(url);

                if (res.StatusCode == HttpStatusCode.OK)
                    return "Healthy";

                return "Unhealthy";
            }
            catch (Exception ex)
            {
                return "Down";
            }
        }
    }
}
