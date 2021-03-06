﻿#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.UI.Notifications;
using DesktopNotifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NLog;
using Octokit;
using Zombie.Utilities;
using Formatting = Newtonsoft.Json.Formatting;

#endregion

namespace Zombie.Controls
{
    public class ZombieModel : INotifyPropertyChanged
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged("Settings"); }
        }

        #endregion

        public ZombieModel(ZombieSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Displays toast notification using the MS Windows toast service.
        /// </summary>
        /// <param name="message">Message to show to the user.</param>
        public void ShowNotification(string message)
        {
            // Specify message
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);
            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            // Specify the absolute path to an image
            var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Resources", "zombie_64x64.png");
            var imagePath = "file:///" + dir;
            var imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;

            // Create the toast notification
            var toast = new ToastNotification(toastXml);

            // Show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Downloads latest pre-release from GitHub.
        /// </summary>
        /// <param name="settings">Zombie Settings.</param>
        public async void DownloadPreRelease(ZombieSettings settings)
        {
            var segments = GitHubUtils.ParseUrl(settings.Address);
            var client = new GitHubClient(new ProductHeaderValue("Zombie"));
            var tokenAuth = new Credentials(settings.AccessToken);
            client.Credentials = tokenAuth;

            Release prerelease = null;
            try
            {
                var releases = await client.Repository.Release.GetAll(segments["owner"], segments["repo"], ApiOptions.None);
                if (releases.Any()) prerelease = releases.OrderBy(x => x.PublishedAt).FirstOrDefault(x => x.Prerelease);
                if (prerelease == null)
                {
                    Messenger.Default.Send(new PrereleaseDownloaded
                    {
                        Status = PrereleaseStatus.Failed,
                        Settings = null
                    });
                    return;
                }
            }
            catch (Exception e)
            {
                _logger.Fatal("Failed to retrieve Pre-Release from GitHub. " + e.Message);
                return;
            }

            settings.LatestRelease = new ReleaseObject(prerelease);

            Messenger.Default.Send(new PrereleaseDownloaded
            {
                Status = PrereleaseStatus.Found,
                Settings = settings
            });
        }

        /// <summary>
        /// Commits Zombie Settings to GitHub overriding existing file.
        /// </summary>
        /// <param name="settings">Zombie Settings.</param>
        public async void PushSettingsToGitHub(ZombieSettings settings)
        {
            try
            {
                var segments = GitHubUtils.ParseUrl(settings.SettingsLocation);
                var client = new GitHubClient(new ProductHeaderValue("Zombie"));
                var tokenAuth = new Credentials(settings.AccessToken);
                client.Credentials = tokenAuth;

                var contents = await client.Repository.Content.GetAllContents(segments["owner"], segments["repo"], segments["file"]);
                if (!contents.Any())
                {
                    Messenger.Default.Send(new UpdateStatus { Message = "Could not get contents of the repo!" });
                    return;
                }

                var sha = string.Empty;
                foreach (var rc in contents)
                {
                    if (rc.Name != segments["file"]) continue;

                    sha = rc.Sha;
                    break;
                }

                if (string.IsNullOrEmpty(sha))
                {
                    Messenger.Default.Send(new UpdateStatus { Message = "Could not get valid SHA for the ZombieSettings!" });
                    return;
                }

                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    CheckAdditionalContent = true,
                    Formatting = Formatting.Indented
                };
                settings.ShouldSerialize = false;
                var json = JsonConvert.SerializeObject(settings, jsonSettings);

                var unused = await client.Repository.Content.UpdateFile(segments["owner"], segments["repo"], segments["file"],
                    new UpdateFileRequest("Zombie changed you!", json, sha, "master", true));

                Messenger.Default.Send(new UpdateStatus { Message = "Zombie changed your settings!"});
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                Messenger.Default.Send(new UpdateStatus { Message = "Failed to push update to your Zombie Settings!" });
            }
        }

        /// <summary>
        /// Commits pre-release to GitHub converting it to Latest Release.
        /// </summary>
        /// <param name="settings">Zombie Settings.</param>
        public async void PushReleaseToGitHub(ZombieSettings settings)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("Zombie"));
                var tokenAuth = new Credentials(settings.AccessToken);
                client.Credentials = tokenAuth;

                var segments = GitHubUtils.ParseUrl(settings.Address);
                var unused = await client.Repository.Release.Edit(segments["owner"], segments["repo"], settings.LatestRelease.Id,
                    new ReleaseUpdate
                    {
                        Body = settings.LatestRelease.Body,
                        Draft = false,
                        Name = settings.LatestRelease.Name,
                        Prerelease = false,
                        TagName = settings.LatestRelease.TagName,
                        TargetCommitish = "master"
                    });

                Messenger.Default.Send(new UpdateStatus { Message = "Zombie changed your Release!" });
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                Messenger.Default.Send(new UpdateStatus { Message = "Failed to push updates to your release!" });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
