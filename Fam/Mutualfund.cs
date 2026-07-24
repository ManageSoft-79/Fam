using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml.Linq;

namespace Fam
{
    [DataContract]
    public class Mutualfund : INotifyPropertyChanged
    {
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public readonly string CleanName;
        [DataMember]
        public string Folio { get; private set; }
        [DataMember]
        public string ISIN { get; set; }
        [DataMember]
        public string CpCode { get; private set; }
        [DataMember]
        public string SchemeCode { get; set; }

        [DataMember]
        private decimal _units;
        [DataMember]
        private decimal _balAmt;

        [DataMember]
        private decimal _wtdavgdaysholding;
        [DataMember]
        private decimal _wtdavgdaysLifetime;
        //public List<Transaction> _transactions = new();

        [DataMember]
        private string _fundName = "";
        public string FundName
        {
            get => _fundName != "" ? _fundName : Name.Substring(0, Name.IndexOf(' '));
            set { _fundName = value; }
        }

        [DataMember]
        public string Purename { get; set; } = "";

        [DataMember]
        public Goal Goal { get; set; }

        public NAVmutualfund? navMutualfund { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public string NavName => navMutualfund != null ? navMutualfund.Name : Name; // not 100% sure, in case mapping is incorrect
        public string Onlyname => navMutualfund != null ? navMutualfund.OnlyName : Name;

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

        public decimal UnrealisedLt { get; set; }

        public decimal UnrealisedSt { get; set; }

        public decimal LtAmount { get; set; }

        public decimal StAmount { get; set; }

        public decimal LtUnits { get; set; }

        public decimal StUnits { get; set; }

        public decimal NAV => Units > 0 ? Math.Round(BalAmt / Units, 4) : 0;
        public decimal LatestAmount => navMutualfund != null ? Units * navMutualfund.NAV : 0;
        public decimal Profit => navMutualfund != null ? LatestAmount - BalAmt : 0;
        public decimal Profitpercent => BalAmt > 0 ? Profit / BalAmt : 0;

        public Mutualfund(string name, string folio, string iSIN, string cpCode, string fundname)
        {
            Name = name;
            Folio = folio;
            ISIN = iSIN;
            CpCode = cpCode;
            _fundName = fundname;
            SetPurename();

            // clean name
            CleanName = Name.Replace("-", " ").Replace("(", " ").Replace(")", " ").Trim();
            CleanName = Regex.Replace(CleanName, @"\s+", " ");
        }

        public void SetPurename()
        {
            Purename = Name;
            foreach (string word in _fundName.Split(" "))
                Purename = Purename.Replace(word, "", StringComparison.OrdinalIgnoreCase).Trim();
            Purename = Regex.Replace(Purename, @"\s+", " ");
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
