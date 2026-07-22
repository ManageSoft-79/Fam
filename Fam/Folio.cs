using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fam
{
    [DataContract(IsReference = true)]
    public class Folio
    {
        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string Fundname { get; private set; }

        [DataMember]
        public Foliogroup _group = null;
        public Foliogroup Group
        {
            get => _group;
            set
            {
                if (value != _group)
                {
                    _group = value;
                    Basepf.OnPropertyChanged(null);
                }
            }
        }

        [DataMember]
        public Portfolio Basepf { get; private set; }


        public Folio(string name, string fundname, Foliogroup group, Portfolio basepf)
        {
            Name = name;
            Fundname = fundname;
            _group = group;
            Basepf = basepf;
        }

        [DataMember]
        public bool Isactive { get; set; }

        [DataMember]
        public List<Mutualfund> Fundslist { get; set; } = new();

        public string Funds => string.Join(", ", Fundslist.Select(x => x.Purename).ToArray());
    }
}
