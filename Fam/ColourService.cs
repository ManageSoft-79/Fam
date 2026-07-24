using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fam
{
    public static class ColourService
    {

        public static Dictionary<string, SKColor> Subcategorycolours = new(); // category-subcategory

        public static Dictionary<TaxCategory, SKColor> Categorycolours = new Dictionary<TaxCategory, SKColor>()
        {
            {TaxCategory.GoldSilverETFs, SKColor.FromHsl(215, 60, 55)}, // blue
            {TaxCategory.Others, SKColor.FromHsl(145, 50, 50)}, // green
            {TaxCategory.Debt, SKColor.FromHsl(45,75,60)}, // yellow
            {TaxCategory.Equity, SKColor.FromHsl(5,75,60)}, // red
            {TaxCategory.Uncategorised, SKColor.FromHsl(95,5,81)}, // gray
        };

        public static void CreateSubcateogycolours(List<Tuple<string, TaxCategory>> subcategorylist)
        {
            Subcategorycolours = new Dictionary<string, SKColor>();

            foreach (TaxCategory category in Enum.GetValues<TaxCategory>())
            {
                var subcategories = subcategorylist.Where(x => x.Item2 == category).OrderBy(x=> x.Item1);

                Categorycolours[category].ToHsl(out float h, out float s, out float l);

                h -= subcategories.Count() / 2;
                s += 10;
                l += 15;
                //l -= subcategories.Count() / 2;
                foreach (Tuple<string, TaxCategory> item in subcategories)
                {
                    h += 1;
                    s -= 2;
                    //l -= 1;
                    Subcategorycolours.Add(category.ToString() + "-" + item.Item1, SKColor.FromHsl(h, s, l));
                }
            }
        }

        public static SKColor Subcategorypaint(string name)
        {
            return Subcategorycolours[name];
        }
    }
}
