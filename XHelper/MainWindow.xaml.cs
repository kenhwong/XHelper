using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public MainWindow()
        {
            InitializeComponent();
            InitializeCommand();

            DataContext = this;
        }

        public RoutedCommand cmdNMBrowse = new RoutedCommand("CommandNewMovieBrowse", typeof(MainWindow));
        private void InitializeCommand()
        {
            var cbQuit = new CommandBinding(ApplicationCommands.Close,
                (sender, e) => { Application.Current.Shutdown(); e.Handled = true; },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });
            CommandBinding cbNMBrowse = new CommandBinding(cmdNMBrowse,
                (sender, e) =>
                {
                    //IsReadyProcess = false;
                    NewMovieInfo = new MovieInfo();

                    Microsoft.Win32.OpenFileDialog ofd_selectmovie = new Microsoft.Win32.OpenFileDialog();
                    ofd_selectmovie.Filter = "Video files|*.avi;*.wmv;*.mp4;*.m4v;*.asf；*.asx;*.rm;*.rmvb;*.mpg;*.mpeg;*.mpe;*.3gp;*.mov;*.dat;*.mkv;*.flv;*.vob";
                    Nullable<bool> result = ofd_selectmovie.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        txtNMFullFileName.Text = ofd_selectmovie.FileName;
                        Func_OpenNewMovieFile(ofd_selectmovie.FileName);
                    }
                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });

            bnNMBrowse.Command = cmdNMBrowse;

            this.CommandBindings.AddRange(new CommandBinding[] { cbNMBrowse, cbQuit });

        }


        private void Func_OpenNewMovieFile(string _filename)
        {
            NewMovieInfo = new MovieInfo(_filename);
            /**
            //mi_current.SourceMediaFileExt = Path.GetExtension(_filename);
            //mi_current.TotalMoviesSize = XHelper.IOHelper_GetMoviesTotalSize(mi_current.SourcePath, mi_current.SourceMediaFileExt);

            txt_Keywords.Items.Clear();

            Match _match_heyzo = Regex.Match(_filename, @"hey.*?(\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_caribbean = Regex.Match(_filename, @"carib.*?(\d+[_-]\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_1pondo = Regex.Match(_filename, @"1pondo.*?(\d+[_-]\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_nums = Regex.Match(_filename, @"(\d{6,}[_-]\d{3,})", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_3d2d = Regex.Match(_filename, @"([a-z|A-Z]+3d(?:2d)?[a-z|A-Z]*\-?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_bd = Regex.Match(_filename, @"([a-z|A-Z]+bd\-?[a-z|A-Z]?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_tokyohot = Regex.Match(_filename, @"(n\d{4,})", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            //(ps\d|set|lfm|pss|lovu|xms|ykt|swm)_\d+?_[a-z]+\d*
            Match _match_scute1 = Regex.Match(_filename, @"((?:\d+?|set)_[a-z]+?_\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _match_scute2 = Regex.Match(_filename, @"((?:ps\d|set|lfm|pss|lovu|xms|ykt|swm)_\d+?_[a-z]+\d*)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            bn_SearchSOX.UpdateDefaultStyle();
            bn_SearchMOO.UpdateDefaultStyle();

            var greenbrush = new SolidColorBrush(Colors.Green);
            if (_match_heyzo.Success)
            {
                txt_Keywords.Items.Add("heyzo " + _match_heyzo.Groups[1].Value);
                bn_SearchSOX.Foreground = greenbrush;
                IsCensored = false;
            }
            else if (_match_caribbean.Success)
            {
                txt_Keywords.Items.Add(_match_caribbean.Groups[1].Value);
                bn_SearchSOX.Foreground = new SolidColorBrush(Colors.Green);
                bn_SearchMOO.UpdateDefaultStyle();
            }
            else if (_match_1pondo.Success)
            {
                txt_Keywords.Items.Add(_match_1pondo.Groups[1].Value);
                bn_SearchSOX.Foreground = greenbrush;
                IsCensored = false;
            }
            else if (_match_nums.Success)
            {
                txt_Keywords.Items.Add(_match_nums.Groups[1].Value);
                bn_SearchSOX.Foreground = greenbrush;
                IsCensored = false;
            }
            else if (_match_3d2d.Success)
            {
                txt_Keywords.Items.Add(_match_3d2d.Groups[1].Value);
                bn_SearchSOX.Foreground = greenbrush;
                IsCensored = false;
            }
            else if (_match_bd.Success)
            {
                txt_Keywords.Items.Add(_match_bd.Groups[1].Value);
                bn_SearchSOX.Foreground = greenbrush;
                IsCensored = false;
            }
            else if (_match_tokyohot.Success)
            {
                txt_Keywords.Items.Add(_match_tokyohot.Groups[1].Value);
                bn_SearchSOX.Foreground = greenbrush;
                IsCensored = false;
            }
            else
            {
                //MatchCollection _mc = Regex.Matches(System.IO.Path.GetFileNameWithoutExtension(ofd.FileName), @"([a-z|A-Z]+\-?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                MatchCollection _mc = Regex.Matches(_filename, @"([a-z|A-Z]+\-?\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match _m in _mc)
                {
                    if (_m.Groups[1].Value == "whole1") continue;
                    if (_m.Groups[1].Value == "hd1") continue;
                    if (_m.Groups[1].Value == "mp4") continue;
                    txt_Keywords.Items.Add(Regex.Replace(_m.Groups[1].Value, @"0+", "0"));
                }
                //_mc = Regex.Matches(System.IO.Path.GetFileNameWithoutExtension(ofd.FileName), @"(\d+[_-]\d+)|(\d+_[a-z|A-Z]+_\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                _mc = Regex.Matches(_filename, @"(\d+[_-]\d+)|(\d+_[a-z|A-Z]+_\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match _m in _mc) txt_Keywords.Items.Add(_m.Groups[1].Value);
                bn_SearchSOX.UpdateDefaultStyle();
                bn_SearchMOO.Foreground = greenbrush;
            }
            txt_Keywords.SelectedIndex = 0;

            InitializeUIControls();
            **/
        }


        private void listInformation_SelectionChanged(object sender, SelectionChangedEventArgs e) { var lb = sender as ListBox; lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]); }
    }
}
