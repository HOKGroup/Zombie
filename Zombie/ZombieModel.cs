#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NLog;
using RestSharp;
using Zombie.Utilities;

#endregion

namespace Zombie
{
    public class ZombieModel
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private const string BaseUrl = "https://api.github.com";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="filePath"></param>
        /// <param name="shouldSerialize"></param>
        public bool StoreSettings(ZombieSettings settings, string filePath, bool shouldSerialize = false)
        {
            try
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    CheckAdditionalContent = true,
                    Formatting = Formatting.Indented
                };

                settings.ShouldSerialize = shouldSerialize;

                var json = JsonConvert.SerializeObject(settings, jsonSettings);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
            }

            return File.Exists(filePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static async Task<IRestResponse<ReleaseObject>> GetLatestRelease(ZombieSettings settings)
        {
            // (Konrad) Apparently it's possible that new Windows updates change the standard 
            // SSL protocol to SSL3. RestSharp uses whatever current one is while GitHub server 
            // is not ready for it yet, so we have to use TLS1.2 explicitly.
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var client = new RestClient(BaseUrl);
            var repoAddress = settings.Address.Replace("https://github.com", "");
            var requestString = "/repos" + repoAddress + "/releases/latest";
            var request = new RestRequest(requestString, Method.GET)
            {
                OnBeforeDeserialization = x => { x.ContentType = "application/json"; }
            };
            request.AddHeader("Content-type", "application/json");
            request.AddHeader("Authorization", "Token " + settings.AccessToken);
            request.RequestFormat = DataFormat.Json;

            var response = await client.ExecuteTaskAsync<ReleaseObject>(request);
            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="process"></param>
        public async void GetLatestRelease(ZombieSettings settings, bool process)
        {
            if (string.IsNullOrEmpty(settings?.AccessToken) || string.IsNullOrEmpty(settings.Address))
            {
                UpdateUI("Connection failed!", ConnectionResult.Failure);
                return;
            }

            var response = await GetLatestRelease(settings);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                UpdateUI("Connection failed!", ConnectionResult.Failure);
                return;
            }

            var release = response.Data;
            var currentVersion = Properties.Settings.Default["CurrentVersion"].ToString();
            if (!release.Assets.Any() || new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
            {
                UpdateUI("Your release is up to date!", ConnectionResult.UpToDate, release);
                return;
            }

            // the manual refresh button doesn't need to trigger the full update
            if (!process)
            {
                Messenger.Default.Send(new ReleaseDownloaded
                {
                    Release = release,
                    Result = ConnectionResult.Success
                });
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
                UpdateUI("Failed to download assets!", ConnectionResult.Failure);
                return;
            }

            // (Konrad) Let's get updated settings, they might be local, or remote.
            // We need latest settings since there might be changes to the target locations.
            ZombieSettings newSettings;
            if (File.Exists(settings.SettingsLocation))
            {
                if (!SettingsUtils.TryGetStoredSettings(settings.SettingsLocation, out newSettings))
                {
                    UpdateUI("Could not get latest local Zombie Settings!", ConnectionResult.Failure);
                    return;
                }
            }
            else
            {
                if (!SettingsUtils.TryGetRemoteSettings(settings.SettingsLocation, out newSettings))
                {
                    UpdateUI("Could not get latest remote Zombie Settings!", ConnectionResult.Failure);
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
                        if (LockAllContents(settings, asset, loc.DirectoryPath, out var zippedStreams))
                        {
                            fileStreams = fileStreams.Concat(zippedStreams).GroupBy(x => x.Key)
                                .ToDictionary(x => x.Key, x => x.First().Value);
                            continue;
                        }

                        UpdateUI("Could not get access to all ZIP contents!", ConnectionResult.Failure);
                        return;
                    }

                    var to = Path.Combine(FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), asset.Name);
                    try
                    {
                        var fs = new FileStream(to, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                            FileShare.None);
                        fileStreams.Add(to, fs);
                    }
                    catch (Exception e)
                    {
                        UpdateUI(e.Message, ConnectionResult.Failure);
                        return;
                    }
                }
            }

            // (Konrad) Move assets to target locations
            foreach (var loc in newSettings.DestinationAssets)
            {
                foreach (var asset in loc.Assets)
                {
                    if (asset.IsArchive())
                    {
                        if (ExtractToDirectory(asset, loc.DirectoryPath, fileStreams)) continue;

                        UpdateUI("Could not override existing ZIP contents!", ConnectionResult.Failure);
                        return;
                    }

                    var from = Path.Combine(dir, asset.Name);
                    var to = Path.Combine(FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), asset.Name);

                    // make sure that file is not locked
                    var stream = fileStreams[to];
                    stream?.Close();

                    if (FileUtils.Copy(@from, @to)) continue;

                    UpdateUI("Could not override existing file!", ConnectionResult.Failure);
                    return;
                }
            }

            // (Konrad) Remove temporary assets
            if (!FileUtils.DeleteDirectory(dir))
            {
                UpdateUI("Could not remove temporary download assets!", ConnectionResult.Failure);
                return;
            }

            // (Konrad) Update UI and save current version
            Properties.Settings.Default.CurrentVersion = release.TagName;
            Properties.Settings.Default.Save();

            _logger.Info("Successfully updated to version: " + release.TagName);
            Messenger.Default.Send(new UpdateStatus { Status = "Successfully updated to version: " + release.TagName });
            Messenger.Default.Send(new ReleaseDownloaded
            {
                Release = release,
                Result = ConnectionResult.Success
            });
        }

        #region Utilities

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
                        var completeFileName = Path.Combine(FilePathUtils.CreateUserSpecificPath(destinationDir), file.FullName);
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
            var dir = FileUtils.GetZombieDownloadsDirectory();
            var filePath = Path.Combine(dir, asset.Name);
            if (!File.Exists(filePath)) return false;

            try
            {
                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    foreach (var file in zip.Entries)
                    {
                        var completeFileName = Path.Combine(FilePathUtils.CreateUserSpecificPath(destinationDir), file.FullName);

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
        /// <param name="message"></param>
        /// <param name="result"></param>
        /// <param name="release"></param>
        private static void UpdateUI(string message, ConnectionResult result, ReleaseObject release = null)
        {
            _logger.Info(message);
            Messenger.Default.Send(new UpdateStatus { Status = message });
            Messenger.Default.Send(new ReleaseDownloaded
            {
                Release = release,
                Settings = null,
                Result = result
            });
        }

        #endregion
    }
}
