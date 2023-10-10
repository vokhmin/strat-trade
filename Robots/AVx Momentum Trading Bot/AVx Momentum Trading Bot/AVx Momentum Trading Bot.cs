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
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EqualClosePriceBot : Robot
    {
        [Parameter("Window Period, N TimeFrames", DefaultValue = 100, MinValue = 30, Step = 1)]
        public int WindowPeriod { get; set }
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
        private Bar window

        protected override void OnStart() {
            ATR = Indicators.AverageTrueRange(Periods, MAType);
        }

        protected override void OnBar()
        {
            if (Bars.Count < WindowPeriod) {
                Print("Insufficient data to construct volatility statistics")
                return;
            }
            calcVolatility(Bars, 1)

            Bar bar0 = Bars.Last(1);
            double high0 = bar0.High;
            double low0 = bar0.Low;

            if (high0 == Bars.Last(3).High && high0 == Bars.Last(2).High) {
                Print("Tirple-High Signal: {0}", bar0);
                Bar? prev = ResistanceLevel(high0, true);
                if (prev != null) {
                    Print("Win! {0}...{1}-{2}", prev?.OpenTime, Bars.Last(3).OpenTime, bar0.OpenTime);
                    var atrInPips = ATR.Result.Last(1) * (Symbol.TickSize / Symbol.PipSize * Math.Pow(10, Symbol.Digits));
                    var stopLossInPips = atrInPips * StopLossInPerc;
                    var takeProfitInPips = atrInPips * TakeProfitInPerc;
                    PlaceLimitOrder(TradeType.Buy, Symbol.Name, PositionVolume, high0 + 0.01,
                        "Stop Buy", stopLossInPips, takeProfitInPips);
                }
            }
            if (low0 == Bars.Last(3).Low && low0 == Bars.Last(2).Low) {
                Print("Tirple-Low Signal: {0}", bar0);
                Bar? prev = ResistanceLevel(low0, false);
                if (prev != null) {
                    Print("Win! {0}...{1}-{2}", prev?.OpenTime, Bars.Last(3).OpenTime, bar0.OpenTime);     
                    var atrInPips = ATR.Result.Last(1) * (Symbol.TickSize / Symbol.PipSize * Math.Pow(10, Symbol.Digits));
                    var stopLossInPips = atrInPips * StopLossInPerc;
                    var takeProfitInPips = atrInPips * TakeProfitInPerc;
                    PlaceLimitOrder(TradeType.Sell, Symbol.Name, PositionVolume, low0 - 0.01,
                        "Stop Sell", stopLossInPips, takeProfitInPips);
                }
            }

        }

        private Bar? ResistanceLevel(double level, bool high) {
            for (int i = 0; i < BreakoutPeriod; i++) {
                Bar bar = Bars.Last(3 + i);
                if (high) {
                    if (bar.High == level) {
                        return bar;
                    } else {
                        if (bar.Low == level) {
                            return bar;
                        }
                    }
                }
            }
            return null;
        }
        
        /**
          * ATR calculstion method, because unclear which type Standard or Custom ATR calculation shuld be used :(
          * Let's try any standard and look how it’ll work.
          */
        protected calcATR() {
            
        }
        
    }
    
}
