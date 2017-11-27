using System;
using System.Collections.Generic;
using System.IO;
using MediaInfoDotNet;
using System.Drawing;
using System.Linq;
using System.ComponentModel;

namespace XHelper
{
    [Serializable]
    public class MovieInfo : INotifyPropertyChanged

    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ReleaseID { get; set; }
        public string ReleaseName { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ReleaseStudio { get; set; }
        public string ReleaseLabel { get; set; }
        public List<string> Genre { get; set; }
        public int VWidth { get; set; }
        public int VHeight { get; set; }
        public int NRefFrame { get; set; }
        public string VFormat { get; set; }
        public TimeSpan MediaFilesTotalLength { get; set; }
        public long MediaFilesTotalSize { get; set; }
        public List<MediaFileInfo> MediaFiles { get; set; }
        public string MediaFilesDecodeDesc { get; set; }
        public List<Guid> ActorUIDs { get; set; }
        public DirectoryInfo SourcePath { get; set; }
        public string SourceMediaFileExt { get; set; }
        public string CoverFileName { get; set; }
        public Uri OfficialWeb { get; set; }

        public override string ToString()
        {
            return ReleaseID;
        }

        /// <summary>
        /// Analysis media files -> MediaFiles/MediaFilesTotalLength/MediaFilesTotalSize/MediaFilesDecodeDesc/VWidth/VHeight/NRefFrame/VFormat
        /// </summary>
        /// <param name="targetFileNameFromSource">Full name of media file.</param>
        public MovieInfo(string targetFileNameFromSource)
        {
            ActorUIDs = new List<Guid>();
            MediaFiles = new List<MediaFileInfo>();

            SourcePath = new DirectoryInfo(Path.GetDirectoryName(targetFileNameFromSource));
            SourceMediaFileExt = Path.GetExtension(targetFileNameFromSource);
            if (SourcePath.Exists)
            {
                List<FileInfo> files = SourcePath.EnumerateFiles("*" + SourceMediaFileExt, SearchOption.TopDirectoryOnly)
                    .Where(f => f.Name.EndsWith(SourceMediaFileExt, StringComparison.CurrentCultureIgnoreCase)).ToList();
                files.ForEach( f => MediaFiles.Add(new MediaFileInfo(f)));
            }
            else
            {
                throw new DirectoryNotFoundException(String.Format("源目录{0}不存在！", SourcePath.FullName));
            }

            if (MediaFiles == null) return;
            if (MediaFiles.Count == 0) return;
            VWidth = MediaFiles[0].VWidth;
            VHeight = MediaFiles[0].VHeight;
            NRefFrame = MediaFiles[0].NRefFrame;
            VFormat = MediaFiles[0].VFormat;
            MediaFilesTotalLength = TimeSpan.FromSeconds(MediaFiles.Sum(t => t.VLength.TotalSeconds));
            MediaFilesTotalSize = MediaFiles.Sum(t => t.VFileSize);
            MediaFilesDecodeDesc = string.Format("[{0}, {1}*{2}, {3:00}:{4:00}:{5:00}, {6}, {7}]",
                    MediaFiles[0].VFormat,
                    MediaFiles[0].VWidth,
                    MediaFiles[0].VHeight,
                    MediaFilesTotalLength.Hours,
                    MediaFilesTotalLength.Minutes,
                    MediaFilesTotalLength.Seconds,
                    XService.Format_MachineSize(MediaFilesTotalSize),
                    MediaFiles[0].Type2D3D);
        }
    }

    [Serializable]
    public class MediaFileInfo : IDisposable
    {
        public TimeSpan VLength { get; set; }
        public int VWidth { get; set; }
        public int VHeight { get; set; }
        public int NRefFrame { get; set; }
        public string VFormat { get; set; }
        public long VFileSize { get; set; }
        public Media2D3DType Type2D3D { get; set; }
        public string MediaDecodeDesc { get; set; }
        public FileInfo LocalFileInfo { get; set; }
        [NonSerialized]
        private MediaFile _mfile;

        public void Dispose()
        {
            if (_mfile != null)
            {
                //_mfile.Dispose();
                _mfile = null;                
            }
        }

        public MediaFileInfo(FileInfo finfo)
        {
            LocalFileInfo = finfo;
            _mfile = new MediaFile(LocalFileInfo.FullName);
            VFileSize = _mfile.General.Size;
            TimeSpan tss = TimeSpan.FromMilliseconds(Convert.ToDouble(_mfile.General.Duration));
            if (_mfile.Video.Count > 0)
            {
                Type2D3D = Media2D3DType.Type2D;
                int nref = 1;

                VFormat = _mfile.General.Format;
                VWidth = _mfile.Video[0].Width;
                VHeight = _mfile.Video[0].Height;
                VLength = tss;
                NRefFrame = 1;

                if (_mfile.Video[0].encoderSettings.Count > 0)
                {
                    if (int.TryParse(_mfile.Video[0].EncoderSettings["ref"], out nref))
                        if (nref > 1)
                        {
                            Type2D3D = Media2D3DType.Type3D;
                            NRefFrame = nref;
                        }
                }
                MediaDecodeDesc = string.Format("[{0}, {1}*{2}, {3:00}:{4:00}:{5:00}, {6}, {7}]",
                    _mfile.General.Format,
                    _mfile.Video[0].Width,
                    _mfile.Video[0].Height,
                    tss.Hours,
                    tss.Minutes,
                    tss.Seconds,
                    XService.Format_MachineSize(_mfile.General.Size),
                    Type2D3D);
            }
        }
    }

    public enum Media2D3DType { Type2D = 1, Type3D = 2 }
}