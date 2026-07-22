using Microsoft.Win32;
using System.IO;
using System.Transactions;
using System.Windows;


namespace Fam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            initiateHttpclient();

            await DataService.LoadMaster();
            await DataService.DownloadNavAsync();
            DataService.ReadNAVdata();
        }

        private void initiateHttpclient()
        {
            DataService.client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            DataService.client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
            DataService.client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        }

        private void butNew_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect = true,
                Title = "Select Transaction files",
                Filter = "Excel files (*.xlsx,*.xls)|*.xlsx;*.xls|CSV files (*.csv)|*.csv",
                FilterIndex = 0,
                AddExtension = false
            };

        SelectFile:
            if (openFileDialog.ShowDialog() == true)
            {
                var filepaths = openFileDialog.FileNames;

                var ext = filepaths.Select(x => Path.GetExtension(x)).ToArray();
                if (ext.Any(x => x is not (".csv" or ".xlsx" or ".xls")))
                {
                    MessageBox.Show("Selected file type is not supported. Please select supported file types.", "File type mismatch");
                    goto SelectFile;
                }

                try
                {
                    var name = Path.GetFileNameWithoutExtension(filepaths[0]);
                    var transactions = DataService.LoadtransactionsFromfiles(filepaths);
                    Portfolio NewPf = new(name, transactions);

                    DashWindow DashWin = new() { Owner = this, DataContext = NewPf };
                    DashWin.Show();

                    Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not load file due to following error: \n" + ex.Message);
                }
            }
        }

        private void butOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect = false,
                Title = "Open Portfolio file",
                Filter = "XML files (*.xml)|*.xml",
                FilterIndex = 0,
                DefaultExt = ".xml"
            };

        SelectFile:
            if (openFileDialog.ShowDialog() == true)
            {
                string filepath = openFileDialog.FileName;

                string ext = Path.GetExtension(filepath);
                if (ext != ".xml")
                {
                    MessageBox.Show("Selected file type is not xml. Only xml files are supported. Please select an xml file.", "Filetype mismatch");
                    goto SelectFile;
                }

                try
                {
                    Portfolio portfolio = (Portfolio)DataService.ReadData(filepath);
                    portfolio.SaveFilePath = filepath;
                    Task.Run(() => portfolio.Load());

                    DashWindow DashWin = new() { Owner = this, DataContext = portfolio };
                    DashWin.Show();

                    Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not load file due to following error: \n" + ex.Message);
                }
            }
        }

        private void butDownloadlinks_Click(object sender, RoutedEventArgs e)
        {
            new LinksWindow() { Owner = this }.ShowDialog();
        }
    }
}