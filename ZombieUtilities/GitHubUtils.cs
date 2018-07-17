using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using RestSharp;

namespace Zombie.Utilities
{
    public static class GitHubUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private const string BaseUrl = "https://api.github.com";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        public static bool DownloadAssets(ZombieSettings settings, string url, string filePath)
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
                    if (!File.Exists(filePath)) return false;
                }
                catch (Exception e)
                {
                    _logger.Fatal(e.Message);
                    return false;
                }
            }
            else
            {
                _logger.Error("Download failed. Less than 120 bytes were downloaded.");
                return false;
            }

            return true;
        }
    }
}
