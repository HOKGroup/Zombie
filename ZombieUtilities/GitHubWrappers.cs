using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Zombie.Utilities
{
    public class ReleaseObject
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("author")]
        public AuthorObject Author { get; set; }

        [JsonProperty("assets")]
        public List<AssetObject> Assets { get; set; }
    }

    public class AuthorObject
    {
        [JsonProperty("login")]
        public string Login { get; set; }
    }

    public class AssetObject
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        public bool IsArchive()
        {
            return Path.GetExtension(Name).ToLower() == ".zip" ||
                   Path.GetExtension(Name).ToLower() == ".rar";
        }

        public override bool Equals(object obj)
        {
            var item = obj as AssetObject;
            return item != null && Name.Equals(item.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
