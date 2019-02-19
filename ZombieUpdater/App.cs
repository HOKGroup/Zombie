#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Octokit;
using Zombie.Utilities;
using ZombieUtilities;

#endregion

namespace ZombieUpdater
{
    public class App
    {
        private static Logger _logger { get; set; }

        private static void Main(string[] args)
        {
            try
            {
                // (Konrad) Setup logger.
                NlogUtils.CreateConfiguration();
                _logger = LogManager.GetCurrentClassLogger();

                var task = GetRelease();
                task.Wait();

                var release = task.Result;
                var currentVersion = RegistryUtils.GetVersion(VersionType.Zombie);
                if (!release.Assets.Any() || new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
                {
                    Cleanup("Could not find a release, or release is up to date.");
                    return;
                }

                var dir = FileUtils.GetZombieDownloadsDirectory();
                var downloaded = 0;
                var msiPath = string.Empty;
                foreach (var asset in release.Assets)
                {
                    if (!asset.Name.EndsWith("msi")) continue;

                    msiPath = Path.Combine(dir, asset.Name);
                    if (GitHubUtils.DownloadAssets(asset.Url, msiPath)) downloaded++;
                }

                if (downloaded != release.Assets.Count)
                {
                    Cleanup("Could not download the release.");
                    return;
                }

                var commandLine = RegistryUtils.GetZombieServiceArguments();
                var a = SplitCommandLine(commandLine).ToList();
                var settings = a.Count >= 2 ? a[1] : string.Empty;
                var token = a.Count >= 3 ? a[2] : string.Empty;
                var log = a.Count >= 4 ? a[3] : string.Empty;

                var arguments =
                    $"/a \"{msiPath}\" /quiet /qn REBOOT=ReallySuppress REINSTALL=ALL SETTINGSPATH=\"{settings}\" ACCESSTOKEN=\"{token}\" WEBSERVICEENDPOINT=\"{log}\" /L*V \"{Path.Combine(dir, "InstallerLog.txt")}\"";
                _logger.Info(arguments);

                var processInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    Arguments = arguments,
                    FileName = "msiexec"
                };
                var process = new Process
                {
                    StartInfo = processInfo
                };
                var result = process.Start();
                _logger.Info("Launched installer: " + (result ? "True" : "False"));
                process.WaitForExit();

                //_logger.Info("Writing command line args for ZombieService: " + commandLine);
                //RegistryUtils.SetServiceCommandLine(commandLine);

                //Cleanup("Successfully updated Zombie!");
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
            }
        }

        #region Utilities

        /// <summary>
        /// Cleans up after the update process. We have to re-launch the Zombie Service.
        /// </summary>
        private static void Cleanup(string message)
        {
            try
            {
                _logger.Info(message);

                var processInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    Arguments = "/C net start ZombieService",
                    FileName = "cmd.exe"
                };
                var process = new Process
                {
                    StartInfo = processInfo
                };
                var result = process.Start();
                _logger.Info("Launched service: " + (result ? "True" : "False"));
                process.WaitForExit();

                _logger.Info("Successfully restarted Zombie Service.");
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
            }
        }

        /// <summary>
        /// Retrieves the latest Release from Zombie's GitHub page.
        /// </summary>
        /// <returns>Release or null if failed.</returns>
        private static async Task<Release> GetRelease()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("ZombieUpdater"));
                var tokenAuth = new Credentials("0460743d5180c249dda25f6276e7c7c3a372de30");
                client.Credentials = tokenAuth;
                return await client.Repository.Release.GetLatest("HOKGroup", "Zombie");
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Parses a command line string into its arguments. Used to parse ZombieService launch string.
        /// </summary>
        /// <param name="commandLine">Command line string.</param>
        /// <returns>An array of command line arguments.</returns>
        private static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            var inQuotes = false;

            return commandLine.Split(c =>
                {
                    if (c == '\"')
                        inQuotes = !inQuotes;

                    return !inQuotes && c == ' ';
                })
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        #endregion
    }
}
