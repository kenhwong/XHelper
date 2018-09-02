using System;
using System.Collections.Generic;
using System.IO;
using MediaInfoDotNet;
using System.Drawing;
using System.Linq;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using AqlaSerializer;

namespace XHelper
{
    [Serializable]
    [SerializableType]
    public class MovieInfo : INotifyPropertyChanged

    {
        private string _releaseID;
        private string _releaseName;
        private long _releaseDateTicks;
        private int _releaseLength;
        private string _releaseStudio;
        private string _releaseLabel;
        private ObservableCollection<string> _genre = new ObservableCollection<string>();
        private int _vWidth;
        private int _vHeight;
        private int _nRefFrame;
        private string _vFormat;
        private int _mediaFilesTotalLength;
        private long _mediaFilesTotalSize;
        private List<MediaFileInfo> _mediaFiles;
        private string _mediaFilesDecodeDesc;
        private List<Guid> _actorUIDs;
        private string _sourcePath;
        private string _sourceMediaFileExt;
        private string _coverFileName;
        private string _coverWebUrl;
        private string _officialWeb;

        [XmlElement] public string ReleaseID { get { return _releaseID; } set { _releaseID = value; OnPropertyChanged(nameof(ReleaseID)); } }
        [XmlElement] public string ReleaseName { get { return _releaseName; } set { _releaseName = value; OnPropertyChanged(nameof(ReleaseName)); } }
        [XmlElement] public long ReleaseDateTicks { get { return _releaseDateTicks; } set { _releaseDateTicks = value; OnPropertyChanged(nameof(ReleaseDateTicks)); } }
        [XmlElement] public int ReleaseLength { get { return _releaseLength; } set { _releaseLength = value; OnPropertyChanged(nameof(ReleaseLength)); } }
        [XmlElement] public string ReleaseStudio { get { return _releaseStudio; } set { _releaseStudio = value; OnPropertyChanged(nameof(ReleaseStudio)); } }
        [XmlElement] public string ReleaseLabel { get { return _releaseLabel; } set { _releaseLabel = value; OnPropertyChanged(nameof(ReleaseLabel)); } }
        [XmlElement] public ObservableCollection<string> Genre { get { return _genre; } set { _genre = value; OnPropertyChanged(nameof(Genre)); } }
        [XmlElement] public int VWidth { get { return _vWidth; } set { _vWidth = value; OnPropertyChanged(nameof(VWidth)); } }
        [XmlElement] public int VHeight { get { return _vHeight; } set { _vHeight = value; OnPropertyChanged(nameof(VHeight)); } }
        [XmlElement] public int NRefFrame { get { return _nRefFrame; } set { _nRefFrame = value; OnPropertyChanged(nameof(NRefFrame)); } }
        [XmlElement] public string VFormat { get { return _vFormat; } set { _vFormat = value; OnPropertyChanged(nameof(VFormat)); } }
        [XmlElement] public int MediaFilesTotalLength { get { return _mediaFilesTotalLength; } set { _mediaFilesTotalLength = value; OnPropertyChanged(nameof(MediaFilesTotalLength)); } }
        [XmlElement] public long MediaFilesTotalSize { get { return _mediaFilesTotalSize; } set { _mediaFilesTotalSize = value; OnPropertyChanged(nameof(MediaFilesTotalSize)); } }
        [XmlElement] public List<MediaFileInfo> MediaFiles { get { return _mediaFiles; } set { _mediaFiles = value; OnPropertyChanged(nameof(MediaFiles)); } }
        [XmlElement] public string MediaFilesDecodeDesc { get { return _mediaFilesDecodeDesc; } set { _mediaFilesDecodeDesc = value; OnPropertyChanged(nameof(MediaFilesDecodeDesc)); } }
        [XmlElement] public List<Guid> ActorUIDs { get { return _actorUIDs; } set { _actorUIDs = value; OnPropertyChanged(nameof(ActorUIDs)); } }
        [XmlElement] public string SourcePath { get { return _sourcePath; } set { _sourcePath = value; OnPropertyChanged(nameof(SourcePath)); } }
        [XmlElement] public string SourceMediaFileExt { get { return _sourceMediaFileExt; } set { _sourceMediaFileExt = value; OnPropertyChanged(nameof(SourceMediaFileExt)); } }
        [XmlElement] public string CoverFileName { get { return _coverFileName; } set { _coverFileName = value; OnPropertyChanged(nameof(CoverFileName)); } }
        [XmlElement] public string CoverWebUrl { get { return _coverWebUrl; } set { _coverWebUrl = value; OnPropertyChanged(nameof(CoverWebUrl)); } }
        [XmlElement] public string OfficialWeb { get { return _officialWeb; } set { _officialWeb = value; OnPropertyChanged(nameof(OfficialWeb)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public override string ToString()
        {
            return ReleaseID;
        }

        public MovieInfo() { }

        /// <summary>
        /// Analysis media files -> MediaFiles/MediaFilesTotalLength/MediaFilesTotalSize/MediaFilesDecodeDesc/VWidth/VHeight/NRefFrame/VFormat
        /// </summary>
        /// <param name="targetFileNameFromSource">Full name of media file.</param>
        public MovieInfo(string targetFileNameFromSource)
        {
            ActorUIDs = new List<Guid>();
            MediaFiles = new List<MediaFileInfo>();

            SourcePath = Path.GetDirectoryName(targetFileNameFromSource);
            SourceMediaFileExt = Path.GetExtension(targetFileNameFromSource);
            if (Directory.Exists(SourcePath))
            {
                List<string> files = Directory.EnumerateFiles(SourcePath, "*" + SourceMediaFileExt, SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(SourceMediaFileExt, StringComparison.CurrentCultureIgnoreCase)).ToList();
                files.ForEach(f => MediaFiles.Add(new MediaFileInfo(f)));
            }
            else
            {
                throw new DirectoryNotFoundException(String.Format("源目录{0}不存在！", SourcePath));
            }

            if (MediaFiles == null) return;
            if (MediaFiles.Count == 0) return;
            VWidth = MediaFiles[0].VWidth;
            VHeight = MediaFiles[0].VHeight;
            NRefFrame = MediaFiles[0].NRefFrame;
            VFormat = MediaFiles[0].VFormat;
            MediaFilesTotalLength = MediaFiles.Sum(t => t.VLength);
            MediaFilesTotalSize = MediaFiles.Sum(t => t.VFileSize);
            var _vlength = TimeSpan.FromMinutes(MediaFilesTotalLength);
            MediaFilesDecodeDesc = string.Format("[{0}, {1}*{2}, {3:00}:{4:00}:{5:00}, {6}, {7}]",
                    MediaFiles[0].VFormat,
                    MediaFiles[0].VWidth,
                    MediaFiles[0].VHeight,
                    _vlength.Hours,
                    _vlength.Minutes,
                    _vlength.Seconds,
                    XService.Format_MachineSize(MediaFilesTotalSize),
                    MediaFiles[0].V2D3DVR);
        }
    }

    public class QueryResultMovieInfo : MovieInfo
    {
        public ImageSource MovieCoverImage { get; set; }
    }

    [Serializable]
    [SerializableType]
    public class MediaFileInfo : IDisposable
    {
        [XmlElement] public int VLength { get; set; } //Minutes
        [XmlElement] public int VWidth { get; set; }
        [XmlElement] public int VHeight { get; set; }
        [XmlElement] public int NRefFrame { get; set; }
        [XmlElement] public string VFormat { get; set; }
        [XmlElement] public long VFileSize { get; set; }
        [XmlElement] public string V2D3DVR { get; set; }
        [XmlElement] public string MediaDecodeDesc { get; set; }
        [XmlElement] public string LocalFileName { get; set; }
        [XmlIgnore] private MediaFile _mfile;

        public void Dispose()
        {
            if (_mfile != null)
            {
                //_mfile.Dispose();
                _mfile = null;
            }
        }

        public MediaFileInfo() { }
        public MediaFileInfo(string fname)
        {
            LocalFileName = fname;
            _mfile = new MediaFile(LocalFileName);
            VFileSize = _mfile.General.Size;
            TimeSpan tss = TimeSpan.FromMilliseconds(Convert.ToDouble(_mfile.General.Duration));
            if (_mfile.Video.Count > 0)
            {
                V2D3DVR = "2D";
                int nref = 1;

                VFormat = _mfile.General.Format;
                VWidth = _mfile.Video[0].Width;
                VHeight = _mfile.Video[0].Height;
                VLength = tss.Minutes;
                NRefFrame = 1;

                if (_mfile.Video[0].encoderSettings.Count > 0)
                {
                    if (int.TryParse(_mfile.Video[0].EncoderSettings["ref"], out nref))
                        if (nref > 1)
                        {
                            V2D3DVR = "3D";
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
                    V2D3DVR);
            }
        }
    }

}