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
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public static async void GetLatestRelease(ZombieSettings settings)
        {
            if (string.IsNullOrEmpty(settings?.AccessToken) || string.IsNullOrEmpty(settings.Address))
            {
                _logger.Error("Connection failed!");
                return;
            }

            var segments = GitHubUtils.ParseUrl(settings.Address);
            var client = new GitHubClient(new ProductHeaderValue("Zombie"));
            var tokenAuth = new Credentials(settings.AccessToken);
            client.Credentials = tokenAuth;

            var release = await client.Repository.Release.GetLatest(segments["owner"], segments["repo"]);
            var currentVersion = Properties.Settings.Default["CurrentVersion"].ToString();
            if (!release.Assets.Any() || new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
            {
                PublishGuiUpdate(Program.Settings, Status.UpToDate, "Your release is up to date!");
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
            foreach (var loc in newSettings.DestinationAssets)
            {
                foreach (var asset in loc.Assets)
                {
                    if (asset.IsArchive())
                    {
                        if (LockAllContents(settings, asset, FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), out var zippedStreams))
                        {
                            fileStreams = fileStreams.Concat(zippedStreams).GroupBy(x => x.Key)
                                .ToDictionary(x => x.Key, x => x.First().Value);
                            continue;
                        }

                        _logger.Error("Could not get access to all ZIP contents!");
                        return;
                    }

                    // (Konrad) Make sure that destination folder exists.
                    var dirPath = FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath);
                    if (!Directory.Exists(dirPath)) FileUtils.CreateDirectory(dirPath);

                    try
                    {
                        var to = Path.Combine(dirPath, asset.Name);
                        var fs = new FileStream(to, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                            FileShare.None);
                        fileStreams.Add(to, fs);
                    }
                    catch (Exception e)
                    {
                        _logger.Fatal(e.Message);
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
                            if(DeleteZipContents(asset, FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), fileStreams)) continue;

                            _logger.Error("Could not override existing ZIP contents!");
                            return;
                        }

                        var to = Path.Combine(FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), asset.Name);

                        // make sure that file is not locked
                        var stream = fileStreams[to];
                        stream?.Close();

                        if (FileUtils.DeleteFile(to)) continue;

                        _logger.Error("Could not delete existing file!");
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
                            if (ExtractToDirectory(asset, FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), fileStreams)) continue;

                            _logger.Error("Could not override existing ZIP contents!");
                            return;
                        }

                        var from = Path.Combine(dir, asset.Name);
                        var to = Path.Combine(FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), asset.Name);

                        // make sure that file is not locked
                        var stream = fileStreams[to];
                        stream?.Close();

                        // (Konrad) Make sure that directory exists.
                        if (!Directory.Exists(Path.GetDirectoryName(to)))
                            FileUtils.CreateDirectory(Path.GetDirectoryName(to));

                        if (FileUtils.Copy(from, to)) continue;

                        _logger.Error("Could not override existing file!");
                        return;
                    }
                }
            }

            // (Konrad) Remove temporary assets
            if (!FileUtils.DeleteDirectory(dir))
            {
                _logger.Error("Could not remove temporary download assets!");
                return;
            }

            Properties.Settings.Default.CurrentVersion = release.TagName;
            Properties.Settings.Default.Save();

            newSettings.LatestRelease = new ReleaseObject(release);
            if (!newSettings.StoreSettings)
            {
                newSettings.AccessToken = Program.Settings.AccessToken;
                newSettings.SettingsLocation = Program.Settings.SettingsLocation;
            }

            // (Konrad) Update Settings and publish to any GUI Clients
            // Note: Since we are not overriding Remotely stored settings this scenario is possible:
            // - Windows gets shut down. Next time it powers up, settings will be pulled from Remote
            // - Remote settings are not updated by ZombieService here, so the GUI would reflect them, again.
            Program.Settings = newSettings;

            PublishGuiUpdate(Program.Settings, Status.Succeeded, "Successfully updated to version: " + release.TagName);
        }

        #region Utilities

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        private static void PublishGuiUpdate(ZombieSettings settings, Status status, string message)
        {
            _logger.Info("GUI Update: \nStatus: " + status + "\nMessage: " + message);
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
        /// <param name="streams"></param>
        /// <returns></returns>
        private static bool LockAllContents(ZombieSettings settings, AssetObject asset, string destinationDir, out Dictionary<string, FileStream> streams)
        {
            streams = new Dictionary<string, FileStream>();
            var dir = FileUtils.GetZombieDownloadsDirectory();
            var filePath = Path.Combine(dir, asset.Name);

            if (!GitHubUtils.DownloadAssets(settings, asset.Url, filePath)) return false;

            try
            {
                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    foreach (var file in zip.Entries)
                    {
                        var completeFileName = Path.Combine(destinationDir, file.FullName);
                        if (file.Name == string.Empty || !Directory.Exists(Path.GetDirectoryName(completeFileName)))
                            continue;

                        var fs = new FileStream(completeFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
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

                        _logger.Error("Failed to delete one of the Zipped files!");
                        return false;
                    }

                    // (Konrad) By now all files inside of the folders should be gone. 
                    foreach (var f in folders)
                    {
                        if (FileUtils.DeleteDirectory(f)) continue;

                        _logger.Error("Failed to delete one of the Zipped folders!");
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
