using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Fam
{
    /// <summary>
    /// Interaction logic for Links.xaml
    /// </summary>
    public partial class LinksWindow : Window
    {
        public LinksWindow()
        {
            InitializeComponent();
        }

        private void txtKfintechlink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string url = txtKfintechlink.Text;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void txtCamslink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string url = txtCamslink.Text;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void txtFamGithublink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string url = txtFamGithublink.Text;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void butAutofill_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
