using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class AnomalyLevelRobot : Robot
    {
        [Parameter("Anomaly Multiplier", DefaultValue = 5)]
        public double AnomalyMultiplier { get; set; }

        private Bars _bars;

        protected override void OnStart()
        {
            _bars = MarketData.GetBars(TimeFrame);
        }

        protected override void OnBar()
        {
            // Calculate the average ATR for the past 5 bars, excluding the bars with the highest and lowest ATR
double maxAtr = double.MinValue;
double minAtr = double.MaxValue;
int maxAtrIndex = -1;
int minAtrIndex = -1;
double sum = 0;
for (int i = _bars.ClosePrices.Count - 1; i >= _bars.ClosePrices.Count - 5; i--)
{
    double atr = _bars.HighPrices[i] - _bars.LowPrices[i];
    sum += atr;
    if (atr > maxAtr)
    {
        maxAtr = atr;
        maxAtrIndex = i;
    }
    if (atr < minAtr)
    {
        minAtr = atr;
        minAtrIndex = i;
    }
}
double atrAvg = (sum - maxAtr - minAtr) / 3;

// Find the anomalous bar
int anomalousIndex = -1;
for (int i = _bars.ClosePrices.Count - 1; i >= _bars.ClosePrices.Count - 5; i--)
{
    double atr = _bars.HighPrices[i] - _bars.LowPrices[i];
    if (atr >= atrAvg * 2)
    {
        anomalousIndex = i;
        break;
    }
}

// Output the level price for the anomalous bar
if (anomalousIndex != -1)
{
    if (_bars.ClosePrices[anomalousIndex] > _bars.OpenPrices[anomalousIndex])
    {
        double levelPriceMax = double.MinValue;
        for (int i = anomalousIndex; i > anomalousIndex - 5 && i >= 0; i--)
        {
            if (_bars.HighPrices[i] > levelPriceMax)
            {
                levelPriceMax = _bars.HighPrices[i];
            }
        }
       
        Print("Anomaly High: " + levelPriceMax + "::" + _bars.HighPrices.LastValue);
    }
    else
    {
        double levelPriceMin = double.MaxValue;
        for (int i = anomalousIndex; i > anomalousIndex - 5 && i >= 0; i--)
        {
            if (_bars.LowPrices[i] < levelPriceMin)
            {
                levelPriceMin = _bars.LowPrices[i];
            }
        }
        Print("Anomaly Low: " + levelPriceMin + "::" + _bars.LowPrices.LastValue);
    }
}
}
}
}