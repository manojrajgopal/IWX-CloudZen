using LibGit2Sharp;

namespace IWX_CloudZen.CloudDeployments.Pipeline
{
    public class GitCloneService
    {
        public string Clone(string repo, string path)
        {
            Repository.Clone(repo, path);

            return path;
        }
    }
}
