using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fam
{
    [DataContract]
    public class Goal
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public List<string> ISIN { get; set; } = new();
        [DataMember]
        public List<string> SchemeCode { get; set; } = new();
        [DataMember]
        public List<string> CpCode { get; } = new();


        public Goal(string name, decimal amount)
        {
            Name = name;
            Amount = amount;
        }

    }
}
