#region References

using System;
using System.Collections.Generic;
using System.IO;
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
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        public void DownloadAssets(ZombieSettings settings, string url, string filePath)
        {
            // (Konrad) Apparently it's possible that new Windows updates change the standard 
            // SSL protocol to SSL3. RestSharp uses whatever current one is while GitHub server 
            // is not ready for it yet, so we have to use TLS1.2 explicitly.
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var client = new RestClient(BaseUrl);
            var request = new RestRequest(url, Method.GET)
            {
                OnBeforeDeserialization = x => { x.ContentType = "application/json"; }
            };
            request.AddHeader("Content-type", "application/json");
            request.AddHeader("Authorization", "Token " + settings.AccessToken);
            request.AddHeader("Accept", "application/octet-stream");
            request.RequestFormat = DataFormat.Json;

            // (Konrad) We use 120 because even when it fails there are always some
            // bytes that get returned with the error message.
            var response = client.DownloadData(request);
            if (response.Length > 120)
            {
                try
                {
                    File.WriteAllBytes(filePath, response);
                }
                catch (Exception e)
                {
                    _logger.Fatal(e.Message);
                }
            }
            else
            {
                _logger.Error("Download failed. Less than 120 bytes were downloaded.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public async void RetrieveRelease(ZombieSettings settings)
        {
            if (!string.IsNullOrEmpty(settings?.AccessToken) && 
                !string.IsNullOrEmpty(settings.Address))
            {
                var response = await GetLatestRelease(settings);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var release = response.Data;

                    var currentVersion = settings.LatestRelease?.TagName ?? "0.0.0.0";
                    if (new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
                    {
                        _logger.Info("Your release is up to date!");
                        Messenger.Default.Send(new UpdateStatus { Status = "Your release is up to date!" });
                        return;
                    }

                    if (!release.Assets.Any())
                    {
                        _logger.Info("Connection succeeded!");
                        Messenger.Default.Send(new UpdateStatus { Status = "Connection succeeded!" });
                        return;
                    }

                    Messenger.Default.Send(new ReleaseDownloaded
                    {
                        Release = release,
                        Result = ConnectionResult.Success
                    });
                    return;
                }
            }

            _logger.Info("Connection failed!");
            Messenger.Default.Send(new UpdateStatus { Status = "Connection failed!" });
            Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public async void ProcessLatestRelease(ZombieSettings settings)
        {
            if (!string.IsNullOrEmpty(settings?.AccessToken) &&
                !string.IsNullOrEmpty(settings.Address))
            {
                var response = await GetLatestRelease(settings);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var release = response.Data;

                    var currentVersion = settings.LatestRelease?.TagName ?? "0.0.0.0";
                    if (new Version(release.TagName).CompareTo(new Version(currentVersion)) <= 0)
                    {
                        _logger.Info("Your release is up to date!");
                        Messenger.Default.Send(new UpdateStatus { Status = "Your release is up to date!" });
                        return;
                    }

                    if (!release.Assets.Any())
                    {
                        _logger.Info("No assets found!");
                        Messenger.Default.Send(new UpdateStatus { Status = "No assets found!" });
                        return;
                    }

                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "downloads");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    var downloaded = 0;
                    foreach (var asset in release.Assets)
                    {
                        try
                        {
                            var filePath = Path.Combine(dir, asset.Name);

                            // download
                            DownloadAssets(settings, asset.Url, filePath);

                            // verify
                            if (File.Exists(filePath))
                            {
                                downloaded++;
                                continue;
                            }

                            _logger.Error("Failed to download an asset! " + filePath);
                        }
                        catch (Exception e)
                        {
                            _logger.Fatal(e.Message);
                        }
                    }

                    if (downloaded != release.Assets.Count)
                    {
                        _logger.Info("Downloaded (" + downloaded + ")/(" + release.Assets.Count + ").");
                        Messenger.Default.Send(new UpdateStatus { Status = "Failed to download assets!" });
                        Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
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
                            Messenger.Default.Send(new UpdateStatus { Status = "Could not get latest local Zombie Settings!" });
                            Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
                            return;
                        }
                    }
                    else
                    {
                        if (!SettingsUtils.TryGetRemoteSettings(settings.SettingsLocation, out newSettings))
                        {
                            _logger.Error("Could not get latest remote Zombie Settings!");
                            Messenger.Default.Send(new UpdateStatus { Status = "Could not get latest remote Zombie Settings!" });
                            Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
                            return;
                        }
                    }

                    // (Konrad) Let's make sure that we own the files that we are trying to override
                    var fileStreams = new Dictionary<string, FileStream>();
                    foreach (var loc in newSettings.DestinationAssets)
                    {
                        foreach (var asset in loc.Assets)
                        {
                            var to = Path.Combine(FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), asset.Name);
                            try
                            {
                                var fs = new FileStream(to, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                    FileShare.None);
                                fileStreams.Add(to, fs);
                            }
                            catch (IOException e)
                            {
                                // this one we can terminate because it means that we can't access the file
                                _logger.Error("Could not get access to the destination asset. Terminating.");
                                Messenger.Default.Send(new UpdateStatus { Status = "Could not get access to the destination asset. Terminating..." });
                                Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
                                return;
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e.Message);
                                Messenger.Default.Send(new UpdateStatus { Status = e.Message });
                                Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
                                return;
                            }
                        }
                    }

                    // (Konrad) Move assets to target locations
                    foreach (var loc in newSettings.DestinationAssets)
                    {
                        foreach (var asset in loc.Assets)
                        {
                            var from = Path.Combine(dir, asset.Name);
                            var to = Path.Combine(FilePathUtils.CreateUserSpecificPath(loc.DirectoryPath), asset.Name);

                            // make sure that file is not locked
                            var stream = fileStreams[to];
                            stream?.Close();

                            if (FileUtils.Copy(from, to)) continue;

                            Messenger.Default.Send(new UpdateStatus { Status = "Could not override existing file!" });
                            Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
                            return;
                        }
                    }

                    // (Konrad) Remove temporary assets
                    if (!FileUtils.DeleteDirectory(dir))
                    {
                        Messenger.Default.Send(new UpdateStatus { Status = "Could not remove temporary download assets!" });
                        Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
                        return;
                    }

                    // (Konrad) Update UI
                    _logger.Info("Successfully updated to version: " + release.TagName);
                    Messenger.Default.Send(new UpdateStatus { Status = "Successfully updated to version: " + release.TagName });
                    Messenger.Default.Send(new ReleaseDownloaded
                    {
                        Release = release,
                        Result = ConnectionResult.Success
                    });
                    return;
                }
            }

            _logger.Info("Connection failed!");
            Messenger.Default.Send(new UpdateStatus { Status = "Connection failed!" });
            Messenger.Default.Send(new ReleaseDownloaded { Result = ConnectionResult.Failure });
        }
    }
}
