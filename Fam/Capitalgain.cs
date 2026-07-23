using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fam
{
    public class Capitalgain
    {
        public TaxCategory Taxcategory { get; private set; }
        public int Year { get; private set; }
        public string FYname => "FY " + Year;

        public Capitalgain(int year, TaxCategory taxcategory)
        {
            Year = year;
            Taxcategory = taxcategory;
        }

        public DateTime FirstDate => new DateTime(Year - 1, 4, 1);
        public DateTime LastDate => new DateTime(Year, 3, 31);

        public decimal BookedLt { get; set; }
        public decimal BookedSt { get; set; }
    }
}
