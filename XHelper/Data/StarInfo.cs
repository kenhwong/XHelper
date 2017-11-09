using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.ComponentModel;

namespace XHelper
{
    [Serializable]
    public class StarInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Guid UniqueID { get; set; }
        public string JName { get; set; }
        public string CName { get; set; }
        public string EName { get; set; }
        public string KanaName { get; set; }
        public int Rate { get; set; }
        public List<string> StoredMovieIDs { get; set; }
        public Uri AvatorWebUri { get; set; }
        public string NameStored { get; set; }
        public FileInfo AvatorFileInfo { get; set; }
        public string OfficialWeb_JA { get; set; }
        public string OfficialWeb_EN { get; set; }

        public DirectoryInfo LastLocation { get; set; }
        public DirectoryInfo DirStored { get; set; }
        public DirectoryInfo DirStoredTemp { get; set; }

        public override string ToString()
        {
            if (JName == string.Empty) return CName;
            return JName;
        }

        public StarInfo() 
        { 
            UniqueID = Guid.NewGuid(); 
            StoredMovieIDs = new List<string>(); 
        }
        public StarInfo(string jname) 
        { 
            JName = jname;
            UniqueID = Guid.NewGuid();
            StoredMovieIDs = new List<string>();
        }

        public void CreateLocalStarDirectory(MovieInfo m)
        {
            NameStored = $"[{JName}][{EName}]";
            LastLocation = new DirectoryInfo(Path.Combine(m.SourcePath.Root.FullName, XGlobal.ArchiveName)).CreateSubdirectory(NameStored);
        }

        public void CreateStarDirectoryTemp()
        {
            DirStoredTemp = XGlobal.CurrentContext.DirStarsTemp.CreateSubdirectory(string.Format("[{0}][{1}]", JName, EName));
        }

        public async void SaveAvatorImageFileTemp(Stream stream_avator)
        {
            string tempfilename_avator = Path.Combine(DirStoredTemp.FullName, AvatorWebUri.Segments[AvatorWebUri.Segments.Length - 1]);

            using (FileStream sourceStream = new FileStream(tempfilename_avator, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await stream_avator.CopyToAsync(sourceStream);
                await sourceStream.FlushAsync();
            }
            AvatorFileInfo = new FileInfo(tempfilename_avator);
        }

        public void RestoreAvatorImageFileFromTemp()
        {
            if (AvatorFileInfo != null)
            {
                AvatorFileInfo.MoveTo(Path.Combine(DirStored.FullName, AvatorFileInfo.Name));
                DirStoredTemp.Delete(true);
            }
        }

        public List<MovieInfo> GetMoviesList()
        {
            List<MovieInfo> ms = new List<MovieInfo>();
            foreach(string m in StoredMovieIDs)
            {
                ms.Add(XGlobal.CurrentContext.GetMoviebyReleaseID(m));
            }
            return ms;
        }
    }

}