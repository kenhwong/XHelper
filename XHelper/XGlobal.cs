﻿using Microsoft.Win32;
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
            CurrentContext = new XContext();

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

    public static class XService
    {
        #region IO Ex
        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        public static void ForceDeleteDirectory(DirectoryInfo dir)
        {
            /*
            Process proc = new Process();
            proc.StartInfo.FileName = "CMD.exe";
            proc.StartInfo.Arguments = "/c rmdir /s /q " + path;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
            */
            //DirectorySetNormalAttributes(path);
            for (int attempts = 0; attempts < 10; attempts++)
            {
                try
                {
                    if (dir.Exists)
                    {
                        dir.Delete(true);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(100);
                    //throw new Exception(e.Message);
                }
            }
        }

        /// <summary>
        /// 刪除目錄下的所有指定後綴名的視頻文件
        /// </summary>
        /// <param name="sourcedi">源目錄</param>
        /// <param name="ext">文件後綴名</param>
        public static void DeleteMovies(DirectoryInfo sourcedi, string ext)
        {
            if (sourcedi.Exists)
            {
                List<FileInfo> files = sourcedi.GetFiles("*" + ext, System.IO.SearchOption.TopDirectoryOnly).Where(f => f.Name.EndsWith(ext, StringComparison.CurrentCultureIgnoreCase)).ToList();
                files.ForEach(c => c.Delete());
            }
            else
            {
                throw new DirectoryNotFoundException(String.Format("源目录{0}不存在，可能已被刪除！", sourcedi.FullName));
            }
        }

        /// <summary>
        /// 移动文件夹中的所有文件夹与文件到另一个文件夹
        /// </summary>
        /// <param name="sourcePath">源文件夹</param>
        /// <param name="destPath">目标文件夹</param>
        public static void MoveFolder(DirectoryInfo sourcePath, DirectoryInfo destPath)
        {
            if (sourcePath.Exists)
            {
                if (!destPath.Exists)
                {
                    //目标目录不存在则创建
                    try
                    {
                        destPath.Create();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<FileInfo> files = new List<FileInfo>(sourcePath.EnumerateFiles());
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(destPath.FullName, c.Name);
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    c.MoveTo(destFile);
                });
                //获得源文件下所有目录文件
                List<DirectoryInfo> folders = new List<DirectoryInfo>(sourcePath.EnumerateDirectories());

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(destPath.FullName, c.Name);
                    //采用递归的方法实现
                    c.MoveTo(destDir);
                });
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }

        /// <summary>
        /// 移动文件夹中的所有文件夹与文件到另一个文件夹
        /// </summary>
        /// <param name="sourcePath">源文件夹</param>
        /// <param name="destPath">目标文件夹</param>
        public static void MoveImageFolder(string sourcePath, string destPath, string ext)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath, "*" + ext, System.IO.SearchOption.AllDirectories));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Move(c, destFile);
                });
                Directory.Delete(sourcePath, true);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }
        public static void MoveImageFolder(DirectoryInfo dirsource, DirectoryInfo dirdest, string ext)
        {
            if (dirsource.Exists)
            {
                if (!dirdest.Exists)
                    try { dirdest.Create(); } catch (Exception ex) { throw new Exception("创建目标目录失败：" + ex.Message); }

                List<FileInfo> files = new List<FileInfo>(dirsource.GetFiles("*" + ext, SearchOption.AllDirectories));
                files.ForEach(c =>
                    {
                        string strfiledest = Path.Combine(dirdest.FullName, c.Name);
                        if (File.Exists(strfiledest)) File.Delete(strfiledest);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        c.MoveTo(strfiledest);
                    });
                dirsource.Delete(true);
            }
        }

        /// <summary>
        /// 利用 WinRAR 进行压缩
        /// </summary>
        /// <param name="path">将要被压缩的文件夹（绝对路径）</param>
        /// <param name="rarPath">压缩后的 .rar 的存放目录（绝对路径）</param>
        /// <param name="rarName">压缩文件的名称（包括后缀）</param>
        /// <returns>true 或 false。压缩成功返回 true，反之，false。</returns>
        public static bool RAR(string path, string rarPath, string rarName)
        {
            bool flag = false;
            string rarexe;       //WinRAR.exe 的完整路径
            RegistryKey regkey;  //注册表键
            Object regvalue;     //键值
            string cmd;          //WinRAR 命令参数
            ProcessStartInfo startinfo;
            Process process;
            try
            {
                regkey = Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\shell\open\command");
                regvalue = regkey.GetValue("");  // 键值为 "d:\Program Files\WinRAR\WinRAR.exe" "%1"
                rarexe = regvalue.ToString();
                regkey.Close();
                rarexe = rarexe.Substring(1, rarexe.Length - 7);  // d:\Program Files\WinRAR\WinRAR.exe

                Directory.CreateDirectory(path);
                path = String.Format("\"{0}\"", path);
                //压缩命令，相当于在要压缩的文件夹(path)上点右键->WinRAR->添加到压缩文件->输入压缩文件名(rarName)
                cmd = string.Format("a {0} {1} -ep1 -o+ -inul -r -ibck",
                                    rarName,
                                    path);
                startinfo = new ProcessStartInfo();
                startinfo.FileName = rarexe;
                startinfo.Arguments = cmd;                          //设置命令参数
                startinfo.WindowStyle = ProcessWindowStyle.Hidden;  //隐藏 WinRAR 窗口

                startinfo.WorkingDirectory = rarPath;
                process = new Process();
                process.StartInfo = startinfo;
                process.Start();
                process.WaitForExit(); //无限期等待进程 winrar.exe 退出
                if (process.HasExited)
                {
                    flag = true;
                }
                process.Close();
            }
            catch (Exception e)
            {
                throw new Exception("", e);
            }
            return flag;
        }
        /// <summary>
        /// 利用 WinRAR 进行解压缩
        /// </summary>
        /// <param name="path">文件解压路径（绝对）</param>
        /// <param name="rarPath">将要解压缩的 .rar 文件的存放目录（绝对路径）</param>
        /// <param name="rarName">将要解压缩的 .rar 文件名（包括后缀）</param>
        /// <returns>true 或 false。解压缩成功返回 true，反之，false。</returns>
        public static bool UnRAR(string path, string rarPath, string rarName)
        {
            bool flag = false;
            string rarexe;
            RegistryKey regkey;
            Object regvalue;
            string cmd;
            ProcessStartInfo startinfo;
            Process process;
            try
            {
                regkey = Registry.ClassesRoot.OpenSubKey(@"WinRAR\shell\open\command");
                regvalue = regkey.GetValue("");
                rarexe = regvalue.ToString();
                regkey.Close();
                rarexe = rarexe.Substring(1, rarexe.Length - 7);

                Directory.CreateDirectory(path);
                //解压缩命令，相当于在要压缩文件(rarName)上点右键->WinRAR->解压到当前文件夹
                cmd = string.Format("e {0} {1} -y",
                                    rarName,
                                    path);
                startinfo = new ProcessStartInfo();
                startinfo.FileName = rarexe;
                startinfo.Arguments = cmd;
                startinfo.WindowStyle = ProcessWindowStyle.Hidden;

                startinfo.WorkingDirectory = rarPath;
                process = new Process();
                process.StartInfo = startinfo;
                process.Start();
                process.WaitForExit();
                if (process.HasExited)
                {
                    flag = true;
                }
                process.Close();
            }
            catch (Exception e)
            {
                throw new Exception("", e);
            }
            return flag;
        }

        #endregion

        #region String Ex
        public static void UrlCheck(ref string url)
        {
            if (url.StartsWith("//")) url = url.Insert(0, "http:");
        }

        public static string UrlCheck(string url)
        {
            if (url.StartsWith("//"))
                return url.Insert(0, "http:");
            else
                return url;
        }

        public static String Format_MachineSize(long msize)
        {
            if (msize < 0)
            {
                throw new ArgumentOutOfRangeException("machine size");
            }
            else if (msize >= 1024 * 1024 * 1024) //大小大于或等于1024M
            {
                return string.Format("{0:0.00} G", (double)msize / (1024 * 1024 * 1024));
            }
            else if (msize >= 1024 * 1024) //大小大于或等于1024K
            {
                return string.Format("{0:0.00} M", (double)msize / (1024 * 1024));
            }
            else if (msize >= 1024) //大小大于等于1024
            {
                return string.Format("{0:0.00} K", (double)msize / 1024);
            }
            else
            {
                return string.Format("{0:0.00}", msize);
            }
        }

        public static String IllegalFiltered(String SourceString)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(SourceString, "");
        }

        #endregion

        #region Stream Ex
        public static BitmapImage LoadBitmapImage(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                //bitmapImage.Freeze(); // just in case you want to load the image in another thread
                return bitmapImage;
            }
        }

        #endregion

         
    }

        [Serializable]
    public class XContext
    {
        public List<StarInfo> TotalStars { get; set; }
        public List<MovieInfo> TotalMovies { get; set; }

        public DirectoryInfo DirAppTemp { get; set; }

        public HttpClient HttpClient { get; set; }
        public CancellationTokenSource HttpCTS { get; set; }

        public string USite { get; set; }
        public string CSite { get; set; }
        public string BSite { get; set; }

        public string DataFile { get; set; }
        public string ArchiveName { get; set; }

        //public HttpClient GlobalHttpClient { get; set; }
        //public CancellationTokenSource GlobalCTS { get; set; } = new CancellationTokenSource();

        public bool UseProxy { get; set; }
        public IWebProxy SSProxy { get; set; }

        public XContext()
        {
            HttpClient = new HttpClient();
            HttpCTS = new CancellationTokenSource();

            TotalStars = new List<StarInfo>();
            TotalMovies = new List<MovieInfo>();

            CSite = ConfigurationManager.AppSettings["CSite"];
            USite = ConfigurationManager.AppSettings["USite"];
            BSite = ConfigurationManager.AppSettings["BSite"];

            DataFile = ConfigurationManager.AppSettings["DataFile"];
            ArchiveName = ConfigurationManager.AppSettings["ArchiveName"];

            string _strapppathtemp = Path.Combine(Path.GetTempPath(), $"_x_{Guid.NewGuid():D}");
            if (Directory.Exists(_strapppathtemp)) XService.ForceDeleteDirectory(new DirectoryInfo(_strapppathtemp));

            UseProxy = ConfigurationManager.AppSettings["UseProxy"] == "0" ? false : true;
            SSProxy = new WebProxy(ConfigurationManager.AppSettings["Proxy"]);
        }

        public void ResetWWW()
        {
            CSite = ConfigurationManager.AppSettings["CSite"];
            USite = ConfigurationManager.AppSettings["USite"];
            BSite = ConfigurationManager.AppSettings["BSite"];
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
    #endregion
}
