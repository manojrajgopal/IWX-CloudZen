namespace IWX_CloudZen.CloudDeployments.Tracking
{
    public class DeploymentTracker
    {
        public async Task Track()
        {
            while(true)
            {
                await Task.Delay(10000);
            }
        }
    }
}
