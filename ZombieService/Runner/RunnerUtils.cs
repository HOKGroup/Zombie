#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using NLog;
using Octokit;
using Zombie.Utilities;
using ZombieUtilities.Client;
using FileMode = System.IO.FileMode;

#endregion

namespace ZombieService.Runner
{
    public static class RunnerUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Method that retrieves the latest release from GitHub.
        /// </summary>
        /// <param name="settings">Zombie Settings to be used to retrieve latest Release.</param>
        public static async void GetLatestRelease(ZombieSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings?.AccessToken) || string.IsNullOrWhiteSpace(settings.Address))
            {
                var a = string.IsNullOrWhiteSpace(settings.Address) ? "Not found" : "Exists";
                _logger.Error($"Connection failed! Address: {a}");
                return;
            }

            var segments = GitHubUtils.ParseUrl(settings.Address);
            var client = new GitHubClient(new ProductHeaderValue("Zombie"));
            var tokenAuth = new Credentials(settings.AccessToken);
            client.Credentials = tokenAuth;

            Release release;
            try
            {
                release = await client.Repository.Release.GetLatest(segments["owner"], segments["repo"]);
                var currentVersion = RegistryUtils.GetZombieVersion();
                if (!release.Assets.Any() || new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
                {
                    PublishGuiUpdate(Program.Settings, Status.UpToDate, "Your release is up to date! Version: " + currentVersion);
                    return;
                }
            }
            catch (Exception e)
            {
                _logger.Fatal("Failed to retrieve Release from GitHub. " + e.Message);
                return;
            }

            var dir = FileUtils.GetZombieDownloadsDirectory();
            var downloaded = 0;
            foreach (var asset in release.Assets)
            {
                var filePath = Path.Combine(dir, asset.Name);
                if (GitHubUtils.DownloadAssets(settings, asset.Url, filePath)) downloaded++;
            }

            if (downloaded != release.Assets.Count)
            {
                _logger.Error("Failed to download assets!");
                return;
            }

            // (Konrad) Let's get updated settings, they might be local, or remote.
            // We need latest settings since there might be changes to the target locations.
            ZombieSettings newSettings;
            if (File.Exists(settings.SettingsLocation))
            {
                if (!SettingsUtils.TryGetStoredSettings(settings.SettingsLocation, out newSettings))
                {
                    _logger.Error("Could not get latest local Zombie Settings!");
                    return;
                }
            }
            else
            {
                if (!SettingsUtils.TryGetRemoteSettings(settings.SettingsLocation, out newSettings))
                {
                    _logger.Error("Could not get latest remote Zombie Settings!");
                    return;
                }
            }

            // (Konrad) Let's make sure that we own the files that we are trying to override
            var fileStreams = new Dictionary<string, FileStream>();
            foreach (var loc in newSettings.DestinationAssets.OrderByDescending(x => (int)x.LocationType))
            {
                foreach (var asset in loc.Assets)
                {
                    if (asset.IsArchive())
                    {
                        // (Konrad) Use old settings
                        if (LockAllContents(loc.LocationType == LocationType.Trash ? settings : newSettings, asset, loc.DirectoryPath, loc.LocationType, out var zippedStreams))
                        {
                            fileStreams = fileStreams.Concat(zippedStreams).GroupBy(x => x.Key)
                                .ToDictionary(x => x.Key, x => x.First().Value);
                            continue;
                        }
                        
                        ReleaseStreams(fileStreams);
                        return;
                    }

                    if (loc.LocationType != LocationType.Trash)
                    {
                        // (Konrad) Make sure that destination folder exists.
                        if (!Directory.Exists(loc.DirectoryPath)) FileUtils.CreateDirectory(loc.DirectoryPath);
                    }

                    var to = Path.Combine(loc.DirectoryPath, asset.Name);
                    if (!File.Exists(to)) continue;

                    try
                    {
                        var fs = new FileStream(to, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        fileStreams.Add(to, fs);
                    }
                    catch (Exception e)
                    {
                        _logger.Fatal(e.Message);
                        ReleaseStreams(fileStreams);
                        return;
                    }
                }
            }

            // (Konrad) Move assets to target locations. 
            // We sort the locations list so that Trash (3) is first.
            // This should make sure that we delete first, then move.
            // Could be important with Zipped contents and overriding.
            foreach (var loc in newSettings.DestinationAssets.OrderByDescending(x => (int)x.LocationType))
            {
                if (loc.LocationType == LocationType.Trash)
                {
                    // (Konrad) Let's remove these files.
                    foreach (var asset in loc.Assets)
                    {
                        if (asset.IsArchive())
                        {
                            if(DeleteZipContents(asset, loc.DirectoryPath, fileStreams)) continue;

                            ReleaseStreams(fileStreams);
                            return;
                        }

                        var to = Path.Combine(loc.DirectoryPath, asset.Name);
                        if (fileStreams.ContainsKey(to))
                        {
                            // make sure that file is not locked
                            var stream = fileStreams[to];
                            stream?.Close();
                        }

                        if (FileUtils.DeleteFile(to)) continue;

                        ReleaseStreams(fileStreams);
                        return;
                    }
                }
                else
                {
                    // (Konrad) Let's copy these files.
                    foreach (var asset in loc.Assets)
                    {
                        if (asset.IsArchive())
                        {
                            if (ExtractToDirectory(asset, loc.DirectoryPath, fileStreams)) continue;

                            ReleaseStreams(fileStreams);
                            return;
                        }

                        var from = Path.Combine(dir, asset.Name);
                        var to = Path.Combine(loc.DirectoryPath, asset.Name);
                        if (fileStreams.ContainsKey(to))
                        {
                            // make sure that file is not locked
                            var stream = fileStreams[to];
                            stream?.Close();
                        }

                        // (Konrad) Make sure that directory exists.
                        if (!Directory.Exists(Path.GetDirectoryName(to)))
                            FileUtils.CreateDirectory(Path.GetDirectoryName(to));

                        if (FileUtils.Copy(from, to)) continue;

                        ReleaseStreams(fileStreams);
                        return;
                    }
                }
            }

            // (Konrad) Remove temporary assets
            if (!FileUtils.DeleteDirectory(dir))
            {
                // (Konrad) Cleanup failed but we can continue.
                _logger.Error("Could not remove temporary download assets!");
            }

            // (Konrad) This is important! 
            ReleaseStreams(fileStreams);

            // (Konrad) We need to store the current version for comparison on next update
            RegistryUtils.SetZombieVersion(release.TagName);

            // (Konrad) Settings need to be updated with the latest one just downloaded
            newSettings.LatestRelease = new ReleaseObject(release);
            if (!newSettings.StoreSettings)
            {
                newSettings.AccessToken = Program.Settings.AccessToken;
                newSettings.SettingsLocation = Program.Settings.SettingsLocation;
            }
            Program.Settings = newSettings;

            // (Konrad) Publish to any open GUIs
            PublishGuiUpdate(newSettings, Status.Succeeded, "Successfully updated to Version: " + release.TagName);
        }

        #region Utilities

        /// <summary>
        /// Closes any streams that we might have had created. This ensures that we don't lock any files for longer than we need to.
        /// </summary>
        /// <param name="streams">Dictionary with file name/stream that has it open.</param>
        private static void ReleaseStreams(Dictionary<string, FileStream> streams)
        {
            foreach (var s in streams.Values)
            {
                s.Close();
            }
        }

        /// <summary>
        /// Utility method that publishes a GUI update to ZombieGUI.
        /// </summary>
        /// <param name="settings">Latest ZombieSettings file that GUI will be updated to.</param>
        /// <param name="status">Message status type.</param>
        /// <param name="message">String message to be displayed in the GUI.</param>
        private static void PublishGuiUpdate(ZombieSettings settings, Status status, string message)
        {
            var msg = "Status: " + status + " Message: " + message;
            if (string.IsNullOrWhiteSpace(Program.RecentLog) || msg != Program.RecentLog)
            {
                _logger.Info(msg);
                Program.RecentLog = msg;
            }

            var update = new GuiUpdate
            {
                Settings = settings,
                Status = status,
                Message = message
            };
            new Thread(() => new ZombieMessenger().Broadcast(update))
            {
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="asset"></param>
        /// <param name="destinationDir"></param>
        /// <param name="type"></param>
        /// <param name="streams"></param>
        /// <returns></returns>
        private static bool LockAllContents(ZombieSettings settings, AssetObject asset, string destinationDir, LocationType type, out Dictionary<string, FileStream> streams)
        {
            streams = new Dictionary<string, FileStream>();
            var dir = FileUtils.GetZombieDownloadsDirectory();
            string filePath;

            if (type == LocationType.Trash)
            {
                filePath = Path.Combine(dir,
                    Path.GetFileNameWithoutExtension(asset.Name) + "_old" + Path.GetExtension(asset.Name));

                if (!File.Exists(filePath))
                {
                    if (!GitHubUtils.DownloadAssets(settings, asset.Url, filePath)) return false;
                }
            }
            else
            {
                filePath = Path.Combine(dir, asset.Name);
            }

            try
            {
                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    foreach (var file in zip.Entries)
                    {
                        var completeFileName = Path.Combine(destinationDir, file.FullName);
                        if (file.Name == string.Empty || 
                            !Directory.Exists(Path.GetDirectoryName(completeFileName)) ||
                            !File.Exists(completeFileName)) continue;

                        var fs = new FileStream(completeFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        streams.Add(completeFileName, fs);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="destinationDir"></param>
        /// <param name="streams"></param>
        /// <returns></returns>
        private static bool ExtractToDirectory(AssetObject asset, string destinationDir, IReadOnlyDictionary<string, FileStream> streams)
        {
            // (Konrad) Make sure that destination folder exists.
            if (!Directory.Exists(destinationDir)) FileUtils.CreateDirectory(destinationDir);

            var dir = FileUtils.GetZombieDownloadsDirectory();
            var filePath = Path.Combine(dir, asset.Name);
            if (!File.Exists(filePath)) return false;

            try
            {
                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    foreach (var file in zip.Entries)
                    {
                        var completeFileName = Path.Combine(destinationDir, file.FullName);

                        if (file.Name == string.Empty) continue; // dir entry

                        // make sure that file is not locked
                        var stream = streams.ContainsKey(completeFileName) ? streams[completeFileName] : null;
                        stream?.Close();

                        if (stream == null)
                        {
                            // we didn't add it to streams which means that directory didn't exist
                            var path = Path.GetDirectoryName(completeFileName);
                            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        }

                        file.ExtractToFile(completeFileName, true);
                        File.SetLastWriteTime(completeFileName, DateTime.Now);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="destinationDir"></param>
        /// <param name="streams"></param>
        /// <returns></returns>
        private static bool DeleteZipContents(AssetObject asset, string destinationDir, IReadOnlyDictionary<string, FileStream> streams)
        {
            var dir = FileUtils.GetZombieDownloadsDirectory();
            var filePath = Path.Combine(dir, asset.Name);
            if (!File.Exists(filePath)) return false;

            try
            {
                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    var folders = new List<string>();
                    foreach (var file in zip.Entries)
                    {
                        var completeFileName = Path.Combine(destinationDir, file.FullName);

                        if (file.Name == string.Empty)
                        {
                            folders.Add(completeFileName);
                        }

                        // make sure that file is not locked
                        var stream = streams.ContainsKey(completeFileName) ? streams[completeFileName] : null;
                        stream?.Close();

                        if (FileUtils.DeleteFile(completeFileName)) continue;
                        return false;
                    }

                    // (Konrad) By now all files inside of the folders should be gone. 
                    foreach (var f in folders)
                    {
                        if (FileUtils.DeleteDirectory(f)) continue;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }
            return true;
        }
        #endregion
    }
}
