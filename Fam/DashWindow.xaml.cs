using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fam
{
    /// <summary>
    /// Interaction logic for DashboardWindow.xaml
    /// </summary>
    public partial class DashWindow : Window
    {
        Portfolio Pf => (Portfolio)DataContext;

        public DashWindow()
        {
            InitializeComponent();

            toggleTrasactions.IsChecked = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Owner?.Close();
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            if (Pf is not null)
            {
                if (string.IsNullOrEmpty(Pf.SaveFilePath))
                {
                    SaveFileDialog sfd = new() { Title = "Save Portfolio As", Filter = "XML Files (*.xml)|*.xml", FilterIndex = 0, DefaultExt = ".xml" };
                    if (sfd.ShowDialog() == true)
                    {
                        DataService.SaveData(sfd.FileName, Pf);
                        MessageBox.Show("Saved", "Portfolio");
                        Pf.SaveFilePath = sfd.FileName;
                    }
                }
                else DataService.SaveData(Pf.SaveFilePath, Pf);
            }
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
            }
        }

        private void butCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Pf is not null)
            {
                if (toggleTrasactions.IsChecked == true)
                {
                    string[] Transactions = Pf.Transactions.Select(x => new string(x.Date.ToString("dd-MM-yyyy") + "\t" + x.Name + "\t" + x.Folio + "\t" + x.Transactiontype + "\t" + x.Units + "\t" + x.Price + "\t" + x.Amount)).ToArray();
                    Clipboard.SetText(string.Join("\n", Transactions));
                    MessageBox.Show("Copied " + Transactions.Length.ToString());
                }
                else if (toggleMutualfunds.IsChecked == true)
                {
                    string[] Mutualfunds = Pf.Mutualfunds.Select(x => new string(x.Name + "\t" + x.Folio)).ToArray();
                    Clipboard.SetText(string.Join("\n", Mutualfunds));
                    MessageBox.Show("Copied " + Mutualfunds.Length.ToString());
                }
                else if (toggleCategories.IsChecked == true)
                {

                }
            }
        }

        private void butClear_Click(object sender, RoutedEventArgs e)
        {
            if (Pf.TransactionFilter != null || Pf.TransactionFilter2 != null)
            {
                Pf.TransactionFilter = null;
                Pf.TransactionFilter2 = null;
                Pf.OnPropertyChanged(nameof(Portfolio.Transactions));
            }
        }

        private void lviMutualfunds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Mutualfund Mf)
            {
                Pf.TransactionFilter = Mf;
                Pf.OnPropertyChanged(nameof(Portfolio.Transactions));
                toggleTrasactions.IsChecked = true;
            }
        }

        private void butRefresh_Click(object sender, RoutedEventArgs e)
        {
            Pf.OnPropertyChanged(nameof(Portfolio.Transactions));
            Pf.OnPropertyChanged(nameof(Portfolio.Mutualfunds));
            Pf.OnPropertyChanged(nameof(Portfolio.Categories));
            Pf.OnPropertyChanged(nameof(Portfolio.Subcategories));
            Pf.OnPropertyChanged(nameof(Portfolio.Capitalgains));
        }

        private void butDownloadlinks_Click(object sender, RoutedEventArgs e)
        {
            new LinksWindow() { Owner = this }.ShowDialog();
        }

        private void lviCapitalgains_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Capitalgain Cg)
            {
                Pf.TransactionFilter2 = Cg;
                Pf.OnPropertyChanged(nameof(Portfolio.Transactions));
                toggleTrasactions.IsChecked = true;
            }
        }
    }
}
