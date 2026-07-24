using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.SKCharts;
using System.Collections.Generic;
using System.Linq;

namespace Fam
{
    public class CustomTooltip : SKDefaultTooltip
    {
        public override void Show(IEnumerable<ChartPoint> foundPoints, Chart chart)
        {
            // 1. Filter out the target series points
            var filteredPoints = foundPoints.Where(point =>
                point.Context.Series.Name is not ("Equity" or "Debt" or "Others" or "GoldSilverETFs" or "Uncategorised"));

            if (!filteredPoints.Any())
            {
                base.Hide(chart);
                return;
            }

            // 2. Pass the clean list back to the core engine layout method
            base.Show(filteredPoints, chart);
        }

        public override void Hide(Chart chart)
        {
            // Custom hide logic
            base.Hide(chart);
        }
    }
}
