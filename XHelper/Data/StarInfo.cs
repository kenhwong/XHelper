using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Configuration;

namespace XHelper
{
    [Serializable]
    public class StarInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Guid UniqueID { get; set; }
        public string JName { get; set; }
        public int Rate { get; set; }
        public List<string> StoredMovieIDs { get; set; }
        public string NameStored { get; set; }
        public string AvatorFileName { get; set; }
        public Uri AvatorWebUri { get; set; }
        public string OfficialWeb { get; set; }
        public DirectoryInfo DirStored { get; set; }

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
            DirStored = new DirectoryInfo(Path.Combine(m.SourcePath.Root.FullName, ConfigurationManager.AppSettings["ArchiveName"])).CreateSubdirectory(NameStored);
        }

        public async void SaveAvatorImageFileTemp(Stream stream_avator)
        {
            AvatorFileName = Path.Combine(DirStored.FullName, AvatorWebUri.Segments[AvatorWebUri.Segments.Length - 1]);

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