using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private MovieInfo _newMovieInfo = new MovieInfo();
        public MovieInfo NewMovieInfo { get { return _newMovieInfo; } set { _newMovieInfo = value; OnPropertyChanged(nameof(NewMovieInfo)); } }
        private ObservableCollection<StarInfo> _newMovieStars = new ObservableCollection<StarInfo>();
        public ObservableCollection<StarInfo> NewMovieStars { get { return _newMovieStars; } set { _newMovieStars = value; OnPropertyChanged(nameof(NewMovieStars)); } }
        private XQuerySite _newMovieQuerySite = new XQuerySite();
        public XQuerySite NewMovieQuerySite { get { return _newMovieQuerySite; } set { _newMovieQuerySite = value; OnPropertyChanged(nameof(NewMovieQuerySite)); } }
        private HtmlDocument _currentHtmlDocument = new HtmlDocument();
        public HtmlDocument CurrentHtmlDocument { get { return _currentHtmlDocument; } set { _currentHtmlDocument = value; OnPropertyChanged(nameof(CurrentHtmlDocument)); } }
        private ObservableCollection<QueryResultMovieInfo> _newMovieQueryResult = new ObservableCollection<QueryResultMovieInfo>();
        public ObservableCollection<QueryResultMovieInfo> NewMovieQueryResult { get { return _newMovieQueryResult; } set { _newMovieQueryResult = value; OnPropertyChanged(nameof(NewMovieQueryResult)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public MainWindow()
        {
            InitializeComponent();
            InitializeCommand();

            DataContext = this;
        }

        public RoutedCommand cmdNMBrowse = new RoutedCommand();
        public RoutedCommand cmdNMQuery = new RoutedCommand();
        public RoutedCommand CommandNMQueryResultItemSolve { get; set; } = new RoutedCommand();
        public RoutedCommand CommandNMRecordSolve { get; set; } = new RoutedCommand();
        private void InitializeCommand()
        {
            var cbQuit = new CommandBinding(ApplicationCommands.Close,
                (sender, e) => { Application.Current.Shutdown(); e.Handled = true; },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });
            CommandBinding cbNMBrowse = new CommandBinding(cmdNMBrowse,
                (sender, e) =>
                {
                    NewMovieInfo = new MovieInfo();

                    Microsoft.Win32.OpenFileDialog ofd_selectmovie = new Microsoft.Win32.OpenFileDialog();
                    ofd_selectmovie.Filter = "Video files|*.avi;*.wmv;*.mp4;*.m4v;*.asf；*.asx;*.rm;*.rmvb;*.mpg;*.mpeg;*.mpe;*.3gp;*.mov;*.dat;*.mkv;*.flv;*.vob";
                    Nullable<bool> result = ofd_selectmovie.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        txtNMFullFileName.Text = ofd_selectmovie.FileName;
                        Func_NMOpenMediaFile(ofd_selectmovie.FileName);
                    }
                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });
            CommandBinding cbNMQuery = new CommandBinding(cmdNMQuery,
                (sender, e) =>
                {
                    switch (NewMovieQuerySite.QName)
                    {
                        case "CSite":
                        case "USite":
                            Func_NMBRecordAsync(NewMovieQuerySite.QName, txtNMKeyword.Text);
                            break;
                        case "BSite":
                            Func_NMBRecordAsync(NewMovieQuerySite.QName, txtNMKeyword.Text);
                            break;
                        case "LSite":
                            Func_NMLRecordAsync(NewMovieQuerySite.QName, txtNMKeyword.Text);
                            break;
                        default:
                            break;
                    }

                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });
            CommandBinding cbNMQueryResultItemSolve = new CommandBinding(CommandNMQueryResultItemSolve,
                (sender, e) =>
                {
                    switch (NewMovieQuerySite.QName)
                    {
                        case "CSite":
                        case "USite":
                            Func_NMResolveUCResult(e.Parameter as string);
                            break;
                        case "BSite":
                            Func_NMResolveBResult(e.Parameter as string);
                            break;
                        case "LSite":
                            Func_NMResolveLResult(e.Parameter as string);
                            break;
                        default:
                            break;
                    }

                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });
            CommandBinding cbNMRecordSolve = new CommandBinding(CommandNMRecordSolve,
                (sender, e) =>
                {

                    MessageBox.Show(e.Parameter as string);
                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });

            bnNMBrowse.Command = cmdNMBrowse;
            bnNMQuery.Command = cmdNMQuery;

            this.CommandBindings.AddRange(new CommandBinding[] { cbNMBrowse, cbQuit, cbNMQuery, cbNMQueryResultItemSolve, cbNMRecordSolve });

        }

        private async void Func_NMResolveLResult(string v)
        {
            listInformation.SelectedIndex = listInformation.Items.Add($"READ: {v}...");
            NewMovieInfo.OfficialWeb = new Uri(v);
            var streamresult = (await XService.Func_Net_ReadWebData(NewMovieInfo.OfficialWeb)).response;

            CurrentHtmlDocument.LoadHtml(streamresult.Replace(Environment.NewLine, " ").Replace("\t", " "));
            HtmlNode hnode = CurrentHtmlDocument.DocumentNode.SelectSingleNode("//div[@id='rightcolumn']");

            NewMovieInfo.ReleaseName = hnode.SelectSingleNode("//div[@id='video_title']/h3[1]").InnerText;
            var cvurl = hnode.SelectSingleNode("//img[@id='video_jacket_img']").Attributes["src"].Value;
            if (cvurl.StartsWith("//")) cvurl = $"http:{cvurl}"; ;
            NewMovieInfo.CoverWebUrl = cvurl;

            hnode = hnode.SelectSingleNode("//table[@id='video_jacket_info']");
            //Resolve movie data
            Func_NMAnalysisLMovie(hnode);
            //處理 Stars 段
            if (!Func_NMAnalysisLStars(hnode)) return;

            //處理 Sample Images 段
            //if (!await Func_AnalysisMovie_Samples(hnode)) return;

        }

        private Task<bool> Func_NMAnalysisLCover(HtmlNode hnode)
        {
            throw new NotImplementedException();
        }

        private async Task<bool> Func_AnalysisMovie_Cover(HtmlNode sourcenode)
        {
            NewMovieInfo.CoverWebUrl = XService.UrlCheck(sourcenode.SelectSingleNode("//a[@class='bigImage']/img").Attributes["src"].Value);
            Uri coveruri = new Uri(NewMovieInfo.CoverWebUrl);
            NewMovieInfo.CoverFileName = System.IO.Path.Combine(NewMovieInfo.SourcePath.FullName, coveruri.Segments[coveruri.Segments.Length - 1]);

            listInformation.SelectedIndex = listInformation.Items.Add($"創建影片封面 {NewMovieInfo.CoverFileName} ...");
            var coverimage = new ImageSourceConverter().ConvertFrom(coveruri);
            
            using (Stream temp = await XService.Func_Net_ReadWebStream(coveruri, NewMovieInfo.OfficialWeb))
            {
                if (temp != Stream.Null)
                {
                    if (File.Exists(NewMovieInfo.CoverFileName)) File.Delete(NewMovieInfo.CoverFileName);
                    using (FileStream sourceStream = new FileStream(
                        NewMovieInfo.CoverFileName,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        useAsync: true))
                    {
                        temp.Seek(0, SeekOrigin.Begin);
                        await temp.CopyToAsync(sourceStream);
                        await sourceStream.FlushAsync();
                    }
                }
            }
            return true;
        }


        private void Func_NMResolveUCResult(string v)
        {
            throw new NotImplementedException();
        }

        private async void Func_NMResolveBResult(string v)
        {
            listInformation.SelectedIndex = listInformation.Items.Add($"READ: {v}...");
            NewMovieInfo.OfficialWeb = new Uri(v);
            var streamresult = (await XService.Func_Net_ReadWebData(NewMovieInfo.OfficialWeb)).response;

            CurrentHtmlDocument.LoadHtml(streamresult.Replace(Environment.NewLine, " ").Replace("\t", " "));
            HtmlNode hnode = CurrentHtmlDocument.DocumentNode;

            HtmlNode _errornode = hnode.SelectSingleNode("//div[@class='alert alert-block alert-error']");
            if (_errornode != null)
            {
                listInformation.SelectedIndex = listInformation.Items.Add($"ERR: {v}...");
                return;
            }

            switch (NewMovieQuerySite.QName)
            {
                case "CSite":
                case "USite":
                    hnode = hnode.SelectSingleNode("/html/body/div[2]");
                    break;
                case "BSite":
                    hnode = hnode.SelectSingleNode("/html/body/div[@class='container']");
                    break;
                case "LSite":
                    break;
                default:
                    break;
            }

            foreach (HtmlNode _txt_node in hnode.SelectNodes(".//text()"))
                if (!Regex.IsMatch(_txt_node.InnerText, @"\S", RegexOptions.Singleline)) _txt_node.Remove();

            NewMovieInfo.ReleaseName = hnode.SelectSingleNode("//h3[1]").InnerText;
            //TODO: LINE 180
            Func_AnalysisMovie(hnode);

            //刪除重複 Mov            
            if (XGlobal.CurrentContext.TotalMovies.Exists(m => m.ReleaseID == NewMovieInfo.ReleaseID))
            {
                if (MessageBox.Show(
                    String.Format(
                    "影片 [{0}] 已有歸類存檔({1}*{2}, {3})，操作取消，刪除影片文件嗎？",
                    NewMovieInfo.ReleaseID,
                    NewMovieInfo.VWidth,
                    NewMovieInfo.VHeight,
                    XService.Format_MachineSize(NewMovieInfo.MediaFilesTotalSize)),
                    "影片已歸檔",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    XService.DeleteMovies(NewMovieInfo.SourcePath, NewMovieInfo.SourceMediaFileExt);
                    listInformation.SelectedIndex = listInformation.Items.Add($"源目錄 {NewMovieInfo.SourcePath} 已刪除.");
                    return;
                }
                else
                {
                    listInformation.SelectedIndex = listInformation.Items.Add($"影片 [{NewMovieInfo.ReleaseID}] 已有歸類存檔，操作取消.");
                    return;
                }
            }

            //處理 Stars 段
            if (!await Func_AnalysisStars(hnode)) return;

            //處理Cover
            //if (!await Func_AnalysisMovie_Cover(hnode)) return;

            //處理 Sample Images 段
            //if (!await Func_AnalysisMovie_Samples(hnode)) return;
        }

        private void Func_NMAnalysisLMovie(HtmlNode sourcenode)
        {
            listInformation.SelectedIndex = listInformation.Items.Add($"READ: MOVIE INFORMATION");

            HtmlNode info_node = sourcenode.SelectSingleNode("//div[@id='video_info']");
            NewMovieInfo.ReleaseID = info_node.SelectSingleNode("//div[@id='video_id']//td[@class='text']").InnerText;
            NewMovieInfo.ReleaseDate = DateTime.Parse(info_node.SelectSingleNode("//div[@id='video_date']//td[@class='text']").InnerText);
            HtmlNode nrl = info_node.SelectSingleNode("//div[@id='video_length']//td[@class='text']");
            if (nrl == null) nrl = info_node.SelectSingleNode("//div[@id='video_length']//span[@class='text']");
            NewMovieInfo.ReleaseLength = int.Parse(nrl.InnerText);
            NewMovieInfo.ReleaseStudio = info_node.SelectSingleNode("//div[@id='video_maker']//td[@class='text']").InnerText.Replace("&nbsp;", "").Trim();
            NewMovieInfo.ReleaseLabel = info_node.SelectSingleNode("//div[@id='video_label']//td[@class='text']").InnerText.Replace("&nbsp;", "").Trim();
            foreach (var ng in info_node.SelectNodes("//div[@id='video_genres']//span[@class='genre']"))
                NewMovieInfo.Genre.Add(ng.InnerText.Replace("&nbsp;", "").Trim());
        }

        private void Func_AnalysisMovie(HtmlNode sourcenode)
        {
            listInformation.SelectedIndex = listInformation.Items.Add($"READ: {NewMovieInfo.ReleaseID}");

            HtmlNode info_node = sourcenode.SelectSingleNode("//div[@class='col-md-3 info']");
            string mov_desc = info_node.InnerHtml;
            mov_desc = Regex.Replace(mov_desc, @"<span class=""genre"">(.*?)</span>", @"[$1]");
            mov_desc = Regex.Replace(mov_desc, @"<a.*?>(.*?)</a>", "$1");
            mov_desc = Regex.Replace(mov_desc, @"<h\d>(.*?)</h\d>", "$1");
            mov_desc = Regex.Replace(mov_desc, @"<span.*?>(.*?)</span>", "$1");
            mov_desc = Regex.Replace(mov_desc, @"<p.*?>(.*?)</p>", "$1\r\n");

            foreach (HtmlNode _node in info_node.SelectNodes(".//*[@class='header']"))
            {
                /* en site
                if (_node.InnerText.Contains("ID")) txtReleaseID.Text = _node.NextSibling.InnerText;
                else if (_node.InnerText.Contains("Release Date")) txtReleaseDate.Text = _node.ParentNode.InnerText;
                */
                /* tw site
                if (_node.InnerText.Contains("識別碼")) txtReleaseID.Text = _node.NextSibling.InnerText;
                else if (_node.InnerText.Contains("發行日期")) txtReleaseDate.Text = Regex.Replace(_node.ParentNode.InnerText, @"(.*?\:)", "");
                */
                /* ja site */
                if (_node.InnerText.Contains("品番")) NewMovieInfo.ReleaseID = _node.NextSibling.InnerText.Trim();
                else if (_node.InnerText.Contains("発売日")) NewMovieInfo.ReleaseDate = DateTime.Parse(Regex.Replace(_node.ParentNode.InnerText, @"(.*?\:)", ""));
                else if (_node.InnerText.Contains("収録時間")) NewMovieInfo.ReleaseLength = int.Parse(Regex.Match(_node.ParentNode.InnerText, @"(\d+)").Groups[1].Value);
                else if (_node.InnerText.Contains("メーカー")) NewMovieInfo.ReleaseStudio = _node.NextSibling.InnerText.Trim();
                else if (_node.InnerText.Contains("レーベル")) NewMovieInfo.ReleaseLabel = _node.NextSibling.InnerText.Trim();
                else if (_node.InnerText.Contains("ジャンル"))
                {
                    var nodes_genre = _node.NextSibling.SelectNodes(".//a");
                    foreach (var node in nodes_genre) NewMovieInfo.Genre.Add(node.InnerText.Trim());
                }

            }
        }

        private bool Func_NMAnalysisLStars(HtmlNode hnode)
        {
            NewMovieStars.Clear();
            listInformation.SelectedIndex = listInformation.Items.Add("READ: STARS...");
            HtmlNode ncast = hnode.SelectSingleNode("//div[@id='video_cast']");

            string jname = string.Empty;
            NewMovieInfo.ActorUIDs = new List<Guid>();

            if (!ncast.SelectSingleNode("//td[@class='text']").HasChildNodes)
            {
                MessageBox.Show($"No cast information from {NewMovieQuerySite.QName.ToUpper()}.\nTry other web source.", "Expect Cast Data");
                return false;
            }

            HtmlNodeCollection ncasts = ncast.SelectNodes("//span[@class='star']/a");
            foreach (HtmlNode _nc in ncasts)
            {
                bool is_star_stored = true;
                jname = XService.IllegalFiltered(_nc.InnerText.Replace("&nbsp;", "").Trim());
                StarInfo star = XGlobal.CurrentContext.TotalStars.Find(s => s.JName == jname);
                if (star is null)
                {
                    star = new StarInfo(jname);
                    is_star_stored = false;
                }

                star.CreateLocalStarDirectory(NewMovieInfo);
                listInformation.SelectedIndex = listInformation.Items.Add($"CREATE: DIR/{System.IO.Path.Combine(NewMovieInfo.SourcePath.Root.FullName, ConfigurationManager.AppSettings["ArchiveName"], star.JName)}");

                star.StoredMovieIDs.Add(NewMovieInfo.ReleaseID);
                NewMovieStars.Add(star);
                NewMovieInfo.ActorUIDs.Add(star.UniqueID);
                if (!is_star_stored) XGlobal.CurrentContext.TotalStars.Add(star);
            }

            return true;
        }

        private async Task<bool> Func_AnalysisStars(HtmlNode sourcenode)
        {
            NewMovieStars.Clear();
            listInformation.SelectedIndex = listInformation.Items.Add("READ: STARS...");

            HtmlNodeCollection _hncc = sourcenode.SelectNodes("//a[@class='avatar-box']");

            string name_ja = string.Empty;
            NewMovieInfo.ActorUIDs = new List<Guid>();

            if (_hncc == null)
            {
                MessageBox.Show($"No movie information from{NewMovieQuerySite.QName.ToUpper()}.\nTry other web source.", "數據缺損");
                //return false;
            }
            else
                foreach (HtmlNode _node in _hncc)
                {
                    bool is_star_stored = true;
                    name_ja = XService.IllegalFiltered(_node.SelectSingleNode(".//span").InnerText.Trim());
                    StarInfo star = XGlobal.CurrentContext.TotalStars.Find(s => s.JName == name_ja);
                    if (star is null)
                    {
                        star = new StarInfo(name_ja);
                        is_star_stored = false;
                    }

                    //Read Avator to Stream                    
                    listInformation.SelectedIndex = listInformation.Items.Add($"CREATE: DIR/{star.JName}/TEMP");

                    star.AvatorWebUri = new Uri(XService.UrlCheck(_node.SelectSingleNode(".//img").Attributes["src"].Value));
                    //star.CreateStarDirectoryTemp();
                    star.CreateLocalStarDirectory(NewMovieInfo);

                    Stream temp = await XService.Func_Net_ReadWebStream(star.AvatorWebUri, NewMovieInfo.OfficialWeb);
                    star.AvatorFileName = System.IO.Path.Combine(star.DirStored.FullName, star.AvatorWebUri.Segments[star.AvatorWebUri.Segments.Length - 1]);

                    using (FileStream sourceStream = new FileStream(star.AvatorFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        await temp.CopyToAsync(sourceStream);
                        await sourceStream.FlushAsync();
                    }

                    //list_CurrentStars.Images.Add(Image.FromStream(temp));

                    star.StoredMovieIDs.Add(NewMovieInfo.ReleaseID);
                    NewMovieStars.Add(star);
                    NewMovieInfo.ActorUIDs.Add(star.UniqueID);
                    if (!is_star_stored) XGlobal.CurrentContext.TotalStars.Add(star);
                }

            return true;
        }

        private async void Func_NMLRecordAsync(string qName, string keyword)
        {
            //XGlobal.RebuildSubDirTemp();

            listInformation.SelectedIndex = listInformation.Items.Add($"QUERY: {NewMovieQuerySite.QUri.Host.ToUpper()} / KEY: {keyword}...");

            Uri uri_search = new Uri($"{NewMovieQuerySite.QUri}ja/vl_searchbyid.php?keyword={WebUtility.UrlEncode(keyword.Trim())}");
            var webdata = await XService.Func_Net_ReadWebData(uri_search);
            var streamresult = webdata.response;
            var request = webdata.requestmessage;

            if (streamresult.Contains("System.Net.Http.HttpRequestException:"))
            {
                System.Diagnostics.Debug.WriteLine(uri_search.ToString());
                System.Diagnostics.Debug.WriteLine(streamresult);
                return;
            }

            if (streamresult == HttpStatusCode.NotFound.ToString())
            {
                MessageBox.Show("404", "Key Words Mismatch");
                txtNMKeyword.Focus();
                return;
            }

            CurrentHtmlDocument.LoadHtml(streamresult);
            HtmlNode hnode = CurrentHtmlDocument.DocumentNode;
            HtmlNode _errornode = null;
            _errornode = hnode.SelectSingleNode("//div[@id='rightcolumn']//em");


            if (_errornode != null && _errornode.InnerText.Contains("ご指定の検索条件に合う項目がありませんでした"))
            {
                gbNMQueryResult.Header = $"Query Result: ご指定の検索条件に合う項目がありませんでした";
                MessageBox.Show(_errornode.InnerText, "Key Words Mismatch");
                txtNMKeyword.Focus();
                return;
            }

            if (request.RequestUri.PathAndQuery.Contains("searchbyid"))
            {
                //Records list
                #region 讀取搜索結果
                NewMovieQueryResult.Clear();
                HtmlNodeCollection node_results = CurrentHtmlDocument.DocumentNode.SelectNodes("//div[@class='videothumblist']//div[@class='video']");
                gbNMQueryResult.Header = $"Query Result: {NewMovieQuerySite.QUri.Host.ToUpper()} / {keyword} / {node_results.Count}";

                foreach (HtmlNode _node in node_results)
                {
                    Stream tempimg = await XService.Func_Net_ReadWebStream(_node.SelectSingleNode(".//img").Attributes["src"].Value, uri_search);
                    QueryResultMovieInfo mi = new QueryResultMovieInfo();
                    mi.ReleaseID = _node.SelectSingleNode(".//div[@class='id']").InnerText.Trim();
                    mi.ReleaseName = _node.SelectSingleNode(".//div[@class='title']").InnerText.Trim();
                    mi.MovieCoverImage = new ImageSourceConverter().ConvertFrom(tempimg) as ImageSource;
                    string reurl = _node.SelectSingleNode(".//a[1]").Attributes["href"].Value;
                    if (reurl.StartsWith("./")) reurl = reurl.Replace("./", NewMovieQuerySite.QUri.ToString());
                    mi.OfficialWeb = new Uri(reurl);
                    NewMovieQueryResult.Add(mi);
                }//end foreach in node_results
                #endregion

            }
            else
            {
                Func_NMResolveLResult(request.RequestUri.OriginalString);
            }
        }

        private async void Func_NMBRecordAsync(string qName, string keyword)
        {
            //XGlobal.RebuildSubDirTemp();

            listInformation.SelectedIndex = listInformation.Items.Add($"QUERY: {NewMovieQuerySite.QUri.Host.ToUpper()} / KEY: {keyword}...");

            Uri uri_search = new Uri($"{NewMovieQuerySite.QUri}ja/search/{WebUtility.UrlEncode(keyword.Trim())}");
            var streamresult = (await XService.Func_Net_ReadWebData(uri_search)).response;

            if (streamresult.Contains("System.Net.Http.HttpRequestException:"))
            {
                System.Diagnostics.Debug.WriteLine(uri_search.ToString());
                System.Diagnostics.Debug.WriteLine(streamresult);
                return;
            }

            if (streamresult == HttpStatusCode.NotFound.ToString())
            {
                MessageBox.Show("404", "Key Words Mismatch");
                txtNMKeyword.Focus();
                return;
            }

            CurrentHtmlDocument.LoadHtml(streamresult);
            HtmlNode hnode = CurrentHtmlDocument.DocumentNode;
            HtmlNode _errornode = null;
            switch (NewMovieQuerySite.QName)
            {
                case "CSite":
                case "USite":
                    _errornode = hnode.SelectSingleNode("//div[@class='alert alert-block alert-error']");
                    break;
                case "BSite":
                    _errornode = hnode.SelectSingleNode("//div[@class='alert alert-danger alert-page']");
                    break;
                case "LSite":
                    break;
                default:
                    break;
            }

            if (_errornode != null)
            {
                gbNMQueryResult.Header = $"Query Result: {_errornode.SelectSingleNode("./h4").InnerText}";
                MessageBox.Show(_errornode.InnerText, "Key Words Mismatch");
                txtNMKeyword.Focus();
                return;
            }


            #region 讀取搜索結果
            NewMovieQueryResult.Clear();
            HtmlNodeCollection node_results = CurrentHtmlDocument.DocumentNode.SelectNodes("//div[@class='item']");
            gbNMQueryResult.Header = $"Query Result: {NewMovieQuerySite.QUri.Host.ToUpper()} / {keyword} / {node_results.Count}";

            foreach (HtmlNode _node in node_results)
            {
                Stream tempimg = await XService.Func_Net_ReadWebStream(_node.SelectSingleNode(".//img").Attributes["src"].Value, uri_search);
                QueryResultMovieInfo mi = new QueryResultMovieInfo();
                mi.ReleaseID = _node.SelectSingleNode(".//date[1]").InnerText;
                _node.SelectSingleNode(".//span[1]/date[1]").Remove();
                mi.ReleaseDate = Convert.ToDateTime(_node.SelectSingleNode(".//date[1]").InnerText);
                _node.SelectSingleNode(".//span[1]/date[1]").Remove();
                _node.SelectSingleNode(".//span[1]/div[1]").Remove();
                mi.ReleaseName = _node.SelectSingleNode(".//span[1]").InnerText.Trim(new char[] { ' ', '/' });
                //gi.Glyph = new BitmapImage() { StreamSource = tempimg };
                mi.MovieCoverImage = new ImageSourceConverter().ConvertFrom(tempimg) as ImageSource;
                mi.OfficialWeb = new Uri(XService.UrlCheck(_node.SelectSingleNode(".//a[1]").Attributes["href"].Value));
                NewMovieQueryResult.Add(mi);
            }
            #endregion
            /**
                        **/
        }

        #region Function: Add New Movie, Open Media File
        /// <summary>
        /// Function: Add New Movie, Open Media File
        /// </summary>
        /// <param name="_filename">New media file full name</param>
        private void Func_NMOpenMediaFile(string _filename)
        {
            NewMovieInfo = new MovieInfo(_filename);
            NewMovieInfo.SourceMediaFileExt = System.IO.Path.GetExtension(_filename);
            txtNMKeyword.Items.Clear();
            NewMovieQuerySite = new XQuerySite("LSite");

            Match _match_heyzo = Regex.Match(_filename, @"hey.*?(\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_caribbean = Regex.Match(_filename, @"carib.*?(\d+[_-]\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_1pondo = Regex.Match(_filename, @"1pondo.*?(\d+[_-]\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_nums = Regex.Match(_filename, @"(\d{6,}[_-]\d{3,})", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_3d2d = Regex.Match(_filename, @"([a-z|A-Z]+3d(?:2d)?[a-z|A-Z]*\-?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_bd = Regex.Match(_filename, @"([a-z|A-Z]+bd\-?[a-z|A-Z]?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_tokyohot = Regex.Match(_filename, @"(n\d{4,})", RegexOptions.Singleline | RegexOptions.IgnoreCase);


            if (_match_heyzo.Success) { txtNMKeyword.Items.Add("heyzo " + _match_heyzo.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else if (_match_caribbean.Success) { txtNMKeyword.Items.Add(_match_caribbean.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else if (_match_1pondo.Success) { txtNMKeyword.Items.Add(_match_1pondo.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else if (_match_nums.Success) { txtNMKeyword.Items.Add(_match_nums.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else if (_match_3d2d.Success) { txtNMKeyword.Items.Add(_match_3d2d.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else if (_match_bd.Success) { txtNMKeyword.Items.Add(_match_bd.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else if (_match_tokyohot.Success) { txtNMKeyword.Items.Add(_match_tokyohot.Groups[1].Value); NewMovieQuerySite.ChangeTo("BSite"); }
            else
            {
                MatchCollection _mc = Regex.Matches(_filename, @"([a-z|A-Z]+\-?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match _m in _mc)
                {
                    if (_m.Groups[1].Value == "whole1") continue;
                    if (_m.Groups[1].Value == "hd1") continue;
                    if (_m.Groups[1].Value == "mp4") continue;
                    txtNMKeyword.Items.Add(Regex.Replace(_m.Groups[1].Value, @"0+", "0"));
                }
                _mc = Regex.Matches(_filename, @"(\d+[_-]\d+)|(\d+_[a-z|A-Z]+_\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match _m in _mc) txtNMKeyword.Items.Add(_m.Groups[1].Value);
                NewMovieQuerySite.ChangeTo("LSite");
            }
            txtNMKeyword.SelectedIndex = 0;
        }
        #endregion

        private void listInformation_SelectionChanged(object sender, SelectionChangedEventArgs e) { var lb = sender as ListBox; lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]); }

    }
}
