using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XHelper
{
    [Serializable]
    class XGlobal
    {
    }

    [Serializable]
    public class XContext
    {
        public int StorageCount { get; set; }
        public List<StarInfo> TotalStars { get; set; }
        public List<MovieInfo> TotalMovies { get; set; }

        public DirectoryInfo DirAppTemp { get; set; }
        public DirectoryInfo DirCoversTemp { get; set; }
        public DirectoryInfo DirStarsTemp { get; set; }
        public DirectoryInfo DirSamplesTemp { get; set; }



        public string USite { get; set; } = "www.avsox.com";
        public string CSite { get; set; } = "www.avmoo.com";
        public string BSite { get; set; } = "www.javbus.com";

        //public HttpClient GlobalHttpClient { get; set; }
        //public CancellationTokenSource GlobalCTS { get; set; } = new CancellationTokenSource();

        public bool IsUseProxy { get; set; }
        public IWebProxy SSProxy { get; set; }

        public XContext()
        {
            TotalStars = new List<StarInfo>();
            TotalMovies = new List<MovieInfo>();

            CSite = "avio.pw"; //http://jav.tellme.pw
            USite = "avso.pw"; //http://javu.tellme.pw
            BSite = "www.javbus.com";

            string _strapppathtemp = Path.Combine(Path.GetTempPath(), "__xdatatemp");
            if (Directory.Exists(_strapppathtemp)) XGlobal.ForceDeleteDirectory(new DirectoryInfo(_strapppathtemp));
            DirAppTemp = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory("__xdatatemp");

            XGlobal.RebuildSubDirTemp();

            IsUseProxy = false;
            SSProxy = new WebProxy("http://127.0.0.1:10800");
        }

        public void ResetWWW()
        {
            CSite = "avio.pw"; //http://jav.tellme.pw
            USite = "avso.pw"; //http://javu.tellme.pw
            BSite = "www.javbus.com";
        }

        public StarInfo GetStarbyUID(Guid uid)
        {
            return TotalStars.Find(s => s.UniqueID == uid);
        }
        public MovieInfo GetMoviebyReleaseID(string rid)
        {
            return TotalMovies.Find(m => m.ReleaseID == rid);
        }

        public bool RemoveMovie(MovieInfo m)
        {

            return TotalMovies.Remove(m);
        }
    }

}
