using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;
using AqlaSerializer;

namespace XHelper
{
    [Serializable]
    [SerializableType]
    public class StarInfo : INotifyPropertyChanged
    {
        private Guid _uniqueID;
        private string _jName;
        private int _rate;
        private List<string> _storedMovieIDs;
        private string _nameStored;
        private string _avatorFileName;
        private string _avatorWebUrl;
        private string _officialWeb;
        private string _dirStored;

        [XmlElement] public Guid UniqueID { get { return _uniqueID; } set { _uniqueID = value; OnPropertyChanged(nameof(UniqueID)); } }
        [XmlElement] public string JName { get { return _jName; } set { _jName = value; OnPropertyChanged(nameof(JName)); } }
        [XmlElement] public int Rate { get { return _rate; } set { _rate = value; OnPropertyChanged(nameof(Rate)); } }
        [XmlElement] public List<string> StoredMovieIDs { get { return _storedMovieIDs; } set { _storedMovieIDs = value; OnPropertyChanged(nameof(StoredMovieIDs)); } }
        [XmlElement] public string NameStored { get { return _nameStored; } set { _nameStored = value; OnPropertyChanged(nameof(NameStored)); } }
        [XmlElement] public string AvatorFileName { get { return _avatorFileName; } set { _avatorFileName = value; OnPropertyChanged(nameof(AvatorFileName)); } }
        [XmlElement] public string AvatorWebUrl { get { return _avatorWebUrl; } set { _avatorWebUrl = value; OnPropertyChanged(nameof(AvatorWebUrl)); } }
        [XmlElement] public string OfficialWeb { get { return _officialWeb; } set { _officialWeb = value; OnPropertyChanged(nameof(OfficialWeb)); } }
        [XmlElement] public string DirStored { get { return _dirStored; } set { _dirStored = value; OnPropertyChanged(nameof(DirStored)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public override string ToString() => JName;

        public StarInfo()
        {
            UniqueID = Guid.NewGuid();
            StoredMovieIDs = new List<string>();
        }
        public StarInfo(string jname) : this() => JName = jname;

        public void CreateLocalStarDirectory(MovieInfo m)
        {
            NameStored = $"[{JName}]";
            DirStored = new DirectoryInfo(Path.Combine(Path.GetPathRoot(m.SourcePath), ConfigurationManager.AppSettings["ArchiveName"])).CreateSubdirectory(NameStored).FullName;
        }

        public void CreateLocalMovieDirectory(MovieInfo m)
        {
            m.SourcePath = Directory.CreateDirectory(Path.Combine(DirStored,m.ReleaseID)).FullName;
        }

        public async void SaveAvatorImageFileTemp(Stream stream_avator)
        {
            var au = new Uri(AvatorWebUrl);
            AvatorFileName = Path.Combine(DirStored, au.Segments[au.Segments.Length - 1]);

            using (FileStream sourceStream = new FileStream(AvatorFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await stream_avator.CopyToAsync(sourceStream);
                await sourceStream.FlushAsync();
            }
        }

        public List<MovieInfo> GetMoviesList()
        {
            List<MovieInfo> ms = new List<MovieInfo>();
            foreach (string m in StoredMovieIDs)
            {
                ms.Add(XGlobal.CurrentContext.GetMoviebyReleaseID(m));
            }
            return ms;
        }
    }

}