using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace Fam
{
    public static class FileService
    {
        public static string NAV_FilePath => Path.Combine(DirectoryService.NavDirectory, "nav_" + DateTime.Today.ToString("yyyy-MM-dd")) + ".txt";
        public static string Portfolio_FilePath(NAVmutualfund navmf) => Path.Combine(DirectoryService.PortfolioDirectory, navmf.ISIN + "_portfolio_" + DateTime.Today.ToString("yyy-MM")) + ".json";

        public static string[] SelectTransactionfiles()
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
                string[] filepaths = openFileDialog.FileNames;

                var ext = filepaths.Select(x => Path.GetExtension(x)).ToArray();
                if (ext.Any(x => x is not (".csv" or ".xlsx" or ".xls")))
                {
                    MessageBox.Show("Selected file type is not supported. Please select supported file types.", "File type mismatch");
                    goto SelectFile;
                }

                return filepaths;
            }

            return new string[] { };
        }
    }
}
