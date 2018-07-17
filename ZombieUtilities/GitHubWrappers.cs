using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace Zombie.Utilities
{
    public class ReleaseObject : INotifyPropertyChanged
    {
        private int _id;
        [JsonProperty("id")]
        public int Id
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged("Id"); }
        }

        private string _tagName;
        [JsonProperty("tag_name")]
        public string TagName
        {
            get { return _tagName; }
            set { _tagName = value; RaisePropertyChanged("TagName"); }
        }

        private string _name;
        [JsonProperty("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged("Name"); }
        }

        private string _body;
        [JsonProperty("body")]
        public string Body
        {
            get { return _body; }
            set { _body = value; RaisePropertyChanged("Body"); }
        }

        private bool _prerelease;
        [JsonProperty("prerelease")]
        public bool Prerelease
        {
            get { return _prerelease; }
            set { _prerelease = value; RaisePropertyChanged("Prerelease"); }
        }

        private DateTime _publishedAt;
        [JsonProperty("published_at")]
        public DateTime PublishedAt
        {
            get { return _publishedAt; }
            set { _publishedAt = value; RaisePropertyChanged("PublishedAt"); }
        }

        private AuthorObject _author;
        [JsonProperty("author")]
        public AuthorObject Author
        {
            get { return _author; }
            set { _author = value; RaisePropertyChanged("Author"); }
        }

        [JsonProperty("assets")]
        public List<AssetObject> Assets { get; set; } = new List<AssetObject>();

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
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
