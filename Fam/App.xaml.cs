using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Fam
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Override FrameworkElement default Language so it uses system settings
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //// Installing playwright's binaries
            //// Alternatives: "chromium", "firefox", "webkit"
            //int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);

            //if (exitCode != 0)
            //{
            //    throw new Exception($"Playwright browser installation failed with exit code {exitCode}");
            //}

            //MessageBox.Show("Browser ready.");
        }
    }

}
