using System.IO;

namespace Fam
{
    public static class DirectoryService
    {
        static string AppdataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static string _famdataDirectory = Path.Combine(AppdataDirectory, "ManageSoft", "Fam");
        static string _navDirectory = Path.Combine(_famdataDirectory, "nav");
        static string _portfolioDirectory = Path.Combine(_famdataDirectory, "portfolio");

        public static string FamdataDirectory
        {
            get
            {
                if (!Directory.Exists(_famdataDirectory))
                    Directory.CreateDirectory(_famdataDirectory);
                return _famdataDirectory;
            }
        }

        public static string NavDirectory
        {
            get
            {
                if (!Directory.Exists(_navDirectory))
                    Directory.CreateDirectory(_navDirectory);
                return _navDirectory;
            }
        }

        public static string PortfolioDirectory
        {
            get
            {
                if (!Directory.Exists(_portfolioDirectory))
                    Directory.CreateDirectory(_portfolioDirectory);
                return _portfolioDirectory;
            }
        }
    }
}
