// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Automate API example.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//    
//    It's a test bot that implements Momentum Trading Strategy.
//    See https://github.com/dinozords/bot
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using cAlgo.API;
using cAlgo.API.Indicators;
using NUnit.Framework.Internal;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EqualClosePriceBot : Robot
    {
        [Parameter("Window Period, N TimeFrames", DefaultValue = 100, MinValue = 30, Step = 1)]
        public int ObservedWindow { get; set; }
        [Parameter("Order Price", DefaultValue = 0, MinValue = 0, Step = 1)]
        public double OrderPrice { get; set; }
        [Parameter("Breakout Period", DefaultValue = 24 * 60 * 60, MinValue = 0, Step = 1)]
        public int BreakoutPeriod { get; set; }
        [Parameter("Position Volume", DefaultValue = 1, MinValue = 0.1, Step = 0.1)]
        public double PositionVolume { get; set; }
        [Parameter(DefaultValue = 14)]
        public int Periods { get; set; }
        [Parameter(DefaultValue = 0.002)]
        public double ATRValue { get; set; }
        [Parameter("MA Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }
        [Parameter("TakeProfitInPerc", DefaultValue = 1.5, MinValue = 0, Step = 0.01)]
        public double TakeProfitInPerc { get; set; }
        [Parameter("StopLossInPerc", DefaultValue = 0.25, MinValue = 0, Step = 0.01)]
        public double StopLossInPerc { get; set; }

        private AverageTrueRange ATR;
        [Parameter("ATRRatio", DefaultValue = 0.3, MinValue = 0.01, Step = 0.01)]
        public double ATRRatio { get; set; } 
        private Bar window;

        SignalLevelsDataSet dataSet;
        private int[] percentiles;
        private double[] volatility;

        protected override void OnStart() {
            ATR = Indicators.AverageTrueRange(Periods, MAType);
            volatility = new double[ObservedWindow];
        }

        protected override void OnBarClosed()
        {
            var last = Bars.Count;
            if (Bars.Count < ObservedWindow) {
                Print("Insufficient data to construct volatility statistics");
                return;
            }

            double[] volatility = LastBarsVolatility(ObservedWindow);
            // double[] volatility = LastBarsVolatility(WindowPeriod);
            // percentiles = calcVolatility(Bars, 1, 30)
            Print($"Last Bar: {Bars.Last(0)}");
            Print($"Volatility: [{volatility[ObservedWindow - 1]},{volatility[ObservedWindow - 2]},{volatility[ObservedWindow - 3]}]");

            var signals = new SignalLevelsDataSet().InitByLastBars(Bars, ObservedWindow);
            OrderTrigger trigger = signals.detectSignal(Bars.Last(0), ATR.Result.LastValue * ATRRatio);
            if (trigger != null) {
                Print($"Detected the point to openning order: {trigger}");
                Print($"Signals Dataset: {signals}");
            }

            return;
        }

        protected void calcVolatility() {
            
        }

        /**
          * ATR calculstion method, because unclear which type Standard or Custom ATR calculation shuld be used :(
          * Let's try any standard and look how it’ll work.
          */
        protected void calcATR() {
            
        }

        protected double[] LastBarsVolatility(int Count) {
            double[] volatility = new double[Count];
            for (int i = 1; i <= Count; i++) {
                var index = Count - i + 1;
                volatility[i-1] = (Bars.HighPrices.Last(index) - Bars.LowPrices.Last(index)) / 2;
            }
            return volatility;
        }

        protected double BarRange(Bar bar) {
            return (bar.High - bar.Low) / 2;
        }
        
    }
}
