using ExcelDataReader;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;
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
            var serialiser = new DataContractSerializer(data.GetType(), settings);

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                serialiser.WriteObject(fs, data);
        }

        public static T ReadData<T>(string filepath)
        {
            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serialiser = new DataContractSerializer(typeof(T), settings);

            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return (T)serialiser.ReadObject(fs);
        }

        public static bool CheckSaved(string filepath, object data)
        {
            if (!File.Exists(filepath))
                return false;

            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serialiser = new DataContractSerializer(data.GetType(), settings);

            using (MemoryStream memorystream = new MemoryStream())
            {
                serialiser.WriteObject(memorystream, data);
                byte[] currentbytes = memorystream.ToArray();

                byte[] savedbytes = File.ReadAllBytes(filepath);

                if (currentbytes.Length != savedbytes.Length)
                    return false;

                return CryptographicOperations.FixedTimeEquals(currentbytes, savedbytes);
            }

            GC.Collect();
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
                var allfiles = Directory.GetFiles(DirectoryService.NavDirectory);
                filePath = allfiles.Count() > 0 ? allfiles.MaxBy(x => File.GetLastWriteTime(x)) : "";
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
                    NAVmutualfunds = NAVmutualfunds.OrderBy(x => x.FundName).ThenBy(x => x.Name).ToList();

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

        public static void CreateSubcategorycolours()
        {
            List<Tuple<string, TaxCategory>> subcategorylist = NAVmutualfunds.DistinctBy(x => x.Taxcategory + x.Subcategory).Select(x => new Tuple<string, TaxCategory>(x.Subcategory, x.Taxcategory)).ToList();
            ColourService.CreateSubcateogycolours(subcategorylist);
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

        public static List<Transaction> LoadtransactionsFromfiles(string[] filePaths)
        {
            List<Transaction> transactions = new List<Transaction>();

            foreach (string filepath in filePaths)
                if (File.Exists(filepath))
                {
                    int date_i = -1, folio_i = -1, name_i = -1, transaction_i = -1, units_i = -1, nav_i = -1, amount_i = -1, isin_i = -1, fundname_i = -1, cpcode_i = -1;

                    // possible columns
                    // Date, Folio Number, Name of the Fund, Order, Units, NAV, Current Nav, Amount (INR)
                    // Cams: MF_NAME,INVESTOR_NAME,PAN,FOLIO_NUMBER,PRODUCT_CODE,SCHEME_NAME,Type,TRADE_DATE,TRANSACTION_TYPE,DIVIDEND_RATE,AMOUNT,UNITS,PRICE,BROKER
                    // Kfintech: FundName,Investor Name,Account Number,Product Code,Scheme Description,Transaction Date,Transaction Description,Amount,Units,NAV,Broker Code,Broker Name,SchemeISIN

                    string[] dateheaders = new string[] { "date", "trade_date", "transaction date" };
                    string[] folioheaders = new string[] { "folio number", "folio_number", "account number" };
                    string[] nameheaders = new string[] { "name of the fund", "scheme_name", "scheme description" };
                    string[] transactionheaders = new string[] { "order", "transaction_type", "transaction description" };
                    string[] unitsheaders = new string[] { "units" };
                    string[] navheaders = new string[] { "nav", "price" };
                    string[] amountheaders = new string[] { "amount (inr)", "amount" };
                    string[] isinheaders = new string[] { "schemeisin", "isin" };
                    string[] fundnameheaders = new string[] { "mf_name", "fundname" };
                    string[] cpcodeheaders = new string[] { "product_code", "product code" };

                    if (Path.GetExtension(filepath) == ".csv")
                    {
                        using (StreamReader Sr = new StreamReader(filepath, new FileStreamOptions() { Share = FileShare.ReadWrite }))
                        {
                            // read and assign header indices
                            if (!Sr.EndOfStream)
                            {
                                string headerline = Sr.ReadLine();
                                var headers = headerline.Split(",");


                                for (int i = 0; i <= headers.Length - 1; i++)
                                {
                                    string header_lower = headers[i].Trim().ToLower();

                                    if (dateheaders.Contains(header_lower))
                                        date_i = i;
                                    else if (folioheaders.Contains(header_lower))
                                        folio_i = i;
                                    else if (nameheaders.Contains(header_lower))
                                        name_i = i;
                                    else if (transactionheaders.Contains(header_lower))
                                        transaction_i = i;
                                    else if (unitsheaders.Contains(header_lower))
                                        units_i = i;
                                    else if (navheaders.Contains(header_lower))
                                        nav_i = i;
                                    else if (amountheaders.Contains(header_lower))
                                        amount_i = i;
                                    // optional
                                    else if (isinheaders.Contains(header_lower))
                                        isin_i = i;
                                    else if (fundnameheaders.Contains(header_lower))
                                        fundname_i = i;
                                    else if (cpcodeheaders.Contains(header_lower))
                                        cpcode_i = i;
                                }
                            }

                            // check if headers are okay
                            if (date_i == -1 || folio_i == -1 || name_i == -1 || transaction_i == -1 || units_i == -1 || nav_i == -1)
                            {
                                MessageBox.Show("Headers mismatch. File skipped.\n" + Path.GetFileName(filepath));
                                continue;
                            }

                            // read csv file into transactions
                            while (!Sr.EndOfStream)
                            {
                                string[] rowItems = Sr.ReadLine().Split(",");

                                var units = !string.IsNullOrEmpty(rowItems[units_i].Trim()) ? Math.Abs(decimal.Parse(rowItems[units_i].Trim())) : 0;

                                // financial transactions only
                                if (units != 0)
                                {
                                    var ISIN = isin_i >= 0 ? rowItems[isin_i] : "";
                                    var fundname = fundname_i >= 0 ? rowItems[fundname_i] : "";
                                    var cpcode = cpcode_i >= 0 ? rowItems[cpcode_i].Trim() : "";
                                    var amount = decimal.Parse(rowItems[amount_i].Trim()); // to check rejections
                                    var folio = rowItems[folio_i].Trim();
                                    if (folio[^1] == '/')
                                        folio = folio.Substring(0, folio.Length - 1);

                                    transactions.Add(new Transaction(
                                        date: DateTime.ParseExact(rowItems[date_i].Trim(), ["dd-MMM-yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture),
                                       name: rowItems[name_i].Trim(),
                                       folio: folio,
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
                    }
                    else if (Path.GetExtension(filepath) is ".xls" or ".xlsx")
                    {
                        using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            // read and assign header indices
                            if (reader.Read())
                            {
                                string[] headers = new string[reader.FieldCount];

                                for (int i = 0; i <= headers.Length - 1; i++)
                                {
                                    headers[i] = reader.GetValue(i).ToString();

                                    string header_lower = headers[i].ToLower();

                                    if (dateheaders.Contains(header_lower))
                                        date_i = i;
                                    else if (folioheaders.Contains(header_lower))
                                        folio_i = i;
                                    else if (nameheaders.Contains(header_lower))
                                        name_i = i;
                                    else if (transactionheaders.Contains(header_lower))
                                        transaction_i = i;
                                    else if (unitsheaders.Contains(header_lower))
                                        units_i = i;
                                    else if (navheaders.Contains(header_lower))
                                        nav_i = i;
                                    else if (amountheaders.Contains(header_lower))
                                        amount_i = i;
                                    // optional
                                    else if (isinheaders.Contains(header_lower))
                                        isin_i = i;
                                    else if (fundnameheaders.Contains(header_lower))
                                        fundname_i = i;
                                    else if (cpcodeheaders.Contains(header_lower))
                                        cpcode_i = i;
                                }

                                if (date_i == -1 || name_i == -1 || folio_i == -1 || transaction_i == -1 || units_i == -1 || nav_i == -1 || amount_i == -1)
                                {
                                    MessageBox.Show(Path.GetFileName(filepath) + " headers mismatch. skipped.");
                                    continue;
                                }
                            }

                            // read excel file into transactions
                            while (reader.Read())
                            {
                                var units = reader.GetValue(units_i) != null && !string.IsNullOrEmpty(reader.GetValue(units_i).ToString())
                                    ? Math.Abs(decimal.Parse(reader.GetValue(units_i).ToString())) : 0;

                                // financial transactions only
                                if (units != 0)
                                {
                                    var ISIN = isin_i >= 0 ? reader.GetValue(isin_i).ToString() : "";
                                    var fundname = fundname_i >= 0 ? reader.GetValue(fundname_i).ToString() : "";
                                    var cpcode = cpcode_i >= 0 ? reader.GetValue(cpcode_i).ToString().Trim() : "";
                                    var amount = decimal.Parse(reader.GetValue(amount_i).ToString().Trim()); // to check rejections
                                    var folio = reader.GetValue(folio_i).ToString().Trim();
                                    if (folio[^1] == '/')
                                        folio = folio.Substring(0, folio.Length - 1);

                                    transactions.Add(new Transaction(
                                       date: DateTime.ParseExact(reader.GetValue(date_i).ToString().Trim(), ["dd-MMM-yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture),
                                       name: reader.GetValue(name_i).ToString().Trim(),
                                       folio: folio,
                                       transactionName: reader.GetValue(transaction_i).ToString().Trim(),
                                       units: units,
                                       price: decimal.Parse(reader.GetValue(nav_i).ToString().Trim()),
                                       amount: amount,
                                       iSIN: ISIN,
                                       fundName: fundname,
                                       cpCode: cpcode
                                       ));
                                }
                            }
                        }
                    }
                    else if (Path.GetExtension(filepath) == ".pdf")
                    {

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
                    if (!(prevItem.Name == item.Name && prevItem.Amount == item.Amount && prevItem.Transactiontype == item.Transactiontype))
                        prevItem = transactions[transactions.IndexOf(item) + 1];

                    if (prevItem.Name == item.Name && prevItem.Amount == item.Amount && prevItem.Transactiontype == item.Transactiontype)
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
