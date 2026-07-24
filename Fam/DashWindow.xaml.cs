using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fam
{
    /// <summary>
    /// Interaction logic for DashboardWindow.xaml
    /// </summary>
    public partial class DashWindow : Window
    {
        Portfolio portfolio => (Portfolio)DataContext;

        public DashWindow()
        {
            InitializeComponent();

            toggleTrasactions.IsChecked = true;

            //piechartCategories.Tooltip =new CustomTooltip();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!DataService.CheckSaved(portfolio.SaveFilePath, portfolio))
            {
                var resp = MessageBox.Show("File is not saved. Do you want to save now?", "Dashboard", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (resp == MessageBoxResult.Yes)
                {
                    butSave_Click(sender, new RoutedEventArgs());
                    if (!DataService.CheckSaved(portfolio.SaveFilePath, portfolio))
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (resp == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            Owner?.Show();
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            if (portfolio is not null && string.IsNullOrEmpty(portfolio.SaveFilePath))
            {
                SaveFileDialog sfd = new()
                {
                    Title = "Save Portfolio As",
                    Filter = "XML Files (*.xml)|*.xml",
                    FileName = portfolio.Name,
                    FilterIndex = 0,
                    DefaultExt = ".xml"
                };
                if (sfd.ShowDialog() == true)
                {
                    DataService.SaveData(sfd.FileName, portfolio);
                    MessageBox.Show("Saved", "Portfolio");
                    portfolio.SaveFilePath = sfd.FileName;
                }
            }
            else DataService.SaveData(portfolio.SaveFilePath, portfolio);

            FileService.AddtoRecentfiles(portfolio.SaveFilePath);
        }

        private void toggle_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                if (toggleTrasactions.IsChecked == true)
                    gridTransactions.Visibility = Visibility.Visible;
                else gridTransactions.Visibility = Visibility.Collapsed;

                if (toggleMutualfunds.IsChecked == true)
                    gridMutualfunds.Visibility = Visibility.Visible;
                else gridMutualfunds.Visibility = Visibility.Collapsed;

                if (toggleCategories.IsChecked == true)
                    gridCategories.Visibility = Visibility.Visible;
                else gridCategories.Visibility = Visibility.Collapsed;

                if (toggleCapitalgains.IsChecked == true)
                    gridCapitalgains.Visibility = Visibility.Visible;
                else gridCapitalgains.Visibility = Visibility.Collapsed;

                if (toggleFolios.IsChecked == true)
                    gridFolios.Visibility = Visibility.Visible;
                else gridFolios.Visibility = Visibility.Collapsed;

                if (toggleChart.IsChecked == true)
                    gridChart.Visibility = Visibility.Visible;
                else gridChart.Visibility = Visibility.Collapsed;
            }
        }

        private void butCopy_Click(object sender, RoutedEventArgs e)
        {
            if (portfolio is not null)
            {
                if (toggleTrasactions.IsChecked == true)
                {
                    string Headers = "Date" + "\t" + "Name of fund" + "\t" + "Folio" + "\t" + "Transaction" + "\t" + "Units" + "\t" + "NAV" + "\t" + "Amount" + "\t" + "Bal units";
                    string[] Transactions = portfolio.Transactions.Select(x => new string(x.Date.ToString("dd-MM-yyyy") + "\t" + x.Name + "\t" + x.Folio + "\t" + x.Transactiontype + "\t" + x.Units + "\t" + x.NAV + "\t" + Math.Round(x.Amount, 0) + "\t" + x.BalUnits)).ToArray();
                    Clipboard.SetText(Headers + "\n" + string.Join("\n", Transactions));
                    MessageBox.Show("Copied " + Transactions.Length.ToString());
                }
                else if (toggleMutualfunds.IsChecked == true)
                {
                    string Headers = "Name of fund" + "\t" + "Folio" + "\t" + "Amount" + "\t" + "Units" + "\t" + "NAV" + "\t" + "Current NAV" + "\t" + "Latest amt" + "\t" + "Profit" + "\t" + "%";
                    string[] Mutualfunds = portfolio.Mutualfunds.Select(x => new string(x.Name + "\t" + x.Folio + "\t" + Math.Round(x.BalAmt, 0) + "\t" + x.Units + "\t" + x.NAV + "\t" + x.navMutualfund?.NAV + "\t" + Math.Round(x.LatestAmount, 0) + "\t" + Math.Round(x.Profit, 0) + "\t" + x.Profitpercent.ToString("P1"))).ToArray();
                    Clipboard.SetText(Headers + "\n" + string.Join("\n", Mutualfunds));
                    MessageBox.Show("Copied " + Mutualfunds.Length.ToString());
                }
            }
        }

        private void butClear_Click(object sender, RoutedEventArgs e)
        {
            if (portfolio.TransactionFilter != null || portfolio.TransactionFilter2 != null)
            {
                portfolio.TransactionFilter = null;
                portfolio.TransactionFilter2 = null;
                portfolio.OnPropertyChanged(nameof(Portfolio.Transactions));
            }
            if (portfolio.MutualfundFilter != null)
            {
                portfolio.MutualfundFilter = null;
                portfolio.OnPropertyChanged(nameof(Portfolio.Mutualfunds));
            }
        }

        private void lviMutualfunds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Mutualfund Mf)
            {
                portfolio.TransactionFilter = Mf;
                portfolio.OnPropertyChanged(nameof(Portfolio.Transactions));
                toggleTrasactions.IsChecked = true;
            }
        }

        private void butRefresh_Click(object sender, RoutedEventArgs e)
        {
            portfolio.OnPropertyChanged(nameof(Portfolio.Transactions));
            portfolio.OnPropertyChanged(nameof(Portfolio.Mutualfunds));
            portfolio.OnPropertyChanged(nameof(Portfolio.Categories));
            portfolio.OnPropertyChanged(nameof(Portfolio.Subcategories));
            portfolio.OnPropertyChanged(nameof(Portfolio.Capitalgains));
            portfolio.OnPropertyChanged(nameof(Portfolio.Folios));

            //DataService.CreateSubcategorycolours();
            //portfolio.CreateCategorychart();
        }

        private void butDownloadlinks_Click(object sender, RoutedEventArgs e)
        {
            new LinksWindow() { Owner = this }.ShowDialog();
        }

        private void lviCapitalgains_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Capitalgain Cg && (Cg.BookedLt > 0 || Cg.BookedSt > 0))
            {
                portfolio.TransactionFilter2 = Cg;
                portfolio.OnPropertyChanged(nameof(Portfolio.Transactions));
                toggleTrasactions.IsChecked = true;
            }
        }

        private void butClearfilter_Click(object sender, RoutedEventArgs e)
        {
            if (portfolio.Foliogroups.Count() > 1)
                portfolio.Foliofilter = null;
        }

        private void butDeletegroup_Click(object sender, RoutedEventArgs e)
        {
            portfolio.RemoveFoliogroup((Foliogroup)((Button)sender).DataContext);
        }

        private void butAddgroup_Click(object sender, RoutedEventArgs e)
        {
            string groupname = "New group";
            int i = 1;
            while (portfolio.Foliogroups.Any(x => x.Name == groupname))
                groupname = "New group " + i.ToString();

            portfolio.AddFoliogroup(groupname);
        }

        private void lviSubcategories_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Category category)
            {
                portfolio.MutualfundFilter = category;
                portfolio.OnPropertyChanged(nameof(Portfolio.Mutualfunds));
            }
        }

        private void butReloadfiles_Click(object sender, RoutedEventArgs e)
        {
            var filepaths = FileService.SelectTransactionfiles();

            if (filepaths.Count() > 0)
                try
                {
                    var name = Path.GetFileNameWithoutExtension(filepaths[0]);
                    var transactions = DataService.LoadtransactionsFromfiles(filepaths);

                    if (transactions.Count() < portfolio.Transactions.Count)
                    {
                        var res = MessageBox.Show("Total no. of new transactions is less than currently existing transactions.\nDo you still want to continue?", "Warning", MessageBoxButton.YesNoCancel);
                        if (res == MessageBoxResult.Yes)
                            portfolio.LoadTransactions(transactions);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not load a file due to following error: \n" + ex.Message);
                }
        }
    }
}
