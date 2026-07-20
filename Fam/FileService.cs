using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fam
{
    public static class FileService
    {
        public static string NAV_FilePath => Path.Combine(DirectoryService.NavDirectory, "nav_" + DateTime.Today.ToString("yyyy-MM-dd")) + ".txt";
        public static string Portfolio_FilePath(NAVmutualfund navmf) => Path.Combine(DirectoryService.PortfolioDirectory, navmf.ISIN + "_portfolio_" + DateTime.Today.ToString("yyy-MM")) + ".json";
    }
}
