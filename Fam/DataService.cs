using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Transactions;
using System.Windows;

namespace Fam
{
    internal static class DataService
    {
        public static List<NAVmutualfund> NAVmutualfunds = new();
        public static List<NAVmutualfund> Active_NAVmutualfunds => NAVmutualfunds.Where(x => x.NavDate > DateTime.Today.AddDays(-30)).ToList();

        public static Dictionary<string, MasterMf> MasterMfs = new();

        public static readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };


        // Save and read Datacontractserialised data
        public static void SaveData(string filepath, object data)
        {
            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serialiser = new DataContractSerializer(typeof(Portfolio), settings);

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                serialiser.WriteObject(fs, data);
        }

        public static object ReadData(string filepath)
        {
            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serialiser = new DataContractSerializer(typeof(Portfolio), settings);

            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return serialiser.ReadObject(fs);
        }

        public static bool ConnectionIsAvailable(string url = "www.google.co.in")
        {
            try
            {
                Ping myPing = new Ping();
                String host = url;
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingReply reply = myPing.Send(host, timeout, buffer);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task DownloadNavAsync()
        {
            if (!File.Exists(FileService.NAV_FilePath) && ConnectionIsAvailable("portal.amfiindia.com"))
            {
                try
                {
                    string NavFileUrl = "https://portal.amfiindia.com/spages/NAVAll.txt";

                    using (HttpResponseMessage response = await client.GetAsync(NavFileUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP status is an error

                        using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
                        using Stream streamToWriteTo = File.Open(FileService.NAV_FilePath, FileMode.Create);
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not download NAVs due to following error:\n" + ex.Message, "NAV");
                }
            }
        }

        public static void ReadNAVdata()
        {
            NAVmutualfunds = new List<NAVmutualfund>();

            var filePath = FileService.NAV_FilePath;

            if (!File.Exists(filePath))
            {
                // last available file
                filePath = Directory.GetFiles(DirectoryService.NavDirectory).MaxBy(x => File.GetLastWriteTime(x));
            }

            if (File.Exists(filePath))
            {
                using (StreamReader Sr = new StreamReader(filePath, new FileStreamOptions() { Share = FileShare.ReadWrite }))
                {
                    if (!Sr.EndOfStream)
                        Sr.ReadLine(); // Headers
                    // Scheme Code;ISIN Div Payout/ ISIN Growth;ISIN Div Reinvestment;Scheme Name;Net Asset Value;Date

                    string fund = "", type = "";

                    while (!Sr.EndOfStream)
                    {
                        string line = Sr.ReadLine().Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        if (line.Contains(";")) // data line
                        {
                            string[] values = line.Split(';');
                            NAVmutualfund navmutualfund = new(name: values[3],
                                fund: fund,
                                type: type,
                                schemeCode: values[0],
                                iSIN1: values[1],
                                iSIN2: values[2],
                                nAV: decimal.Parse(values[4]),
                                navDate: DateTime.ParseExact(values[5], "dd-MMM-yyyy", CultureInfo.InvariantCulture));
                            NAVmutualfunds.Add(navmutualfund);
                        }
                        else // section header line
                        {
                            if (line.ToLower().Contains("mutual fund"))
                                fund = line;
                            else type = line;
                        }
                    }
                }

                if (NAVmutualfunds.Count > 0)
                {
                    NAVmutualfunds = NAVmutualfunds.OrderBy(x => x.Fund).ThenBy(x => x.Name).ToList();

                    // Assignment of common/base PortfolioMutualfund for each fund to refer tax-category
                    var uniquefunds = NAVmutualfunds.DistinctBy(x => x.OnlyName, StringComparer.OrdinalIgnoreCase);
                    foreach (NAVmutualfund item in uniquefunds)
                    {
                        item.PortfolioMutualfund = item;
                        var similarfunds = NAVmutualfunds.Where(x => string.Compare(x.OnlyName, item.OnlyName, true) == 0);
                        foreach (NAVmutualfund item2 in NAVmutualfunds)
                            if (item2.PortfolioMutualfund == null)
                                item2.PortfolioMutualfund = item;
                    }
                }

            }
        }

        public static void CopyAllCategoriesAndSubcateogories()
        {
            List<string> categories = new();

            foreach (NAVmutualfund item in Active_NAVmutualfunds.OrderBy(x => x.MainCategory).ThenBy(x => x.Subcategory))
            {
                if (!(categories.Any(x => x == item.MainCategory + "\t" + item.Subcategory)))
                    categories.Add(item.MainCategory + "\t" + item.Subcategory);
            }

            Clipboard.SetText(string.Join("\n", categories));
        }

        private static void DownloadPortfoliofile(NAVmutualfund navmutualfund)
        {
            var isin = navmutualfund.ISIN;
            if (string.IsNullOrEmpty(isin))
                return;

            // call FinAPI using ISIN
            var filePath = FileService.Portfolio_FilePath(navmutualfund);
            if (!File.Exists(filePath) && ConnectionIsAvailable())
            {
                try
                {
                    string PortfolioUrl = $"https://finapi.upvaly.com/api/mf/isin/{isin}?fields=portfolio";

                    using (var request = new HttpRequestMessage(HttpMethod.Get, PortfolioUrl))
                    {
                        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        request.Headers.Accept.ParseAdd("application/json");

                        using (HttpResponseMessage response = client.Send(request, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            // Read the stream synchronously and copy it to the file
                            using (Stream networkStream = response.Content.ReadAsStream())
                            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                networkStream.CopyTo(fileStream);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not download portfolio due to following error:\n" + ex.Message);
                }

                Thread.Sleep(1000); // to avoid rate limiting
            }
        }

        public static TaxCategory GetTaxcategory(NAVmutualfund navmutualfund)
        {
            if (navmutualfund == null)
                return TaxCategory.Uncategorised;

            string filepath = FileService.Portfolio_FilePath(navmutualfund);
            if (!File.Exists(filepath))
                DownloadPortfoliofile(navmutualfund);

            // get from downloaded file
            if (File.Exists(filepath))
            {
                string jsonString = File.ReadAllText(filepath);

                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                // root > data > portfolio > assetAllocation > 
                JsonElement data = root.GetProperty("data");
                JsonElement portfolio = data.GetProperty("portfolio").GetProperty("assetAllocation");

                decimal equity = decimal.Parse(portfolio.GetProperty("equityAllocation").ToString());
                decimal debt = decimal.Parse(portfolio.GetProperty("debtAllocation").ToString());

                // Ascertain
                if (equity == 0 && debt == 0)
                    return TaxCategory.Uncategorised;
                else if (equity >= 65 && !navmutualfund.isFoF)
                    return TaxCategory.Equity;
                else if (equity > 81 && navmutualfund.isFoF)
                    return TaxCategory.Equity;
                else if (debt >= 65)
                    return TaxCategory.Debt;
                else if (equity < 65 && debt < 65)
                    return TaxCategory.Others;
            }

            return TaxCategory.Uncategorised;
        }

        public static async Task LoadMaster()
        {
            Uri[] uris = new Uri[] { new Uri("pack://application:,,,/res/SCHMSTRDMAT.txt"), new Uri("pack://application:,,,/res/SCHMSTRPHY.txt") };

            MasterMfs = new Dictionary<string, MasterMf>();

            foreach (Uri uri in uris)
            {
                var resourcestream = Application.GetResourceStream(uri);

                using (StreamReader reader = new StreamReader(resourcestream.Stream))
                {
                    string line;

                    int isin_i = -1, name_i = -1, cpcode_i = -1;

                    // headers
                    // ...|ISIN|AMC Code|Scheme Type|Scheme Plan|Scheme Name|...|RTA Agent Code|...|Lock-in Period Flag|Lock-in Period|Channel Partner Code|...

                    if ((line = await reader.ReadLineAsync()) != null)
                    {
                        string[] headers = line.Split("|");

                        for (int i = 0; i <= headers.Length - 1; i++)
                        {
                            var header_lower = headers[i].Trim().ToLower();
                            if (header_lower == "isin")
                                isin_i = i;
                            else if (header_lower == "scheme name")
                                name_i = i;
                            else if (header_lower == "channel partner code")
                                cpcode_i = i;
                        }
                    }

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        string[] items = line.Split("|");
                        var name = items[name_i].Trim();

                        // a filter
                        if (name.ToLower().Contains("nfo smart switch -"))
                            name = name.Substring(name.ToLower().IndexOf("nfo smart switch -") + 18).Trim();

                        if (!MasterMfs.ContainsKey(name))
                            MasterMfs.Add(name,
                                new MasterMf(
                                name: name,
                                iSIN: items[isin_i].Trim(),
                                cpCode: items[cpcode_i].Trim()
                                ));
                    }
                }
            }
        }

        public static List<Transaction> Loadtransactionsfromfiles(string[] filePaths)
        {
            List<Transaction> transactions = new List<Transaction>();

            foreach (string filepath in filePaths)
                if (File.Exists(filepath))
                    using (StreamReader Sr = new StreamReader(filepath, new FileStreamOptions() { Share = FileShare.ReadWrite }))
                    {
                        int date_i = -1, folio_i = -1, name_i = -1, transaction_i = -1, units_i = -1, nav_i = -1, amount_i = -1, isin_i = -1, fundname_i = -1, cpcode_i = -1;

                        if (!Sr.EndOfStream)
                        {
                            string headerline = Sr.ReadLine();
                            var headers = headerline.Split(",");

                            // possible columns
                            // Date, Folio Number, Name of the Fund, Order, Units, NAV, Current Nav, Amount (INR)
                            // MF_NAME,INVESTOR_NAME,PAN,FOLIO_NUMBER,PRODUCT_CODE,SCHEME_NAME,Type,TRADE_DATE,TRANSACTION_TYPE,DIVIDEND_RATE,AMOUNT,UNITS,PRICE,BROKER
                            // FundName,Investor Name,Account Number,Product Code,Scheme Description,Transaction Date,Transaction Description,Amount,Units,NAV,Broker Code,Broker Name,SchemeISIN
                            for (int i = 0; i <= headers.Length - 1; i++)
                            {
                                string header_lower = headers[i].Trim().ToLower();

                                if (header_lower == "date" || header_lower == "trade_date" || header_lower == "transaction date")
                                    date_i = i;
                                else if (header_lower == "folio number" || header_lower == "folio_number" || header_lower == "account number")
                                    folio_i = i;
                                else if (header_lower == "name of the fund" || header_lower == "scheme_name" || header_lower == "scheme description")
                                    name_i = i;
                                else if (header_lower == "order" || header_lower == "transaction_type" || header_lower == "transaction description")
                                    transaction_i = i;
                                else if (header_lower == "units")
                                    units_i = i;
                                else if (header_lower == "nav" || header_lower == "price")
                                    nav_i = i;
                                else if (header_lower == "amount (inr)" || header_lower == "amount")
                                    amount_i = i;
                                // optional
                                else if (header_lower == "schemeisin")
                                    isin_i = i;
                                else if (header_lower == "mf_name" || header_lower == "fundname")
                                    fundname_i = i;
                                else if (header_lower == "product_code" || header_lower == "product code")
                                    cpcode_i = i;
                            }
                        }

                        // check if headers are okay
                        if (date_i == -1 || folio_i == -1 || name_i == -1 || transaction_i == -1 || units_i == -1 || nav_i == -1)
                        {
                            MessageBox.Show("Headers mismatch. File skipped.\n" + Path.GetFileName(filepath));
                            continue;
                        }

                        while (!Sr.EndOfStream)
                        {
                            string[] rowItems = Sr.ReadLine().Split(",");

                            var units = !string.IsNullOrEmpty(rowItems[units_i].Trim()) ? Math.Abs(decimal.Parse(rowItems[units_i].Trim())) : 0;
                            var ISIN = isin_i >= 0 ? rowItems[isin_i] : "";
                            var fundname = fundname_i >= 0 ? rowItems[fundname_i] : "";
                            var cpcode = cpcode_i >= 0 ? rowItems[cpcode_i].Trim() : "";

                            // financial transactions only
                            if (units != 0)
                            {
                                var amount = decimal.Parse(rowItems[amount_i].Trim()); // to check rejections

                                transactions.Add(new Transaction(
                                    date: DateTime.ParseExact(rowItems[date_i].Trim(), ["dd-MMM-yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture),
                                   name: rowItems[name_i].Trim(),
                                   folio: rowItems[folio_i].Trim(),
                                   transactionName: rowItems[transaction_i].Trim(),
                                   units: units,
                                   price: decimal.Parse(rowItems[nav_i].Trim()),
                                   amount: amount,
                                   iSIN: ISIN,
                                   fundName: fundname,
                                   cpCode: cpcode
                                   ));
                            }
                        }
                    }

            // remove rejected
            if (transactions.Any(x => x.TransactionName.ToLower().Contains("rejection")))
            {
                var rejected = transactions.FindAll(x => x.TransactionName.ToLower().Contains("rejection"));

                foreach (Transaction item in rejected)
                {
                    if (transactions.IndexOf(item) == 0)
                        continue;

                    Transaction prevItem = transactions[transactions.IndexOf(item) - 1];

                    if (prevItem.Name == item.Name && item.TransactionName.Contains(prevItem.TransactionName))
                    {
                        transactions.Remove(prevItem);
                        transactions.Remove(item);
                    }
                }
            }

            transactions = transactions.OrderBy(x => x.Date).ToList();

            return transactions;
        }
    }
}
