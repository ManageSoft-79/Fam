using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Fam
{
    public class NAVmutualfund
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public string Name { get; }
        public string Fund { get; private set; }
        public string Type { get; private set; }
        public string SchemeCode { get; private set; }
        private string ISIN1 { get; set; }
        private string ISIN2 { get; set; }
        public decimal NAV { get; private set; }
        public DateTime NavDate { get; private set; }

        public string CleanName { get; private set; }

        public string OnlyName { get; private set; } = "";

        /// <summary>
        /// Equity, Debt, Others, Gold Silver ETFs
        /// </summary>
        private TaxCategory _taxcategory = TaxCategory.Uncategorised;
        public TaxCategory Taxcategory
        {
            get
            {
                if (_taxcategory == TaxCategory.Uncategorised)
                {
                    if (!IsPortfolioMutualfund && PortfolioMutualfund != null)
                        _taxcategory = PortfolioMutualfund.Taxcategory;
                }
                return _taxcategory;
            }
            set
            {
                if (value != _taxcategory)
                {
                    _taxcategory = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool isFoF => new string[] { "fof", "fund of fund" }.Any(x => CleanName.ToLower().Contains(x));


        //private string MainType => Type.Substring(0, Type.IndexOf('('));
        public string Subtype => Type.Substring(Type.IndexOf('(') + 1)[..^1];
        public string MainCategory => Subtype.Contains('-') ? Subtype.Substring(0, Subtype.IndexOf('-')).Trim() : Subtype;
        private string _subcategory => Subtype.Contains('-') ? Subtype.Substring(Subtype.IndexOf('-') + 1).Trim() : Subtype;
        public string Subcategory { get; private set; }

        public string ISINgrowth => CleanName.ToLower().Contains("growth") ? ISIN1 : "-";
        public string ISINdivPayout => !CleanName.ToLower().Contains("growth") ? ISIN1 : "-";
        public string ISINdivReinvest => ISIN2;
        public string ISIN => !string.IsNullOrEmpty(ISIN1) ? ISIN1 : ISIN2;

        public override string ToString() => Name;


        public NAVmutualfund? PortfolioMutualfund = null;
        public bool IsPortfolioMutualfund => PortfolioMutualfund == this;

        public NAVmutualfund(string name, string fund, string type, string schemeCode, string iSIN1, string iSIN2, decimal nAV, DateTime navDate)
        {
            Name = name;
            Fund = fund;
            Type = type;
            SchemeCode = schemeCode;
            ISIN1 = iSIN1;
            ISIN2 = iSIN2;
            NAV = nAV;
            NavDate = navDate;

            Name = Name.Replace("( ", "(");
            Name = Regex.Replace(Name, @"\s+", " ");

            // clean name
            CleanName = Name.Replace("-", " ").Trim();
            CleanName = Regex.Replace(CleanName, @"\s+", " ");
            var cleanNamelower = CleanName.ToLower();

            if (cleanNamelower.Contains("fof") && !cleanNamelower.Contains("fund of fund"))
                CleanName += " Fund of Fund";
            else if (cleanNamelower.Contains("fund of fund"))
                CleanName += " FoF";

            if (cleanNamelower.Contains(" & "))
                CleanName = CleanName.Replace(" & ", " & and ");
            else if (cleanNamelower.Contains(" and "))
                CleanName = CleanName.Replace(" and ", " and & ");

            if (!cleanNamelower.Contains("plan"))
                CleanName += " Plan";

            // Name only
            OnlyName = Name;
            var OnlynameLower = OnlyName.ToLower();

            if (OnlynameLower.Contains('-'))
            {
                if (OnlynameLower.Contains("-dir"))
                    OnlyName = Name.Substring(0, OnlynameLower.IndexOf("-dir"));
                else if (OnlynameLower.Contains("- dir"))
                    OnlyName = Name.Substring(0, OnlynameLower.IndexOf("- dir"));
                else if (OnlynameLower.Contains("-r"))
                    OnlyName = Name.Substring(0, OnlynameLower.IndexOf("-r"));
                else if (OnlynameLower.Contains("- r"))
                    OnlyName = Name.Substring(0, OnlynameLower.IndexOf("- r"));

                OnlynameLower = OnlyName.ToLower();

                if (OnlynameLower.Contains("-g"))
                    OnlyName = Name.Substring(0, OnlynameLower.IndexOf("-g"));
                else if (OnlynameLower.Contains("- g"))
                    OnlyName = Name.Substring(0, OnlynameLower.IndexOf("- g"));
                else if (OnlynameLower.Contains("-div"))
                    OnlyName = OnlyName.Substring(0, OnlynameLower.IndexOf("-div"));
                else if (OnlynameLower.Contains("- div"))
                    OnlyName = OnlyName.Substring(0, OnlynameLower.IndexOf("- div"));
                else if (OnlynameLower.Contains("-i"))
                    OnlyName = OnlyName.Substring(0, OnlynameLower.IndexOf("-i"));
                else if (OnlynameLower.Contains("- i"))
                    OnlyName = OnlyName.Substring(0, OnlynameLower.IndexOf("- i"));
            }
            if (OnlynameLower.Contains("direct"))
                OnlyName = Name.Substring(0, OnlynameLower.IndexOf("direct"));
            else if (OnlynameLower.Contains("regular") && !OnlynameLower.Contains("savings"))
                OnlyName = Name.Substring(0, OnlynameLower.IndexOf("regular"));
            if (OnlyName.ToLower().Contains("(formerly"))
                OnlyName = OnlyName.Substring(0, OnlyName.ToLower().IndexOf("(formerly"));

            OnlyName = OnlyName.Trim();

            // custom subcategory
            Subcategory = _subcategory;
            string[] goldsilverWords = { "gold", "silver" };
            string[] fofWords = { "fof", "fund of fund" };
            if (goldsilverWords.Any(x => Name.ToLower().Contains(x)) && fofWords.Any(x => Name.ToLower().Contains(x)))
                Subcategory = "Gold/Silver FoF";

            // Category
            switch (MainCategory)
            {
                case "Debt Scheme":
                    {
                        _taxcategory = TaxCategory.Debt;
                        break;
                    }
                case "ELSS":
                case "Equity Scheme":
                    {
                        _taxcategory = TaxCategory.Equity;
                        break;
                    }
                case "Exchange Traded Funds (ETFs)":
                    {
                        if (_subcategory == "Equity ETF")
                            _taxcategory = TaxCategory.Equity;
                        if (_subcategory == "Debt ETF")
                            _taxcategory = TaxCategory.Debt;
                        break;
                    }
                case "Fund of Funds Scheme (Domestic)":
                    {
                        CheckSubcategoryforTaxcategory();
                        break;
                    }
                case "Hybrid Scheme":
                case "Hybrid Schemes":
                    {
                        switch (_subcategory)
                        {
                            case "Aggressive Hybrid Fund":
                            case "Arbitrage Fund":
                            case "Equity Savings":
                                {
                                    _taxcategory = TaxCategory.Equity;
                                    break;
                                }
                            case "Balanced Hybrid Fund":
                                {
                                    _taxcategory = TaxCategory.Others;
                                    break;
                                }
                            case "Conservative Hybrid Fund":
                                {
                                    _taxcategory = TaxCategory.Debt;
                                    break;
                                }
                            case "Dynamic Asset Allocation or Balanced Advantage":
                            case "Balanced Advantage Fund/ Dynamic Asset Allocation":
                            case "Multi Asset Allocation":
                                {
                                    CheckSubcategoryforTaxcategory();
                                    break;
                                }
                        }
                        break;
                    }
                case "Income":
                case "Income/Debt Oriented Schemes":
                    {
                        _taxcategory = TaxCategory.Debt;
                        break;
                    }
                case "Index Funds":
                    {
                        if (_subcategory == "Equity Funds")
                            _taxcategory = TaxCategory.Equity;
                        break;
                    }
                case "Money Market":
                    {
                        _taxcategory = TaxCategory.Debt;
                        break;
                    }
                case "Other Scheme":
                    {
                        switch (_subcategory)
                        {
                            case "FoF Domestic":
                            case "Index Funds":
                            case "Other  ETFs":
                                {
                                    CheckSubcategoryforTaxcategory();
                                    break;
                                }
                            case "FoF Overseas":
                                {
                                    _taxcategory = TaxCategory.Others;
                                    break;
                                }
                            case "Gold ETF":
                                {
                                    _taxcategory = TaxCategory.GoldSilverETFs;
                                    break;
                                }
                        }
                        break;
                    }
                case "Overseas Fund of Funds":
                    {
                        _taxcategory = TaxCategory.Others;
                        break;
                    }
                case "Solution Oriented Scheme":
                    {
                        CheckSubcategoryforTaxcategory();
                        break;
                    }
                default:
                    {
                        _taxcategory = TaxCategory.Uncategorised;
                        break;
                    }
            }
        }

        private void CheckSubcategoryforTaxcategory()
        {
            string[] otherWords = { "hang seng", "nyse", "s&p 500", "nasdaq", "msci", "global", "balanced hybrid" };
            string[] goldsilverWords = { "gold", "silver" };
            string[] fofWords = { "fof", "fund of fund" };
            string[] debtwords = { "gilt", "g-sec", "sdl", "bond", "corporate debt", "maturity", "aaa", "psu debt", "months debt", "conservative hybrid", "pure debt" };
            string[] equityWords = { "nifty", "sensex", "bse", "alpha", "aggressive hybrid", "pure equity", "arbitrage", "equity savings" };

            var cleanname_lowercase = CleanName.ToLower();

            if (otherWords.Any(x => cleanname_lowercase.Contains(x))) // Check other
                _taxcategory = TaxCategory.Others;
            else if (goldsilverWords.Any(x => cleanname_lowercase.Contains(x))) // check gold, silver fof / etf
            {
                if (cleanname_lowercase.Contains("etf") && !fofWords.Any(x => cleanname_lowercase.Contains(x)))
                    _taxcategory = TaxCategory.GoldSilverETFs;
                else _taxcategory = TaxCategory.Others;
            }
            else if (debtwords.Any(x => cleanname_lowercase.Contains(x)))  // check debt            
                _taxcategory = TaxCategory.Debt;
            else if (equityWords.Any(x => cleanname_lowercase.Contains(x))) // check equity
                _taxcategory = TaxCategory.Equity;
            else _taxcategory = TaxCategory.Uncategorised;
        }
    }
}
