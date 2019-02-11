using System;
using System.Linq;
using Octokit;
using Zombie.Utilities;

namespace ZombieUpdater
{
    public class App
    {
        private static void Main(string[] args)
        {
            ExecuteUpdate();
        }

        private static async void ExecuteUpdate()
        {
            var client = new GitHubClient(new ProductHeaderValue("Zombie"));
            var release = await client.Repository.Release.GetLatest("HOKGroup", "Zombie");
            var currentVersion = RegistryUtils.GetVersion(VersionType.Zombie);
            if (!release.Assets.Any() || new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
            {
                return;
            }
        }
    }
}
