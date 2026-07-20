using System;
using System.Collections.Generic;
using System.Text;

namespace Fam
{
    internal class MasterMf
    {
        public readonly string Name;
        public readonly string ISIN;
        public readonly string CpCode;

        public MasterMf(string name, string iSIN, string cpCode)
        {
            Name = name;
            ISIN = iSIN;
            CpCode = cpCode;
        }
    }
}
