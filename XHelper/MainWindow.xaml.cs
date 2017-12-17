using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public RoutedCommand cmdNMBrowse = new RoutedCommand("CommandNewMovieBrowse", typeof(MainWindow));
        public RoutedCommand cmdNMQuery = new RoutedCommand("CommandNewMovieQuery", typeof(MainWindow));
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
                    Func_NMQueryRecordAsync(NewMovieQuerySite.QName, txtNMKeyword.Text);
                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });


            bnNMBrowse.Command = cmdNMBrowse;
            bnNMQuery.Command = cmdNMQuery;

            this.CommandBindings.AddRange(new CommandBinding[] { cbNMBrowse, cbQuit, cbNMQuery });

        }

        private async void Func_NMQueryRecordAsync(string qName, string keyword)
        {
            //XGlobal.RebuildSubDirTemp();

            listInformation.SelectedIndex = listInformation.Items.Add($"QUERY: {NewMovieQuerySite.QUri.Host.ToUpper()} / KEY: {keyword}...");

            Uri uri_search = new Uri($"{NewMovieQuerySite.QUri}ja/search/{WebUtility.UrlEncode(keyword.Trim())}");
            var streamresult = await XService.Func_Net_ReadWebData(uri_search);

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

                //gi.Command = Command_SelectResult;
                //gi.CommandTarget = list_ProcessInformation;
                //gi.CommandParameter = gi;
            }//end foreach in node_results
            //lbNMQueryResult.ItemsSource = null;
            //lbNMQueryResult.ItemsSource = NewMovieQueryResult;
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
            NewMovieQuerySite = new XQuerySite("BSite");

            Match _match_heyzo = Regex.Match(_filename, @"hey.*?(\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_caribbean = Regex.Match(_filename, @"carib.*?(\d+[_-]\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_1pondo = Regex.Match(_filename, @"1pondo.*?(\d+[_-]\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_nums = Regex.Match(_filename, @"(\d{6,}[_-]\d{3,})", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_3d2d = Regex.Match(_filename, @"([a-z|A-Z]+3d(?:2d)?[a-z|A-Z]*\-?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_bd = Regex.Match(_filename, @"([a-z|A-Z]+bd\-?[a-z|A-Z]?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_tokyohot = Regex.Match(_filename, @"(n\d{4,})", RegexOptions.Singleline | RegexOptions.IgnoreCase);


            if (_match_heyzo.Success) { txtNMKeyword.Items.Add("heyzo " + _match_heyzo.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
            else if (_match_caribbean.Success) { txtNMKeyword.Items.Add(_match_caribbean.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
            else if (_match_1pondo.Success) { txtNMKeyword.Items.Add(_match_1pondo.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
            else if (_match_nums.Success) { txtNMKeyword.Items.Add(_match_nums.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
            else if (_match_3d2d.Success) { txtNMKeyword.Items.Add(_match_3d2d.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
            else if (_match_bd.Success) { txtNMKeyword.Items.Add(_match_bd.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
            else if (_match_tokyohot.Success) { txtNMKeyword.Items.Add(_match_tokyohot.Groups[1].Value); NewMovieQuerySite.ChangeTo("USite"); }
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
                NewMovieQuerySite.ChangeTo("CSite");
            }
            txtNMKeyword.SelectedIndex = 0;
        }
        #endregion

        private void listInformation_SelectionChanged(object sender, SelectionChangedEventArgs e) { var lb = sender as ListBox; lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]); }

    }
}
