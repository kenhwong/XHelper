using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace XHelper
{
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
            if (url.StartsWith("./"))
                return url.Insert(0, "http:");
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

        #region Net HTTP Ex

        /// <summary>
        /// Get string data from web
        /// </summary>
        /// <param name="uripage">web address uri</param>
        /// <param name="referrer">web request header referrer</param>
        /// <returns></returns>
        public static async Task<(string response, HttpRequestMessage requestmessage)> Func_Net_ReadWebData(Uri uripage, Uri referrer)
        {
            HttpRequestMessage reqmsg = null;
            try
            {
                XGlobal.CurrentContext.HttpClient.DefaultRequestHeaders.Add("over18", "18");
                XGlobal.CurrentContext.HttpClient.DefaultRequestHeaders.Referrer = referrer;
                var resp = await XGlobal.CurrentContext.HttpClient.GetAsync(uripage, XGlobal.CurrentContext.HttpCTS.Token);
                reqmsg = resp.RequestMessage;

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    resp.EnsureSuccessStatusCode();
                    return (await resp.Content.ReadAsStringAsync(), reqmsg);
                }
                else return (resp.StatusCode.ToString(), reqmsg);
                //}
            }
            catch (HttpRequestException hre)
            {
                return (hre.ToString(), reqmsg);
            }
            catch (Exception ex)
            {
                return (ex.ToString(), reqmsg);
            }

        }

        public static async Task<(string response, HttpRequestMessage requestmessage)> Func_Net_ReadWebData(string urlpage, Uri referrer)
        {
            UrlCheck(ref urlpage);
            return await Func_Net_ReadWebData(new Uri(urlpage), referrer);
        }

        public static async Task<(string response, HttpRequestMessage requestmessage)> Func_Net_ReadWebData(Uri uripage)
        {
            return await Func_Net_ReadWebData(uripage, uripage);
        }

        public static async Task<(string response, HttpRequestMessage requestmessage)> Func_Net_ReadWebData(string urlpage)
        {
            UrlCheck(ref urlpage);
            return await Func_Net_ReadWebData(new Uri(urlpage));
        }

        /// <summary>
        /// Get stream data from web
        /// </summary>
        /// <param name="uriimage">web address uri</param>
        /// <param name="referrer">web request header referrer</param>
        /// <returns></returns>
        public static async Task<Stream> Func_Net_ReadWebStream(Uri uriimage, Uri referrer)
        {
            XGlobal.CurrentContext.HttpClient.DefaultRequestHeaders.Add("over18", "18");
            XGlobal.CurrentContext.HttpClient.DefaultRequestHeaders.Referrer = referrer;
            var resp = await XGlobal.CurrentContext.HttpClient.GetAsync(uriimage, XGlobal.CurrentContext.HttpCTS.Token);

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStreamAsync();
            }
            else
            {
                return Stream.Null;
            }
            //}

        }

        public static async Task<Stream> Func_Net_ReadWebStream(string urlimage, Uri referrer)
        {
            UrlCheck(ref urlimage);
            return await Func_Net_ReadWebStream(new Uri(urlimage), referrer);
        }

        #endregion

        #region XML Serialize Ex
        public static class XMLDeserializerHelper
        {
            /// <summary>
            /// Deserialization XML document
            /// </summary>
            /// <typeparam name="T">>The class type which need to deserializate</typeparam>
            /// <param name="xmlPath">XML path</param>
            /// <returns>Deserialized class object</returns>
            public static T Deserialization<T>(string xmlPath)
            {
                //check file is exists in case
                if (File.Exists(xmlPath))
                {
                    //read file to memory
                    StreamReader stream = new StreamReader(xmlPath);
                    XmlTextReader xreader = new XmlTextReader(stream) { Namespaces = false };
                    //declare a serializer
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    try
                    {
                        //Do deserialize 
                        //return (T)serializer.Deserialize(stream);
                        return (T)serializer.Deserialize(xreader);
                    }
                    //if some error occured,throw it
                    catch (InvalidOperationException error)
                    {
                        throw error;
                    }
                    //finally close all resourece
                    finally
                    {
                        //close the reader stream
                        stream.Close();
                        //release the stream resource
                        stream.Dispose();
                    }
                }
                //File not exists
                else
                {
                    //throw data error exception
                    throw new InvalidDataException("Can not open xml file,please check is file exists.");
                }
            }

            /// <summary>
            /// Serializate class object to xml document
            /// </summary>
            /// <typeparam name="T">The class type which need to serializate</typeparam>
            /// <param name="obj">The class object which need to serializate</param>
            /// <param name="outPutFilePath">The xml path where need to save the result</param>
            /// <returns>run result</returns>
            public static bool Serialization<T>(T obj, string outPutFilePath)
            {
                //Declare a boolean value to mark the run result
                bool result = true;
                //Declare a xml writer
                XmlWriter writer = null;
                XmlSerializerNamespaces nameSpace;
                MemoryStream ms = new MemoryStream();
                try
                {

                    //create a stream which write data to xml document.
                    writer = XmlWriter.Create(outPutFilePath, new XmlWriterSettings
                    {
                        //set xml document style - auto create new line
                        Indent = true,
                        //set xml has no declaration
                        //OmitXmlDeclaration = true,
                        DoNotEscapeUriAttributes = true,
                        NamespaceHandling = NamespaceHandling.OmitDuplicates,
                        Encoding = Encoding.UTF8
                    });
                    nameSpace = new XmlSerializerNamespaces();
                    nameSpace.Add("", "");
                }
                //if some error occured,throw it
                catch (ArgumentException error)
                {
                    result = false;
                    throw error;
                }
                //declare a serializer.
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                try
                {
                    //Serializate the object
                    serializer.Serialize(writer, obj, nameSpace);

                }
                //if some error occured,throw it
                catch (InvalidOperationException error)
                {
                    result = false;
                    throw error;
                }
                //At finally close all resource
                finally
                {
                    //close xml stream
                    writer.Close();
                }
                return result;
            }

            public static string SerializationWithoutNameSpaceAndDeclare<T>(T obj)
            {
                //Declare a boolean value to mark the run result
                string result = string.Empty;
                //Declare a xml writer
                XmlWriter writer = null;
                XmlSerializerNamespaces nameSpace;
                MemoryStream ms = new MemoryStream();
                try
                {

                    //create a stream which write data to xml document.
                    writer = XmlWriter.Create(ms, new XmlWriterSettings
                    {
                        //set xml document style - auto create new line
                        Indent = true,
                        //set xml has no declaration
                        OmitXmlDeclaration = true,
                        DoNotEscapeUriAttributes = true,
                        NamespaceHandling = NamespaceHandling.OmitDuplicates
                    });
                    nameSpace = new XmlSerializerNamespaces();
                    nameSpace.Add("", "");
                }
                //if some error occured,throw it
                catch (ArgumentException error)
                {
                    throw error;
                }
                //declare a serializer.
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                try
                {
                    //Serializate the object
                    serializer.Serialize(writer, obj, nameSpace);
                    return Encoding.UTF8.GetString(ms.GetBuffer());
                }
                //if some error occured,throw it
                catch (InvalidOperationException error)
                {
                    throw error;
                }
                //At finally close all resource
                finally
                {
                    //close xml stream
                    writer.Close();

                    ms.Close();
                }
            }

            /// <summary>
            /// Serializate class object to string
            /// </summary>
            /// <typeparam name="T">The class type which need to serializate</typeparam>
            /// <param name="obj">The class object which need to serializate</param>
            /// <param name="outPutFilePath">The xml path where need to save the result</param>
            /// <returns>run result</returns>
            public static string Serialization<T>(T obj)
            {
                //Declare a boolean value to mark the run result
                string result = string.Empty;
                //Declare a MemoryStream to save result
                MemoryStream stream = null;
                try
                {
                    //create a memorystream which write data to memory.
                    stream = new MemoryStream();
                }
                //if some error occured,throw it
                catch (ArgumentException error)
                {
                    throw error;
                }
                //declare a serializer.
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                try
                {
                    //Serializate the object
                    serializer.Serialize(stream, obj);
                    result = Encoding.UTF8.GetString(stream.ToArray());
                }
                //if some error occured,throw it
                catch (InvalidOperationException error)
                {
                    throw error;
                }
                //At finally close all resource
                finally
                {
                    //close xml stream
                    stream.Close();
                }
                return result;
            }
        }
        #endregion

        #region AppConfig Ex
        public static void UpdateAppConfig(string newKey, string newValue)
        {
            bool isModified = false;
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);

            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == newKey)
                {
                    isModified = true;
                }
            }

            if (isModified) config.AppSettings.Settings.Remove(newKey);
            config.AppSettings.Settings.Add(newKey, newValue);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static string GetAppConfig(string strKey)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == strKey)
                {
                    return config.AppSettings.Settings[strKey].Value;
                }
            }
            return null;
        }

        #endregion
    }

    public class XQuerySite : INotifyPropertyChanged
    {
        private string _qName;
        public string QName
        {
            get { return _qName; }
            set
            {
                _qName = value;
                OnPropertyChanged(nameof(QName));
                _qUri = new Uri(ConfigurationManager.AppSettings[QName]);
                OnPropertyChanged(nameof(QUri));
            }
        }
        private Uri _qUri;
        public Uri QUri { get { return _qUri; } set { _qUri = value; OnPropertyChanged(nameof(QUri)); } }

        public XQuerySite()
        {
            QName = "BSite";
            QUri = new Uri(ConfigurationManager.AppSettings[QName]);
        }
        public XQuerySite(string QueryName)
        {
            QName = QueryName;
            QUri = new Uri(ConfigurationManager.AppSettings[QName]);
        }

        public void ChangeTo(string QueryName)
        {
            QName = QueryName;
            QUri = new Uri(ConfigurationManager.AppSettings[QName]);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
