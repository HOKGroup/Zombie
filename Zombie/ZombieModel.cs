#region References

using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NLog;
using Octokit;
using Zombie.Utilities;
using ZombieUtilities;
using ZombieUtilities.Client;

#endregion

namespace Zombie.Controls
{
    public class ZombieModel
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Downloads latest pre-release from GitHub.
        /// </summary>
        /// <param name="settings">Zombie Settings.</param>
        public async void DownloadPreRelease(ZombieSettings settings)
        {
            var segments = ParseUrl(settings.Address);
            var client = new GitHubClient(new ProductHeaderValue("Zombie"));
            var tokenAuth = new Credentials(settings.AccessToken);
            client.Credentials = tokenAuth;

            var releases = await client.Repository.Release.GetAll(segments["owner"], segments["repo"], ApiOptions.None);
            var prerelease = releases.OrderBy(x => x.PublishedAt).FirstOrDefault(x => x.Prerelease);
            if (prerelease == null) return;

            //TODO: There is no reason to carry a different Release wrapper etc. Let's use Octakit one. 
            settings.LatestRelease = new ReleaseObject
            {
                Id = prerelease.Id,
                Name = prerelease.Name,
                Body = prerelease.Body,
                PublishedAt = prerelease.PublishedAt.Value.LocalDateTime,
                TagName = prerelease.TagName,
                Prerelease = prerelease.Prerelease,
                Author = new AuthorObject
                {
                    Login = prerelease.Author.Login,
                },
                Assets = prerelease.Assets.Select(x => new AssetObject
                {
                    Id = x.Id,
                    Name = x.Name,
                    Url = x.Url
                }).ToList()
            };

            Messenger.Default.Send(new GuiUpdate
            {
                Settings = settings,
                Message = "Found new Pre-Release! " + prerelease.TagName,
                Status = Status.Succeeded
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
                var segments = ParseUrl(settings.SettingsLocation);
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

                var segments = ParseUrl(settings.Address);
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

        #region Utilities

        /// <summary>
        /// Parses URL into its components parts. 
        /// </summary>
        /// <param name="url">Base url to parse.</param>
        /// <returns>Dictionary with owner, repo and file info.</returns>
        public static Dictionary<string, string> ParseUrl(string url)
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            var owner = segments[1].TrimLastCharacter("/");
            var repo = segments[2].TrimLastCharacter("/");
            var file = segments.Length > 3 ? segments[4] : string.Empty;

            return new Dictionary<string, string>()
            {
                {"owner", owner},
                {"repo", repo},
                {"file", file}
            };
        }

        #endregion
    }
}
