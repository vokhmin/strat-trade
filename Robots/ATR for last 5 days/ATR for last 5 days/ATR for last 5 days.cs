using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class ATRBot : Robot
    {
        private Bars bars;

        protected override void OnStart()
        {
            bars = MarketData.GetBars(TimeFrame.Daily);
            Print("The ATRBot is started...");
        }

        protected override void OnStop()
        {
            Print("The ATRBot is stopped.");
        }

        protected override void OnBar()
        {
            var highPrices = bars.HighPrices.Reverse().Take(5).ToArray();
            var lowPrices = bars.LowPrices.Reverse().Take(5).ToArray();
            var closePrices = bars.ClosePrices.Reverse().Take(6).ToArray();

            var atr = CalculateATR(highPrices, lowPrices, closePrices);

            var index = bars.Count - 5;
            Print($"ATR for day {index} to {bars.Count - 1} is {atr}");
        }

        private double CalculateATR(double[] highPrices, double[] lowPrices, double[] closePrices)
        {
            var atrValues = new List<double>();

            // AV: Почему тут начинается с 1? Почему не с 0? Т.e. реально обрабатываются 4 бара, а не 5.
            for (int i = 1; i < highPrices.Length; i++)
            {
                var high = highPrices[i];
                var low = lowPrices[i];
                var previousClose = closePrices[i - 1];

                // AV: Это кажется полной ерундой. Предполагается что в расчете ATR используется только High и Low,
                // но по факту тут зачем то еще и closePrice.
                var atr = Math.Max(high - low, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
                atrValues.Add(atr);
            }

            return atrValues.Average();
        }
    }
}
