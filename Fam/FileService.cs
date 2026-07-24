using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
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

        [DataMember]
        public static ObservableCollection<Recentfile> Recentfiles = new();

        public static void ReadRecentfileslist()
        {
            try
            {
                var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };
                Recentfiles = JsonSerializer.Deserialize<ObservableCollection<Recentfile>>(Properties.Settings.Default.Recentfiles);
            }
            catch
            {
                Recentfiles = new ObservableCollection<Recentfile>();
                Properties.Settings.Default.Recentfiles = JsonSerializer.Serialize(Recentfiles);
                Properties.Settings.Default.Save();
            }
        }

        public static void AddtoRecentfiles(string filepath)
        {
            var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };

            if (!Recentfiles.Any(x => x.Filepath == filepath))
            {
                Recentfiles.Add(new Recentfile(filepath));
                Properties.Settings.Default.Recentfiles = JsonSerializer.Serialize(Recentfiles, options);
                Properties.Settings.Default.Save();
            }
        }

        public static void RemoveRecentfile(Recentfile file)
        {
            if (Recentfiles.Contains(file))
                Recentfiles.Remove(file);

            var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };

            Properties.Settings.Default.Recentfiles = JsonSerializer.Serialize(Recentfiles, options);
            Properties.Settings.Default.Save();
        }
    }
}
