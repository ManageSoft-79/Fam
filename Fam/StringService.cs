using System;
using System.Collections.Generic;
using System.Text;

namespace Fam
{
    public class StringService
    {

        public static decimal Commonality(string string1, string string2)
        {
            var string1words = string1.ToLower().Split(" ");
            var string2words = string2.ToLower().Split(" ");
            decimal commonwordscount = string2words.Count(x => string1words.Contains(x));
            decimal uncommonwordscount = string2words.Count(x => !string1words.Contains(x));

            return commonwordscount / uncommonwordscount;
        }

    }
}
