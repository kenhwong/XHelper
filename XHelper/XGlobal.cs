using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace XHelper
{
    [Serializable]
    public static class XGlobal
    {
        public static XContext CurrentContext { get; set; }

        static XGlobal()
        {
            CurrentContext = XContext.Instance;

            ServicePointManager.DefaultConnectionLimit = 512;
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                UseProxy = CurrentContext.UseProxy,
                Proxy = (CurrentContext.UseProxy) ? CurrentContext.SSProxy : null
            };

            CurrentContext.HttpClient = new HttpClient(handler);
            CurrentContext.HttpClient.MaxResponseContentBufferSize = 1024 * 1024;
            CurrentContext.HttpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)");
            CurrentContext.HttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");

            BinaryFormatter binf = new BinaryFormatter();
            //Initialize CurrentContext
            if (File.Exists(CurrentContext.DataFile))
                using (FileStream fs = new FileStream(CurrentContext.DataFile, FileMode.Open)) CurrentContext = (XContext)binf.Deserialize(fs);
        }


    }


    [Serializable]
    public sealed class XContext
    {
        private static readonly XContext instance = new XContext();
        public static XContext Instance { get { return instance; } }

        public List<StarInfo> TotalStars { get; set; }
        public List<MovieInfo> TotalMovies { get; set; }

        public DirectoryInfo DirAppTemp { get; set; }

        public HttpClient HttpClient { get; set; }
        public CancellationTokenSource HttpCTS { get; set; }

        public string DataFile { get; set; }
        public string ArchiveName { get; set; }

        //public HttpClient GlobalHttpClient { get; set; }
        //public CancellationTokenSource GlobalCTS { get; set; } = new CancellationTokenSource();

        public bool UseProxy { get; set; }
        public IWebProxy SSProxy { get; set; }

        //private XContext() { }
        private XContext()
        {
            HttpClient = new HttpClient();
            HttpCTS = new CancellationTokenSource();

            TotalStars = new List<StarInfo>();
            TotalMovies = new List<MovieInfo>();

            DataFile = ConfigurationManager.AppSettings["DataFile"];
            ArchiveName = ConfigurationManager.AppSettings["ArchiveName"];

            string _strapppathtemp = Path.Combine(Path.GetTempPath(), $"_x_{Guid.NewGuid():D}");
            if (Directory.Exists(_strapppathtemp)) XService.ForceDeleteDirectory(new DirectoryInfo(_strapppathtemp));

            UseProxy = ConfigurationManager.AppSettings["UseProxy"] == "0" ? false : true;
            SSProxy = new WebProxy(ConfigurationManager.AppSettings["Proxy"]);
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

    #region Converter
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value == int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var data = (bool)value;
            if (data)
            {
                return System.Convert.ToInt32(parameter);
            }
            return -1;
        }
    }

    public class VarToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }

    public class PrefixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            return (value as string).StartsWith(parameter as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }


    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = (bool)value;
            if (bValue) return System.Windows.Visibility.Visible;
            else return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Visibility visibility = (System.Windows.Visibility)value;

            if (visibility == System.Windows.Visibility.Visible) return true;
            else return false;
        }
    }

    public class NRefFrameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int nreff = (int)value;
            if (nreff == 1) return "2D";
            else return "3D";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }

    public class MachineSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long lsize = (long)value;
            return XService.Format_MachineSize(lsize);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            else return new BitmapImage(new Uri(value.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TrimmedTextConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string otext = value as string;
            int len = int.Parse(parameter as string);
            return otext.Length > len ? otext.Substring(0, len) : otext;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
