using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Transactions;
using System.Windows.Media.Animation;

namespace Fam
{
    [DataContract]
    public class Transaction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        [DataMember]
        public DateTime Date { get; }
        [DataMember]
        private readonly string _nameoriginal;
        [DataMember]
        public string Name { get; }
        [DataMember]
        public string Folio { get; }
        [DataMember]
        public string TransactionName { get; }
        [DataMember]
        public decimal Units { get; }
        [DataMember]
        public decimal NAV { get; }
        [DataMember]
        public string ISIN { get; }
        [DataMember]
        public readonly List<Tuple<Transaction, decimal>> BalancedTransactions = new();

        [DataMember]
        public string CpCode { get; }
        [DataMember]
        public string FundName { get; }

        [DataMember]
        private readonly TransactionType _transactiontype;

        public TransactionType Transactiontype => _transactiontype;

        [DataMember]
        public Mutualfund mutualfund = null;

        public decimal Amount => Units * NAV;
        public decimal BalUnits => Units - BalancedTransactions.Sum(x => x.Item2);
        public decimal BalAmount => BalUnits * NAV;

        public Transaction(DateTime date, string name, string folio, string transactionName, decimal units, decimal price, decimal amount, string iSIN, string fundName, string cpCode)
        {
            Date = date;
            _nameoriginal = name;
            Folio = folio;
            TransactionName = transactionName;
            Units = units;
            NAV = price;
            ISIN = iSIN;
            FundName = fundName;
            CpCode = cpCode;

            // remove formerly...
            Name = _nameoriginal;
            if (Name.Contains("-(formerly", StringComparison.OrdinalIgnoreCase))
                Name = Name.Substring(0, Name.ToLower().IndexOf("-(formerly")).Trim();
            else if (Name.Contains("(formerly", StringComparison.OrdinalIgnoreCase))
                Name = Name.Substring(0, Name.ToLower().IndexOf("(formerly")).Trim();

            // set transaction type
            var transactionName_lower = transactionName.ToLower();
            if (transactionName_lower == "buy")
                _transactiontype = TransactionType.buy;
            else if (transactionName_lower == "sell")
                _transactiontype = TransactionType.sell;
            else
            {
                string[] buywords = { "sip", "purchase", "investment", "allotment", "creation" };
                string[] sellwords = { "swp", "redemption", "extinguished", "creation" };
                string[] transferwords = { "stp", "shift", "transfer", "switch" };
                if (buywords.Any(x => transactionName_lower.Contains(x))
                    || (transferwords.Any(x => transactionName_lower.Contains(x)) && transactionName_lower.Contains("in")))
                    _transactiontype = TransactionType.buy;
                else if (sellwords.Any(x => transactionName_lower.Contains(x))
                    || (transferwords.Any(x => transactionName_lower.Contains(x)) && transactionName_lower.Contains("out")))
                    _transactiontype = TransactionType.sell;
                else throw new Exception("Unknown trasaction type");
            }

            // check rejection
            if (_transactiontype == TransactionType.buy && amount < 0 && !TransactionName.ToLower().Contains("rejection"))
                TransactionName += " rejection";

            // add fundname if missing in the start
            string FundName_short = FundName.ToLower().Contains("mutual fund") ? FundName.Substring(0, FundName.ToLower().IndexOf("mutual fund")).Trim() : FundName;
            if (FundName_short != "" && !Name.Contains(FundName_short, StringComparison.OrdinalIgnoreCase))
                Name = FundName_short + " " + Name;

            BalancedTransactions = new();
        }

        public override string ToString() => Date.ToString("yyyy-MM-dd")
          + "," + Folio
          + "," + Name
          + "," + Transactiontype
          + "," + Units.ToString()
          + "," + NAV.ToString();

        public decimal Wtdavgdayslifetime // for lifetime xirr
        {
            get
            {
                if (_transactiontype == TransactionType.buy)
                {
                    var sumproduct = BalancedTransactions.Sum(x => x.Item2 * NAV * (decimal)(x.Item1.Date - Date).TotalDays) + (BalAmount * Wtdavgdaysheld);
                    var sum = BalancedTransactions.Sum(x => x.Item2 * NAV) + BalAmount;
                    return sum > 0 ? sumproduct / sum : 0;
                }
                return 0;
            }
        }

        public decimal Wtdavgdaysheld
        {
            get
            {
                if (_transactiontype == TransactionType.buy && BalUnits > 0)
                {
                    return (decimal)(DateTime.Today - Date).TotalDays;
                }
                else if (_transactiontype == TransactionType.sell)
                {
                    var sumproduct = BalancedTransactions.Sum(x => x.Item2 * x.Item1.NAV * (decimal)(Date - x.Item1.Date).TotalDays);
                    var sum = BalancedTransactions.Sum(x => x.Item2 * x.Item1.NAV);
                    return sum > 0 ? sumproduct / sum : 0;
                }
                return 0;
            }
        }

        public decimal Profit
        {
            get
            {
                if (_transactiontype == TransactionType.buy && BalUnits > 0 && mutualfund != null && mutualfund.navMutualfund != null)
                    return (mutualfund.navMutualfund.NAV - NAV) * BalUnits;
                else if (_transactiontype == TransactionType.sell)
                    return Amount - BalancedTransactions.Sum(x => x.Item2 * x.Item1.NAV);
                return 0;
            }
        }

        public double Xirr
        {
            get
            {
                if (_transactiontype == TransactionType.buy && BalUnits > 0 && Wtdavgdaysheld >= 90)
                    return BalAmount > 0 ? Math.Pow((double)((BalAmount + Profit) / BalAmount), 365 / (double)Wtdavgdaysheld) - 1 : 0;
                else if (_transactiontype == TransactionType.sell && Wtdavgdaysheld >= 90)
                {
                    decimal Costofsold = BalancedTransactions.Sum(x => x.Item2 * x.Item1.NAV);
                    return Costofsold > 0 ? Math.Pow((double)(Amount / Costofsold), 365 / (double)Wtdavgdaysheld) - 1 : 0;
                }
                return 0;
            }
        }

        private DateTime PrevFyFirstdate => new DateTime(DateTime.Today.Month < 3 ? DateTime.Today.Year - 2 : DateTime.Today.Year - 1, 4, 1);
        public bool Old => (_transactiontype == TransactionType.buy && BalUnits == 0) || (_transactiontype == TransactionType.sell && Date < PrevFyFirstdate);

        private bool ELSS => mutualfund != null && mutualfund.navMutualfund?.Subcategory == "ELSS";
        public bool ELSSunlocked => _transactiontype == TransactionType.buy && BalUnits > 0 && ELSS ? Date < DateTime.Today.AddYears(-3) : false;


    }
}
