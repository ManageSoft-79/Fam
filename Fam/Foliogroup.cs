using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fam
{
    [DataContract(IsReference = true)]
    public class Foliogroup
    {
        [DataMember]
        public string Name { get; set; }

        public Foliogroup(string name)
        {
            Name = name;
        }
    }
}
