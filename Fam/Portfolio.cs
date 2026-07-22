using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Data;

namespace Fam
{
    [DataContract]
    public class Portfolio : INotifyPropertyChanged
    {
        [DataMember]
        private string _name;
        [DataMember]
        private List<Transaction> _transactions = new();
        [DataMember]
        private List<Mutualfund> _mutualfunds = new();

        //[DataMember]
        private List<Category> _categories = new();
        //[DataMember]
        private List<Category> _subcategories = new();
        //[DataMember]
        private List<Capitalgain> _capitalgains = new();

        [DataMember]
        private List<Folio> _folios = new();
        [DataMember]
        private List<Foliogroup> _foliogroups = new();

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

        public Category MutualfundFilter = null;

        public string Name
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

        private List<Mutualfund> _filtermutualfunds => _foliofilter != null ? _mutualfunds.Where(x => Filterfolios.Any(y => y.Name == x.Folio)).ToList() : _mutualfunds;

        public decimal Amt => _filtermutualfunds.Sum(x => x.BalAmt);
        public decimal LatestAmt => _filtermutualfunds.Sum(x => x.LatestAmount);
        public decimal Profit => LatestAmt - Amt;

        public ObservableCollection<Transaction> Transactions
        {
            get
            {
                List<Transaction> list = _transactions.OrderByDescending(x => x.Date).ToList();

                if (Foliofilter != null)
                    list = list.FindAll(x => Filterfolios.Any(y => y.Name == x.Folio));
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

                var _filtermutualfunds2 = _filtermutualfunds.Where(x => x.Units > 0);

                if (MutualfundFilter != null)
                {
                    if (MutualfundFilter.Name != MutualfundFilter.Taxcategory.ToString())
                        _filtermutualfunds2 = _filtermutualfunds2.Where(x => x.Subcategory == MutualfundFilter.Name);
                    else _filtermutualfunds2 = _filtermutualfunds2.Where(x => x.Taxcategory == MutualfundFilter.Taxcategory);
                }

                //return new ObservableCollection<Mutualfund>(_filtermutualfunds2);
                var collection = new ObservableCollection<Mutualfund>(_filtermutualfunds2.OrderBy(x => x.Taxcategory));
                var view = (CollectionView)CollectionViewSource.GetDefaultView(collection);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Taxcategory"));
                return collection;
            }
        }
        //public ObservableCollection<Mutualfund> Mutualfunds_inclzeroholding => new ObservableCollection<Mutualfund>(_mutualfunds);

        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(_categories);
        public ObservableCollection<Category> Subcategories
        {
            get
            {
                //return new ObservableCollection<Category>(_subcategories);
                var collection = new ObservableCollection<Category>(_subcategories.OrderBy(x => x.Taxcategory));
                var view = (CollectionView)CollectionViewSource.GetDefaultView(collection);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Taxcategory"));
                return collection;
            }
        }

        private IEnumerable<ISeries> _categoryseries;
        public ObservableCollection<ISeries> Categoryseries => _categoryseries != null ? new(_categoryseries) : new();

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

        public ObservableCollection<Folio> Folios => new ObservableCollection<Folio>(_folios);

        private List<Folio> Filterfolios => _folios.FindAll(y => y.Group == _foliofilter);

        public ObservableCollection<Foliogroup> Foliogroups => new ObservableCollection<Foliogroup>(_foliogroups);

        public bool Aregroups => Foliogroups.Count > 1;

        private Foliogroup? _foliofilter;
        public Foliogroup? Foliofilter
        {
            get
            {
                if (_foliofilter == null && _foliogroups.Count == 1)
                    _foliofilter = _foliogroups[0];
                return _foliofilter;
            }
            set
            {
                if (_foliofilter != value)
                {
                    _foliofilter = value;
                    Calculate();
                }
            }
        }


        public Portfolio(string name, List<Transaction> transactions)
        {
            _name = name;
            _transactions = transactions != null ? transactions : new();

            Task.Run(() => initialise());
        }


        private void initialise()
        {
            CreateMutualfunds();
            MapMutualfunds(); //
            CalculateMutualfunds(); //
            Calculate(); //
            CreateCategories(); //
            CreateCapitalgains(); //
            CreateFolios();
        }

        public void Load()
        {
            MapMutualfunds();
            CalculateMutualfunds();
            Calculate();
            CreateCategories();
            CreateCapitalgains();
        }

        private void CreateMutualfunds()
        {
            // Unique mutual fund - folio (with ISIN & productcode) list
            _mutualfunds = _transactions.DistinctBy(x => x.Name + x.Folio).Select(x => new Mutualfund(x.Name, x.Folio, x.ISIN, x.CpCode, x.FundName)).ToList();
            _mutualfunds = _mutualfunds.OrderBy(x => x.Name).ThenBy(x => x.Folio).ToList();

            OnPropertyChanged(nameof(Mutualfunds));
        }

        private void MapMutualfunds()
        {
            // map with master using cpCode to get ISIN, if any mfs don't have ISIN but have cpCode
            if (_mutualfunds.Any(x => x.ISIN == "" && x.CpCode != "") && DataService.MasterMfs.Count > 0)
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
                            // try match with name commonality
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
                    // first try with Schemecode if present
                    if (!string.IsNullOrEmpty(mutualfund.SchemeCode))
                    {
                        mutualfund.navMutualfund = DataService.NAVmutualfunds.FirstOrDefault(x => x.SchemeCode == mutualfund.SchemeCode);
                    }

                    // second, try with ISIN
                    if (mutualfund.navMutualfund == null && !string.IsNullOrEmpty(mutualfund.ISIN))
                    {
                        mutualfund.navMutualfund = DataService.NAVmutualfunds.FirstOrDefault(x => x.ISINgrowth == mutualfund.ISIN || x.ISINdivPayout == mutualfund.ISIN || x.ISINdivReinvest == mutualfund.ISIN);
                    }

                    // else try with name
                    if (mutualfund.navMutualfund == null)
                    {
                        var nameWords = mutualfund.CleanName.ToLower().Trim().Split(" ");

                        // search by name words
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

                    // set schemecode, and fundname if absent
                    if (mutualfund.navMutualfund != null)
                    {
                        mutualfund.SchemeCode = mutualfund.navMutualfund.SchemeCode;
                        mutualfund.FundName = mutualfund.navMutualfund.FundName;
                        mutualfund.SetPurename();
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

        private void CalculateMutualfunds()
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

                    // buy
                    if (mutualfund.navMutualfund?.Taxcategory == TaxCategory.Equity || mutualfund.navMutualfund?.Taxcategory == TaxCategory.GoldSilverETFs)
                    {
                        // >1y Lt
                        decimal costLT = buyTransactionswithholdings.Where(x => x.Date <= Date_1yback).Sum(x => x.BalUnits * x.NAV);
                        decimal amountLT = buyTransactionswithholdings.Where(x => x.Date <= Date_1yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedLt = amountLT - costLT;
                        mutualfund.LtAmount = amountLT;
                        mutualfund.LtUnits = buyTransactionswithholdings.Where(x => x.Date <= Date_1yback).Sum(x => x.BalUnits);

                        // <1y St
                        decimal costST = buyTransactionswithholdings.Where(x => x.Date > Date_1yback).Sum(x => x.BalUnits * x.NAV);
                        decimal amountST = buyTransactionswithholdings.Where(x => x.Date > Date_1yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedSt = amountST - costST;
                        mutualfund.StAmount = amountST;
                        mutualfund.StUnits = buyTransactionswithholdings.Where(x => x.Date > Date_1yback).Sum(x => x.BalUnits);
                    }
                    else if (mutualfund.navMutualfund?.Taxcategory == TaxCategory.Debt)
                    {
                        // Before 1 april 2023 - Lt
                        decimal costLt = buyTransactionswithholdings.Where(x => x.Date < new DateTime(2023, 4, 1)).Sum(x => x.BalUnits * x.NAV);
                        decimal amountLt = buyTransactionswithholdings.Where(x => x.Date < new DateTime(2023, 4, 1)).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedLt = amountLt - costLt;
                        mutualfund.LtAmount = amountLt;
                        mutualfund.LtUnits = buyTransactionswithholdings.Where(x => x.Date < new DateTime(2023, 4, 1)).Sum(x => x.BalUnits);

                        // on or after 1 april 2023 - St
                        decimal costSt = buyTransactionswithholdings.Where(x => x.Date >= new DateTime(2023, 4, 1)).Sum(x => x.BalUnits * x.NAV);
                        decimal amountSt = buyTransactionswithholdings.Where(x => x.Date >= new DateTime(2023, 4, 1)).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedSt = amountSt - costSt;
                        mutualfund.StAmount = amountSt;
                        mutualfund.StUnits = buyTransactionswithholdings.Where(x => x.Date >= new DateTime(2023, 4, 1)).Sum(x => x.BalUnits);
                    }
                    else if (mutualfund.navMutualfund?.Taxcategory == TaxCategory.Others)
                    {
                        // >2y Lt
                        decimal costLT = buyTransactionswithholdings.Where(x => x.Date <= Date_2yback).Sum(x => x.BalUnits * x.NAV);
                        decimal amountLT = buyTransactionswithholdings.Where(x => x.Date <= Date_2yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedLt = amountLT - costLT;
                        mutualfund.LtAmount = amountLT;
                        mutualfund.LtUnits = buyTransactionswithholdings.Where(x => x.Date <= Date_2yback).Sum(x => x.BalUnits);

                        // <2y St
                        decimal costST = buyTransactionswithholdings.Where(x => x.Date > Date_2yback).Sum(x => x.BalUnits * x.NAV);
                        decimal amountST = buyTransactionswithholdings.Where(x => x.Date > Date_2yback).Sum(x => x.BalUnits) * mutualfund.navMutualfund.NAV;
                        mutualfund.UnrealisedSt = amountST - costST;
                        mutualfund.StAmount = amountST;
                        mutualfund.StUnits = buyTransactionswithholdings.Where(x => x.Date > Date_2yback).Sum(x => x.BalUnits);
                    }

                    // Wtdavgdays
                    var sumproduct = buyTransactionswithholdings.Sum(x => x.Wtdavgdaysheld * x.BalAmount);
                    var sum = buyTransactionswithholdings.Sum(x => x.BalAmount);
                    mutualfund.Wtdavgdaysholding = sum > 0 ? sumproduct / sum : 0;
                }

                // Wtdavgdays - lifetime
                var sumproductlt = buyTransactions.Sum(x => x.Wtdavgdayslifetime * x.Amount);
                var sumlt = buyTransactions.Sum(x => x.Amount);
                mutualfund.WtdavgdaysLifetime = sumlt > 0 ? sumproductlt / sumlt : 0;
            }

            OnPropertyChanged(nameof(Mutualfunds));
        }

        private void Calculate()
        {
            var mutualfundswithholdings = _filtermutualfunds.Where(x => x.Units > 0);

            // Wtdavgdays
            var Pfsumproduct = mutualfundswithholdings.Sum(x => x.Wtdavgdaysholding * x.BalAmt);
            var Pfsum = mutualfundswithholdings.Sum(x => x.BalAmt);
            Wtdavgdaysholding = Pfsum > 0 ? Pfsumproduct / Pfsum : 0;

            // Wtdavgdays - lifetime
            var Pfsumproductlt = _mutualfunds.Sum(x => x.WtdavgdaysLifetime * x.AmountLifetime);
            var Pfsumlt = _mutualfunds.Sum(x => x.AmountLifetime);
            Wtdavgdayslifetime = Pfsumlt > 0 ? Pfsumproductlt / Pfsumlt : 0;

            // Calculate ratio for current funds
            var maxAmt = mutualfundswithholdings.Max(x => x.LatestAmount);
            var maxProfit = mutualfundswithholdings.Max(x => x.Profit);
            var maxCg = mutualfundswithholdings.Max(x => Math.Max(x.UnrealisedLt, x.UnrealisedSt));
            var maxPropercept = mutualfundswithholdings.Max(x => x.Profitpercent);
            var maxwadays = mutualfundswithholdings.Max(x => x.Wtdavgdaysholding);
            var maxxirr = (double)mutualfundswithholdings.Max(x => x.XirrHolding);
            foreach (Mutualfund fund in mutualfundswithholdings)
            {
                fund.LatestAmtratio = (fund.LatestAmount / maxAmt) * 100;
                fund.Profitratio = (fund.Profit / maxProfit) * 100;
                fund.Ultratio = (fund.UnrealisedLt / maxCg) * 100;
                fund.Ustratio = (fund.UnrealisedSt / maxCg) * 100;
                fund.Properratio = (fund.Profitpercent / maxPropercept) * 100;
                fund.Wadaysratio = (fund.Wtdavgdaysholding / maxwadays) * 100;
                fund.Xirrratio = (fund.XirrHolding / maxxirr) * 100;
            }

            OnPropertyChanged(null);
        }

        private void CreateCategories()
        {
            var mutualfundswithholdings = _mutualfunds.Where(x => x.Units > 0 && x.BalAmt > 0).OrderBy(x => x.Taxcategory);

            _categories = mutualfundswithholdings.DistinctBy(x => x.Taxcategory).Select(x => new Category(x.Taxcategory.ToString(), x.Taxcategory)).ToList();
            _subcategories = mutualfundswithholdings.DistinctBy(x => x.Subcategory).Select(x => new Category(x.Subcategory.ToString(), x.Taxcategory)).ToList();

            var totalLatestamt = mutualfundswithholdings.Sum(x => x.LatestAmount);

            // categories
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

            // ratios
            var maxAmt = _categories.Max(x => x.LatestAmount);
            var maxProfit = _categories.Max(x => x.Profit);
            var maxCg = _categories.Max(x => Math.Max(x.UnrealisedLt, x.UnrealisedSt));
            var maxPropercept = _categories.Max(x => x.Profitpercent);
            var maxwadays = _categories.Max(x => x.Wtdavgdaysholding);
            var maxxirr = (double)_categories.Max(x => x.XirrHolding);

            foreach (Category item in _categories)
            {
                item.LatestAmtratio = (item.LatestAmount / maxAmt) * 100;
                item.Profitratio = (item.Profit / maxProfit) * 100;
                item.Ultratio = (item.UnrealisedLt / maxCg) * 100;
                item.Ustratio = (item.UnrealisedSt / maxCg) * 100;
                item.Properratio = (item.Profitpercent / maxPropercept) * 100;
                item.Wadaysratio = (item.Wtdavgdaysholding / maxwadays) * 100;
                item.Xirrratio = (item.XirrHolding / maxxirr) * 100;
            }

            // subcategories
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

            // ratios
            maxAmt = _subcategories.Max(x => x.LatestAmount);
            maxProfit = _subcategories.Max(x => x.Profit);
            maxCg = _subcategories.Max(x => Math.Max(x.UnrealisedLt, x.UnrealisedSt));
            maxPropercept = _subcategories.Max(x => x.Profitpercent);
            maxwadays = _subcategories.Max(x => x.Wtdavgdaysholding);
            maxxirr = (double)_subcategories.Max(x => x.XirrHolding);

            foreach (Category item in _subcategories)
            {
                item.LatestAmtratio = (item.LatestAmount / maxAmt) * 100;
                item.Profitratio = (item.Profit / maxProfit) * 100;
                item.Ultratio = (item.UnrealisedLt / maxCg) * 100;
                item.Ustratio = (item.UnrealisedSt / maxCg) * 100;
                item.Properratio = (item.Profitpercent / maxPropercept) * 100;
                item.Wadaysratio = (item.Wtdavgdaysholding / maxwadays) * 100;
                item.Xirrratio = (item.XirrHolding / maxxirr) * 100;
            }

            _subcategories = _subcategories.OrderBy(x => x.Name).ToList();

            OnPropertyChanged(nameof(Categories));
            OnPropertyChanged(nameof(Subcategories));
        }

        public void CreateCategorycharts()
        {
            // inner chart
            _categoryseries = _categories.Select(x => new PieSeries<double> { Values = new double[] { (double)x.LatestAmount }, Name = x.Name, MaxRadialColumnWidth = 100, OuterRadiusOffset = 200 }).ToArray();
            //    Categoryseries = new ISeries[]
            //{
            //        new PieSeries<double> {Values=new double[] {(double)_categories[0].LatestAmount }, MaxRadialColumnWidth=100, OuterRadiusOffset=200, Fill=new SolidColorPaint(SKColors.DarkGreen) },
            //        new PieSeries<double> {Values=new double[] {(double)_categories[1].LatestAmount  }, MaxRadialColumnWidth=100, OuterRadiusOffset=200, Fill=new SolidColorPaint(SKColors.Beige) },
            //         new PieSeries<double> {Values=new double[] {(double)_categories[2].LatestAmount  }, MaxRadialColumnWidth=100, OuterRadiusOffset=200, Fill=new SolidColorPaint(SKColors.CornflowerBlue) },
            //          new PieSeries<double> {Values=new double[] {(double)_categories[3].LatestAmount  }, MaxRadialColumnWidth=100, OuterRadiusOffset=200, Fill=new SolidColorPaint(SKColors.Salmon) }
            //};

            // outer chart
            var _subcategoryseries = _subcategories.Select(x => new PieSeries<double> { Values = new double[] { (double)x.LatestAmount }, Name = x.Name, MaxRadialColumnWidth = 100, OuterRadiusOffset = 0 }).ToArray();

            _categoryseries = _categoryseries.Concat(_subcategoryseries);

            OnPropertyChanged(nameof(Categoryseries));

            //Subcategoryseries = new ISeries[]
            //{
            //    new PieSeries<double> {Values=new double[] {15 }, MaxRadialColumnWidth=100, Fill=new SolidColorPaint(SKColors.Tomato) },
            //    new PieSeries<double> {Values=new double[] {25}, MaxRadialColumnWidth=100, Fill=new SolidColorPaint(SKColors.MediumSeaGreen) },
            //    new PieSeries<double> {Values=new double[] {60}, MaxRadialColumnWidth=100, Fill=new SolidColorPaint(SKColors.LightGray) }
            //};
        }

        private void CreateCapitalgains()
        {
            _capitalgains = new List<Capitalgain>();

            int currentfy = DateTime.Today.Month < 3 ? DateTime.Today.Year : DateTime.Today.Year + 1;

            foreach (TaxCategory item in Enum.GetValues<TaxCategory>())
            {
                _capitalgains.Add(new Capitalgain(currentfy, item));
                _capitalgains.Add(new Capitalgain(currentfy - 1, item));
            }

            foreach (Capitalgain item in _capitalgains)
            {
                var _mutualfundsCg = _mutualfunds.Where(x => x.Taxcategory == item.Taxcategory);
                var _transactionsCg = _transactions.Where(x => _mutualfundsCg.Any(y => y.Name == x.Name && y.Folio == x.Folio) && x.Transactiontype == TransactionType.sell && x.Date >= item.FirstDate && x.Date <= item.LastDate);

                item.BookedLt = 0;
                item.BookedSt = 0;

                // since FY 2026
                if (item.Taxcategory == TaxCategory.Equity || item.Taxcategory == TaxCategory.GoldSilverETFs)
                {
                    // >1y Lt                     
                    foreach (Transaction trsn in _transactionsCg)
                    {
                        item.BookedLt += trsn.BalancedTransactions.Where(x => x.Item1.Date <= trsn.Date.AddYears(-1)).Sum(x => x.Item2 * (trsn.NAV - x.Item1.NAV));
                        item.BookedSt += trsn.BalancedTransactions.Where(x => x.Item1.Date > trsn.Date.AddYears(-1)).Sum(x => x.Item2 * (trsn.NAV - x.Item1.NAV));
                    }
                }
                else if (item.Taxcategory == TaxCategory.Debt)
                {
                    // Before 1 april 2023 - Lt
                    foreach (Transaction trsn in _transactionsCg)
                    {
                        item.BookedLt += trsn.BalancedTransactions.Where(x => x.Item1.Date < new DateTime(2023, 4, 1)).Sum(x => x.Item2 * (trsn.NAV - x.Item1.NAV));
                        item.BookedSt += trsn.BalancedTransactions.Where(x => x.Item1.Date >= new DateTime(2023, 4, 1)).Sum(x => x.Item2 * (trsn.NAV - x.Item1.NAV));
                    }
                }
                else if (item.Taxcategory == TaxCategory.Others)
                {
                    // >2y Lt 
                    foreach (Transaction trsn in _transactionsCg)
                    {
                        item.BookedLt += trsn.BalancedTransactions.Where(x => x.Item1.Date <= trsn.Date.AddYears(-2)).Sum(x => x.Item2 * (trsn.NAV - x.Item1.NAV));
                        item.BookedSt += trsn.BalancedTransactions.Where(x => x.Item1.Date > trsn.Date.AddYears(-2)).Sum(x => x.Item2 * (trsn.NAV - x.Item1.NAV));
                    }
                }
            }

            _capitalgains.RemoveAll(x => x.Taxcategory == TaxCategory.Uncategorised && x.BookedLt == 0 && x.BookedSt == 0);

            OnPropertyChanged(nameof(Capitalgains));
        }

        public decimal Amtlifetime => _filtermutualfunds.Sum(x => x.AmountLifetime);
        public decimal LatestAmtlifetime => _filtermutualfunds.Sum(x => x.FinalamountLifetime);

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

        public int Fundscount => _mutualfunds.Count(x => x.Units > 0);


        private void CreateFolios()
        {
            _foliogroups ??= new();
            var defaultgroup = new Foliogroup("Default");
            if (_foliogroups.Count == 0)
                _foliogroups.Add(defaultgroup);
            else defaultgroup = null;

            _folios ??= new();
            var folios = _mutualfunds.DistinctBy(x => x.Folio).Select(x => new Folio(x.Folio, x.FundName, defaultgroup, this)).OrderBy(x => x.Fundname).ThenBy(x => x.Name).ToList();

            foreach (Folio item in folios)
            {
                if (!_folios.Any(x => x.Name == item.Name && x.Fundname == item.Fundname))
                    _folios.Add(item);
            }

            foreach (Folio item in _folios)
            {
                item.Isactive = _mutualfunds.Where(x => x.Folio == item.Name && x.FundName == item.Fundname).Sum(x => x.Units) > 0;
                item.Fundslist ??= new();
                if (item.Isactive)
                    item.Fundslist = _mutualfunds.Where(x => x.Units > 0 && x.Folio == item.Name && x.FundName == item.Fundname).ToList();
            }

            OnPropertyChanged(nameof(Folios));
            OnPropertyChanged(nameof(Foliogroups));
            OnPropertyChanged(nameof(Foliofilter));
        }

        public void AddFoliogroup(string groupname)
        {
            _foliogroups.Add(new Foliogroup(groupname));
            OnPropertyChanged(nameof(Foliogroups));
        }

        public void RemoveFoliogroup(Foliogroup group)
        {
            if (_foliogroups.Contains(group) && _foliogroups.Count() > 1)
            {
                foreach (Folio folio in _folios)
                    if (folio.Group == group)
                        return;

                _foliogroups.Remove(group);
                OnPropertyChanged(nameof(Foliogroups));
            }
        }
    }
}
