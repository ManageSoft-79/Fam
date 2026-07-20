using System;
using System.Collections.Generic;
using System.Text;

namespace Fam
{
    public class Category
    {
        public string Name { get; private set; }
        public TaxCategory Taxcategory { get; private set; }
        public decimal Amount { get; private set; }
        public decimal LatestAmount { get; private set; }
        public decimal Profit { get; private set; }
        public int Funds { get; private set; }


        public Category(string name, TaxCategory taxcategory)
        {
            Name = name;
            Taxcategory = taxcategory;
        }

        public decimal Profitpercent => Amount > 0 ? Profit / Amount : 0;

        public decimal LatestamtShare { get; private set; }

        public decimal UnrealisedLt { get; private set; }
        public decimal UnrealisedSt { get; private set; }
        public decimal Wtdavgdaysholding { get; private set; }
        public double XirrHolding => Amount > 0 && Wtdavgdaysholding > 90 ? Math.Pow((double)(LatestAmount / Amount), (double)(365 / Wtdavgdaysholding)) - 1 : 0;

        public void Setvalues(int funds, decimal amount, decimal latestAmount, decimal profit, decimal unrealisedLt, decimal unrealisedSt, decimal wtdavgdaysholding, decimal latestamtShare)
        {
            Funds = funds;
            Amount = amount;
            LatestAmount = latestAmount;
            Profit = profit;
            UnrealisedLt = unrealisedLt;
            UnrealisedSt = unrealisedSt;
            Wtdavgdaysholding = wtdavgdaysholding;
            LatestamtShare = latestamtShare;
        }

        public decimal LatestAmtratio { get; set; }
        public decimal Profitratio { get; set; }
        public decimal Properratio { get; set; }
        public decimal Ultratio { get; set; }
        public decimal Ustratio { get; set; }
        public decimal Wadaysratio { get; set; }
        public double Xirrratio { get; set; }


    }
}
