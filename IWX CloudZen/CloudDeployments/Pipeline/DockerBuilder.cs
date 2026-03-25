using System.Diagnostics;

namespace IWX_CloudZen.CloudDeployments.Pipeline
{
    public class DockerBuilder
    {
        public async Task Build(string path, string image)
        {
            var process = new Process();

            process.StartInfo.FileName = "docker";

            process.StartInfo.Arguments = $"build -t {image} {path}";

            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            await process.WaitForExitAsync();
        }
    }
}
