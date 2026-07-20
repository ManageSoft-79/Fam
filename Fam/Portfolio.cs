using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Transactions;
using System.Windows;
using System.Windows.Data;

namespace Fam
{
    [DataContract]
    public class Portfolio : INotifyPropertyChanged
    {
        [DataMember]
        private string? _name;
        [DataMember]
        private List<Transaction> _transactions = new();
        [DataMember]
        private List<Mutualfund> _mutualfunds = new();
        [DataMember]
        private List<Category> _categories = new();
        [DataMember]
        private List<Category> _subcategories = new();
        [DataMember]
        private List<Capitalgain> _capitalgains = new();

        private decimal _wtdavgdaysholding;
        private decimal _wtdavgdayslifetime;

        public string SaveFilePath = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        //protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyname = null)
        //{
        //    if (Equals(storage, value)) return false;
        //    storage = value;
        //    OnPropertyChanged(propertyname);
        //    return true;
        //}

        public Mutualfund? TransactionFilter = null;

        public Capitalgain? TransactionFilter2 = null;

        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Transaction> Transactions
        {
            get
            {
                List<Transaction> list = _transactions.OrderByDescending(x => x.Date).ToList();

                if (TransactionFilter != null)
                    list = list.FindAll(x => x.Name == TransactionFilter.Name && x.Folio == TransactionFilter.Folio);
                if (TransactionFilter2 != null)
                    list = list.FindAll(x => x.mutualfund?.Taxcategory == TransactionFilter2.Taxcategory && x.Transactiontype == TransactionType.sell && x.Date >= TransactionFilter2.FirstDate && x.Date <= TransactionFilter2.LastDate);

                return new ObservableCollection<Transaction>(list);
            }
        }

        public ObservableCollection<Mutualfund> Mutualfunds
        {
            get
            {
                OnPropertyChanged(nameof(LatestAmt));
                OnPropertyChanged(nameof(Fundscount));

                var collection = new ObservableCollection<Mutualfund>(_mutualfunds.Where(x => x.Units > 0).OrderBy(x => x.Taxcategory));
                var view = (CollectionView)CollectionViewSource.GetDefaultView(collection);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Taxcategory"));
                return collection;
            }
        }
        public ObservableCollection<Mutualfund> Mutualfunds_inclzeroholding => new ObservableCollection<Mutualfund>(_mutualfunds);

        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(_categories);
        public ObservableCollection<Category> Subcategories
        {
            get
            {
                var collection = new ObservableCollection<Category>(_subcategories.OrderBy(x => x.Taxcategory));
                var view = (CollectionView)CollectionViewSource.GetDefaultView(collection);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Taxcategory"));
                return collection;
            }
        }

        public ObservableCollection<Capitalgain> Capitalgains
        {
            get
            {
                var collection = new ObservableCollection<Capitalgain>(_capitalgains.OrderByDescending(x => x.Year));
                var view = (CollectionView)CollectionViewSource.GetDefaultView(collection);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("FYname"));
                return collection;
            }
        }

        public decimal Amtlifetime => _mutualfunds.Sum(x => x.AmountLifetime);
        public decimal LatestAmtlifetime => _mutualfunds.Sum(x => x.FinalamountLifetime);

        public decimal Wtdavgdayslifetime
        {
            get { return _wtdavgdayslifetime; }
            private set
            {
                if (_wtdavgdayslifetime != value)
                {
                    _wtdavgdayslifetime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Xirrlifetime));
                }
            }
        }
        public double Xirrlifetime => Amtlifetime > 0 && Wtdavgdayslifetime > 0 ? Math.Pow((double)(LatestAmtlifetime / Amtlifetime), (double)(365 / Wtdavgdayslifetime)) - 1 : 0;

        public decimal Wtdavgdaysholding
        {
            get { return _wtdavgdaysholding; }
            private set
            {
                if (_wtdavgdaysholding != value)
                {
                    _wtdavgdaysholding = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(XirrHolding));
                }
            }
        }
        public double XirrHolding => Amt > 0 && Wtdavgdaysholding > 0 ? Math.Pow((double)(LatestAmt / Amt), (double)(365 / Wtdavgdaysholding)) - 1 : 0;

        public decimal Amt => _mutualfunds.Sum(x => x.BalAmt);
        public decimal LatestAmt => _mutualfunds.Sum(x => x.LatestAmount);

        public int Fundscount => _mutualfunds.Count(x => x.Units > 0);

        public Portfolio(string name, List<Transaction> transactions)
        {
            Name = name;
            _transactions = transactions != null ? transactions : new();

            Task.Run(() => initialise());
        }

        public void initialise()
        {
            CreateMutualfunds();
            MapMutualfunds();
            Calculate();
            CreateCategories();
            CreateCapitalgains();
        }

        public void CreateMutualfunds()
        {
            _mutualfunds = new();

            // Unique mutual fund - folio (with ISIN & productcode) pair list
            _mutualfunds = _transactions.DistinctBy(x => x.Name + x.Folio).Select(x => new Mutualfund(x.Name, x.Folio, x.ISIN, x.CpCode)).ToList();
            _mutualfunds = _mutualfunds.OrderBy(x => x.Name).ThenBy(x => x.Folio).ToList();

            OnPropertyChanged(nameof(Mutualfunds));
        }


        public void MapMutualfunds()
        {
            // map to master using cpCode to get ISIN, for mfs that don't have ISIN but have productcode
            if (DataService.MasterMfs.Count > 0)
            {
                var fundswithoutISIN = _mutualfunds.Where(x => x.ISIN == "" && x.CpCode != "");
                foreach (Mutualfund mutualfund in fundswithoutISIN)
                {
                    if (DataService.MasterMfs.Any(x => x.Value.CpCode == mutualfund.CpCode))
                    {
                        var matches = DataService.MasterMfs.Where(x => x.Value.CpCode == mutualfund.CpCode).ToList();

                        if (matches.Count() == 1)
                            mutualfund.ISIN = matches[0].Value.ISIN;
                        else if (matches.Count() > 1)
                        {
                            // try match with name using commonality
                            var nameWords = mutualfund.CleanName.ToLower().Trim().Split(" ");

                            decimal commonality = 0.5M;
                            foreach (var item in matches)
                            {
                                var commonalitytemp = StringService.Commonality(mutualfund.CleanName, item.Value.Name);
                                if (commonalitytemp > commonality)
                                {
                                    commonality = commonalitytemp;
                                    mutualfund.ISIN = item.Value.ISIN;
                                }
                            }
                        }
                    }
                }
            }

            // map with NAVmutualfunds
            if (DataService.NAVmutualfunds.Count != 0)
            {
                foreach (Mutualfund mutualfund in _mutualfunds)
                {
                    // first, try with ISIN
                    if (!string.IsNullOrEmpty(mutualfund.ISIN))
                    {
                        mutualfund.navMutualfund = DataService.NAVmutualfunds.FirstOrDefault(x => x.ISINgrowth == mutualfund.ISIN || x.ISINdivPayout == mutualfund.ISIN || x.ISINdivReinvest == mutualfund.ISIN);
                        if (mutualfund.navMutualfund != null)
                            continue;
                    }

                    // else try with name
                    var nameWords = mutualfund.CleanName.ToLower().Trim().Split(" ");

                    // search
                    List<NAVmutualfund> FilteredNAVmutualfunds = DataService.NAVmutualfunds.ToList();
                    foreach (string nameword in nameWords)
                    {
                        if (FilteredNAVmutualfunds.Count(x => x.CleanName.ToLower().Contains(nameword)) > 0)
                            FilteredNAVmutualfunds = FilteredNAVmutualfunds.Where(x => x.CleanName.ToLower().Contains(nameword)).ToList();
                        if (FilteredNAVmutualfunds.Count is 1 or 0)
                            break;
                    }

                    if (FilteredNAVmutualfunds.Count == 1)
                    {
                        var commonality = StringService.Commonality(mutualfund.CleanName, FilteredNAVmutualfunds[0].CleanName);
                        if (commonality > 0.5M)
                            mutualfund.navMutualfund = FilteredNAVmutualfunds[0];
                    }
                    else if (FilteredNAVmutualfunds.Count > 1)
                    {
                        // with commonality
                        decimal commonality = 0.5M;
                        NAVmutualfund tempNavfund = null;
                        foreach (NAVmutualfund navmutualfund in FilteredNAVmutualfunds)
                        {
                            var commonalitytemp = StringService.Commonality(mutualfund.CleanName, navmutualfund.CleanName);
                            if (commonalitytemp > commonality)
                            {
                                commonality = commonalitytemp;
                                tempNavfund = navmutualfund;
                            }
                        }
                        mutualfund.navMutualfund = tempNavfund;
                    }
                }
            }

            // Get tax-categories if missing
            foreach (Mutualfund mutualfund in _mutualfunds)
            {
                if (mutualfund.navMutualfund != null && mutualfund.navMutualfund.Taxcategory == TaxCategory.Uncategorised)
                    mutualfund.navMutualfund.Taxcategory = DataService.GetTaxcategory(mutualfund.navMutualfund);
            }

            OnPropertyChanged(nameof(Mutualfunds));            
        }

        public void Calculate()
        {
            // Calculation for each mutualfund by filtering transactions
            foreach (Transaction transaction in _transactions)
                transaction.BalancedTransactions.Clear();

            foreach (Mutualfund mutualfund in _mutualfunds)
            {
                var transactions = _transactions.Where(x => x.Name == mutualfund.Name && x.Folio == mutualfund.Folio).OrderBy(x => x.Date);
                foreach (Transaction item in transactions)
                    item.mutualfund = mutualfund;

                var sellTransactions = transactions.Where(x => x.Transactiontype == TransactionType.sell);
                foreach (Transaction sTransaction in sellTransactions)
                {
                    decimal balSellUnits = sTransaction.Units;

                    var prevbuyTransactions = transactions.Where(x => x.Transactiontype == TransactionType.buy && x.Date < sTransaction.Date);
                    foreach (Transaction bTransaction in prevbuyTransactions)
                    {
                        if (bTransaction.BalUnits > 0)
                        {
                            decimal units = Math.Min(bTransaction.BalUnits, balSellUnits);
                            bTransaction.BalancedTransactions.Add(new(sTransaction, units));
                            sTransaction.BalancedTransactions.Add(new(bTransaction, units));
                        }
                        balSellUnits = sTransaction.BalUnits;
                        if (balSellUnits == 0) break;
                    }
                    if (balSellUnits > 0) break;
                }

                var buyTransactions = transactions.Where(x => x.Transactiontype == TransactionType.buy);
                var buyTransactionswithholdings = buyTransactions.Where(x => x.BalUnits > 0);
                mutualfund.Units = buyTransactionswithholdings.Sum(x => x.BalUnits);
                mutualfund.BalAmt = buyTransactionswithholdings.Sum(x => x.BalAmount);

                mutualfund.AmountLifetime = buyTransactions.Sum(x => x.Amount);
                mutualfund.FinalamountLifetime = mutualfund.LatestAmount + sellTransactions.Sum(x => x.Amount);

                // unrealised capital gains
                if (mutualfund.Units > 0)
                {
                    DateTime Date_1yback = DateTime.Today.AddYears(-1);
                    DateTime Date_2yback = DateTime.Today.AddYears(-2);

                    if (mutualfund.navMutualfund?.Taxcategory == TaxCategory.Equity || mutualfund.navMutualfund?.Taxcategory == TaxCategory.GoldSilverETFs)
                    {
                        decimal costLT = buyTransactionswithholdings.Where(x => x.Date <= Date_1yback).Sum(x => x.BalUnits * x.Price);
                        decimal amountLT = buyTransactionswithholdings.Where(x => x.Date <= Date_1yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedLt = amountLT - costLT;
                        mutualfund.LtAmount = amountLT;
                        mutualfund.LtUnits = buyTransactionswithholdings.Where(x => x.Date <= Date_1yback).Sum(x => x.BalUnits);

                        decimal costST = buyTransactionswithholdings.Where(x => x.Date > Date_1yback).Sum(x => x.BalUnits * x.Price);
                        decimal amountST = buyTransactionswithholdings.Where(x => x.Date > Date_1yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedSt = amountST - costST;
                        mutualfund.StAmount = amountST;
                        mutualfund.StUnits = buyTransactionswithholdings.Where(x => x.Date > Date_1yback).Sum(x => x.BalUnits);
                    }
                    else if (mutualfund.navMutualfund?.Taxcategory == TaxCategory.Debt)
                    {
                        // Before 1 april 2023 - Lt
                        decimal costLt = buyTransactionswithholdings.Where(x => x.Date < new DateTime(2023, 4, 1)).Sum(x => x.BalUnits * x.Price);
                        decimal amountLt = buyTransactionswithholdings.Where(x => x.Date < new DateTime(2023, 4, 1)).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedLt = amountLt - costLt;
                        mutualfund.LtAmount = amountLt;
                        mutualfund.LtUnits = buyTransactionswithholdings.Where(x => x.Date < new DateTime(2023, 4, 1)).Sum(x => x.BalUnits);

                        // on or after 1 april 2023 - St
                        decimal costSt = buyTransactionswithholdings.Where(x => x.Date >= new DateTime(2023, 4, 1)).Sum(x => x.BalUnits * x.Price);
                        decimal amountSt = buyTransactionswithholdings.Where(x => x.Date >= new DateTime(2023, 4, 1)).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedSt = amountSt - costSt;
                        mutualfund.StAmount = amountSt;
                        mutualfund.StUnits = buyTransactionswithholdings.Where(x => x.Date >= new DateTime(2023, 4, 1)).Sum(x => x.BalUnits);
                    }
                    else if (mutualfund.navMutualfund?.Taxcategory == TaxCategory.Others)
                    {
                        decimal costLT = buyTransactionswithholdings.Where(x => x.Date <= Date_2yback).Sum(x => x.BalUnits * x.Price);
                        decimal amountLT = buyTransactionswithholdings.Where(x => x.Date <= Date_2yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedLt = amountLT - costLT;
                        mutualfund.LtAmount = amountLT;
                        mutualfund.LtUnits = buyTransactionswithholdings.Where(x => x.Date <= Date_2yback).Sum(x => x.BalUnits);

                        decimal costST = buyTransactionswithholdings.Where(x => x.Date > Date_2yback).Sum(x => x.BalUnits * x.Price);
                        decimal amountST = buyTransactionswithholdings.Where(x => x.Date > Date_2yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedSt = amountST - costST;
                        mutualfund.StAmount = amountST;
                        mutualfund.StUnits = buyTransactionswithholdings.Where(x => x.Date > Date_2yback).Sum(x => x.BalUnits);
                    }

                    // Wtdavgdays
                    var sumproduct = buyTransactionswithholdings.Sum(x => x.DaysHolding * x.BalAmount);
                    var sum = buyTransactionswithholdings.Sum(x => x.BalAmount);
                    mutualfund.Wtdavgdaysholding = sum > 0 ? sumproduct / sum : 0;
                }

                // Wtdavgdays - lifetime
                var sumproductlt = buyTransactions.Sum(x => x.Wtdavgdaysheld * x.Amount);
                var sumlt = buyTransactions.Sum(x => x.Amount);
                mutualfund.WtdavgdaysLifetime = sumlt > 0 ? sumproductlt / sumlt : 0;
            }

            var mutualfundswithholdings = _mutualfunds.Where(x => x.Units > 0);

            // Wtdavgdays
            var Pfsumproduct = mutualfundswithholdings.Sum(x => x.Wtdavgdaysholding * x.BalAmt);
            var Pfsum = mutualfundswithholdings.Sum(x => x.BalAmt);
            Wtdavgdaysholding = Pfsum > 0 ? Pfsumproduct / Pfsum : 0;

            // Wtdavgdays - lifetime
            var Pfsumproductlt = _mutualfunds.Sum(x => x.WtdavgdaysLifetime * x.AmountLifetime);
            var Pfsumlt = _mutualfunds.Sum(x => x.AmountLifetime);
            Wtdavgdayslifetime = Pfsumlt > 0 ? Pfsumproductlt / Pfsumlt : 0;

            // Calculate Lat Amt ratio for current funds
            var maxAmt = mutualfundswithholdings.Max(x => x.LatestAmount);
            var maxProfit = mutualfundswithholdings.Max(x => x.Profit);
            var maxUlt = mutualfundswithholdings.Max(x => x.UnrealisedLt);
            var maxUst = mutualfundswithholdings.Max(x => x.UnrealisedSt);
            var maxPropercept = mutualfundswithholdings.Max(x => x.Profitpercent);
            var maxwadays = mutualfundswithholdings.Max(x => x.Wtdavgdaysholding);
            var maxxirr = (double)mutualfundswithholdings.Max(x => x.XirrHolding);
            foreach (Mutualfund fund in mutualfundswithholdings)
            {
                fund.LatestAmtratio = (fund.LatestAmount / maxAmt) * 100;
                fund.Profitratio = (fund.Profit / maxProfit) * 100;
                fund.Ultratio = (fund.UnrealisedLt / maxUlt) * 100;
                fund.Ustratio = (fund.UnrealisedSt / maxUst) * 100;
                fund.Properratio = (fund.Profitpercent / maxPropercept) * 100;
                fund.Wadaysratio = (fund.Wtdavgdaysholding / maxwadays) * 100;
                fund.Xirrratio = (fund.XirrHolding / maxxirr) * 100;
            }

            OnPropertyChanged(nameof(Mutualfunds));
        }


        public void CreateCategories()
        {
            var mutualfundswithholdings = _mutualfunds.Where(x => x.Units > 0 && x.BalAmt > 0).OrderBy(x => x.Taxcategory);

            _categories = mutualfundswithholdings.DistinctBy(x => x.Taxcategory).Select(x => new Category(x.Taxcategory.ToString(), x.Taxcategory)).ToList();
            _subcategories = mutualfundswithholdings.DistinctBy(x => x.Subcategory).Select(x => new Category(x.Subcategory.ToString(), x.Taxcategory)).ToList();

            var totalLatestamt = mutualfundswithholdings.Sum(x => x.LatestAmount);

            foreach (Category item in _categories)
            {
                var itemfunds = mutualfundswithholdings.Where(x => x.Taxcategory.ToString() == item.Name);
                var amount = itemfunds.Sum(x => x.BalAmt);
                var latestamount = itemfunds.Sum(x => x.LatestAmount);
                var profit = itemfunds.Sum(x => x.Profit);
                var ult = itemfunds.Sum(x => x.UnrealisedLt);
                var ust = itemfunds.Sum(x => x.UnrealisedSt);
                var sumproduct = itemfunds.Sum(x => x.Wtdavgdaysholding * x.BalAmt);
                var sum = itemfunds.Sum(x => x.BalAmt);
                var wadays = sumproduct / sum;
                item.Setvalues(itemfunds.Count(), amount, latestamount, profit, ult, ust, wadays, latestamount / totalLatestamt);
            }

            var maxAmt = _categories.Max(x => x.LatestAmount);
            var maxProfit = _categories.Max(x => x.Profit);
            var maxUlt = _categories.Max(x => x.UnrealisedLt);
            var maxUst = _categories.Max(x => x.UnrealisedSt);
            var maxPropercept = _categories.Max(x => x.Profitpercent);
            var maxwadays = _categories.Max(x => x.Wtdavgdaysholding);
            var maxxirr = (double)_categories.Max(x => x.XirrHolding);

            foreach (Category item in _categories)
            {
                item.LatestAmtratio = (item.LatestAmount / maxAmt) * 100;
                item.Profitratio = (item.Profit / maxProfit) * 100;
                item.Ultratio = (item.UnrealisedLt / maxUlt) * 100;
                item.Ustratio = (item.UnrealisedSt / maxUst) * 100;
                item.Properratio = (item.Profitpercent / maxPropercept) * 100;
                item.Wadaysratio = (item.Wtdavgdaysholding / maxwadays) * 100;
                item.Xirrratio = (item.XirrHolding / maxxirr) * 100;
            }

            foreach (Category item in _subcategories)
            {
                var itemfunds = mutualfundswithholdings.Where(x => x.Subcategory == item.Name);
                var amount = itemfunds.Sum(x => x.BalAmt);
                var latestamount = itemfunds.Sum(x => x.LatestAmount);
                var profit = itemfunds.Sum(x => x.Profit);
                var ult = itemfunds.Sum(x => x.UnrealisedLt);
                var ust = itemfunds.Sum(x => x.UnrealisedSt);
                var sumproduct = itemfunds.Sum(x => x.Wtdavgdaysholding * x.BalAmt);
                var sum = itemfunds.Sum(x => x.BalAmt);
                var wadays = sumproduct / sum;
                item.Setvalues(itemfunds.Count(), amount, latestamount, profit, ult, ust, wadays, latestamount / totalLatestamt);
            }

            maxAmt = _subcategories.Max(x => x.LatestAmount);
            maxProfit = _subcategories.Max(x => x.Profit);
            maxUlt = _subcategories.Max(x => x.UnrealisedLt);
            maxUst = _subcategories.Max(x => x.UnrealisedSt);
            maxPropercept = _subcategories.Max(x => x.Profitpercent);
            maxwadays = _subcategories.Max(x => x.Wtdavgdaysholding);
            maxxirr = (double)_subcategories.Max(x => x.XirrHolding);

            foreach (Category item in _subcategories)
            {
                item.LatestAmtratio = (item.LatestAmount / maxAmt) * 100;
                item.Profitratio = (item.Profit / maxProfit) * 100;
                item.Ultratio = (item.UnrealisedLt / maxUlt) * 100;
                item.Ustratio = (item.UnrealisedSt / maxUst) * 100;
                item.Properratio = (item.Profitpercent / maxPropercept) * 100;
                item.Wadaysratio = (item.Wtdavgdaysholding / maxwadays) * 100;
                item.Xirrratio = (item.XirrHolding / maxxirr) * 100;
            }

            _subcategories = _subcategories.OrderBy(x => x.Name).ToList();

            OnPropertyChanged(nameof(Categories));
            OnPropertyChanged(nameof(Subcategories));
        }


        public void CreateCapitalgains()
        {
            _capitalgains = new List<Capitalgain>();

            int currentfy = DateTime.Today.Month < 3 ? DateTime.Today.Year : DateTime.Today.Year + 1;

            foreach (TaxCategory item in Enum.GetValues<TaxCategory>())
            {
                _capitalgains.Add(new Capitalgain(currentfy, item));
                _capitalgains.Add(new Capitalgain(currentfy - 1, item));
            }

            OnPropertyChanged(nameof(Capitalgains));
        }

    }
}
