using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fam
{
    [DataContract]
    public enum TaxCategory
    {
        [EnumMember]
        Equity = 0,

        [EnumMember]
        Debt = 1,

        [EnumMember]
        Others = 2,

        [EnumMember]
        GoldSilverETFs = 3,

        [EnumMember]
        Uncategorised = 4,
    }

    [DataContract]
    public enum TransactionType
    {
        [EnumMember]
        buy = 0,

        [EnumMember]
        sell = 1,
    }
}
