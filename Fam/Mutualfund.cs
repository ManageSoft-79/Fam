using System.ComponentModel;
using System.Formats.Tar;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Fam
{
    [DataContract]
    public class Mutualfund : INotifyPropertyChanged
    {
        [DataMember]
        public string Name { get; }
        [DataMember]
        public readonly string CleanName;
        [DataMember]
        public string Folio { get; }
        [DataMember]
        public string ISIN { get; set; }
        [DataMember]
        public string CpCode { get; }

        [DataMember]
        private decimal _units;
        [DataMember]
        private decimal _balAmt;
        [DataMember]
        private decimal _unrealisedLt;
        [DataMember]
        private decimal _unrealisedSt;
        [DataMember]
        private decimal _ltAmount;
        [DataMember]
        private decimal _stAmount;
        [DataMember]
        private decimal _ltUnits;
        [DataMember]
        private decimal _stUnits;
        [DataMember]
        private decimal _wtdavgdaysholding;
        [DataMember]
        private decimal _wtdavgdaysLifetime;
        //public List<Transaction> _transactions = new();

        public NAVmutualfund? navMutualfund { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public string NavName => navMutualfund != null ? navMutualfund.Name : Name;

        public TaxCategory Taxcategory => navMutualfund != null ? navMutualfund.Taxcategory : TaxCategory.Uncategorised;
        public string Subcategory => navMutualfund != null ? navMutualfund.Subcategory : "Unknown";

        public override string ToString() => Name + " - " + Folio;

        public decimal Units
        {
            get { return _units; }
            set
            {
                if (_units != value)
                {
                    _units = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal BalAmt
        {
            get { return _balAmt; }
            set
            {
                if (_balAmt != value)
                {
                    _balAmt = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AmountLifetime { get; set; }
        public decimal FinalamountLifetime { get; set; }

        public decimal UnrealisedLt
        {
            get { return _unrealisedLt; }
            set
            {
                if (_unrealisedLt != value)
                {
                    _unrealisedLt = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal UnrealisedSt
        {
            get { return _unrealisedSt; }
            set
            {
                if (_unrealisedSt != value)
                {
                    _unrealisedSt = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal LtAmount
        {
            get { return _ltAmount; }
            set
            {
                if (_ltAmount != value)
                {
                    _ltAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal StAmount
        {
            get { return _stAmount; }
            set
            {
                if (_stAmount != value)
                {
                    _stAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal LtUnits
        {
            get { return _ltUnits; }
            set
            {
                if (_ltUnits != value)
                {
                    _ltUnits = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal StUnits
        {
            get { return _stUnits; }
            set
            {
                if (_stUnits != value)
                {
                    _stUnits = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Price => Units > 0 ? Math.Round(BalAmt / Units, 4) : 0;
        public decimal LatestAmount => navMutualfund != null ? Units * navMutualfund.NAV : 0;
        public decimal Profit => navMutualfund != null ? LatestAmount - BalAmt : 0;
        public decimal Profitpercent => BalAmt > 0 ? Profit / BalAmt : 0;

        public Mutualfund(string name, string folio, string iSIN, string cpCode)
        {
            Name = name;
            Folio = folio;
            ISIN = iSIN;
            CpCode = cpCode;
            //_transactions = new();

            // clean name
            CleanName = Name.Replace("-", " ").Replace("(", " ").Replace(")", " ").Trim();
            CleanName = Regex.Replace(CleanName, @"\s+", " ");
        }


        public decimal LatestAmtratio { get; set; }
        public decimal Profitratio { get; set; }
        public decimal Ultratio { get; set; }
        public decimal Ustratio { get; set; }
        public decimal Properratio { get; set; }
        public decimal Wadaysratio { get; set; }
        public double Xirrratio { get; set; }


        public decimal WtdavgdaysLifetime
        {
            get { return _wtdavgdaysLifetime; }
            set
            {
                if (_wtdavgdaysLifetime != value)
                {
                    _wtdavgdaysLifetime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(XirrLifetime));
                }
            }
        }

        public double XirrLifetime => AmountLifetime > 0 && WtdavgdaysLifetime > 0 ? Math.Pow((double)(FinalamountLifetime / AmountLifetime), (double)(365 / WtdavgdaysLifetime)) - 1 : 0;

        public decimal Wtdavgdaysholding
        {
            get { return _wtdavgdaysholding; }
            set
            {
                if (_wtdavgdaysholding != value)
                {
                    _wtdavgdaysholding = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(XirrHolding));
                }
            }
        }

        public double XirrHolding => BalAmt > 0 && Wtdavgdaysholding > 90 ? Math.Pow((double)(LatestAmount / BalAmt), (double)(365 / Wtdavgdaysholding)) - 1 : 0;


    }
}
