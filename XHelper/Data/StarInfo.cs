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
    [SerializableType]
    public class StarInfo : INotifyPropertyChanged
    {
        private Guid _uniqueID;
        private string _jName;
        private int _rate;
        private List<string> _storedMovieIDs;
        private string _nameStored;
        private string _avatorFileName;
        private Uri _avatorWebUri;
        private string _officialWeb;
        private string _dirStored;

        public Guid UniqueID { get { return _uniqueID; } set { _uniqueID = value; OnPropertyChanged(nameof(UniqueID)); } }
        public string JName { get { return _jName; } set { _jName = value; OnPropertyChanged(nameof(JName)); } }
        public int Rate { get { return _rate; } set { _rate = value; OnPropertyChanged(nameof(Rate)); } }
        public List<string> StoredMovieIDs { get { return _storedMovieIDs; } set { _storedMovieIDs = value; OnPropertyChanged(nameof(StoredMovieIDs)); } }
        public string NameStored { get { return _nameStored; } set { _nameStored = value; OnPropertyChanged(nameof(NameStored)); } }
        public string AvatorFileName { get { return _avatorFileName; } set { _avatorFileName = value; OnPropertyChanged(nameof(AvatorFileName)); } }
        public Uri AvatorWebUri { get { return _avatorWebUri; } set { _avatorWebUri = value; OnPropertyChanged(nameof(AvatorWebUri)); } }
        public string OfficialWeb { get { return _officialWeb; } set { _officialWeb = value; OnPropertyChanged(nameof(OfficialWeb)); } }
        public string DirStored { get { return _dirStored; } set { _dirStored = value; OnPropertyChanged(nameof(DirStored)); } }

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
            DirStored = new DirectoryInfo(Path.Combine(m.SourcePath.Root.FullName, ConfigurationManager.AppSettings["ArchiveName"])).CreateSubdirectory(NameStored).FullName;
        }

        public void CreateLocalMovieDirectory(MovieInfo m)
        {
            m.SourcePath = Directory.CreateDirectory(Path.Combine(DirStored,m.ReleaseID));
        }

        public async void SaveAvatorImageFileTemp(Stream stream_avator)
        {
            AvatorFileName = Path.Combine(DirStored, AvatorWebUri.Segments[AvatorWebUri.Segments.Length - 1]);

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