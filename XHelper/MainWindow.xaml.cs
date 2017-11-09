using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeCommand();
        }

        public RoutedCommand cmdLoadData = new RoutedCommand("CommandLoadData", typeof(MainWindow));
        private void InitializeCommand()
        {
            var cbQuit = new CommandBinding(ApplicationCommands.Close,
                (sender, e) => { Application.Current.Shutdown(); e.Handled = true; },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });
            CommandBinding cbLoadData = new CommandBinding(cmdLoadData,
                (sender, e) =>
                {
                    Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                    //ofd.InitialDirectory = System.IO.Path.GetDirectoryName(XGlobal.DataFile);
                    ofd.DefaultExt = "*.xdata";
                    ofd.Filter = "Data Files(*.data)|*.data|Data Backup Files(*.bak)|*.bak|All(*.*)|*.*";
                    if (ofd.ShowDialog() == true)
                        if (MessageBox.Show($"Load Data File {ofd.FileName}", "Load Data...",
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Question,
                            MessageBoxResult.OK) == MessageBoxResult.OK)
                        {
                            //ZoneHelper.FnContentDeserialize(ofd.FileName);
                            //listInformation.SelectedIndex = listInformation.Items.Add($"{DateTime.Now:hh:MM:ss} - 加载新的数据文件{ofd.FileName}.");
                        }
                    e.Handled = true;
                },
                (sender, e) => { e.CanExecute = true; e.Handled = true; });

            this.CommandBindings.AddRange(new CommandBinding[] { cbQuit });

        }

        private void listInformation_SelectionChanged(object sender, SelectionChangedEventArgs e) { var lb = sender as ListBox; lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]); }
    }
}
