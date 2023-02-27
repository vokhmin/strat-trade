// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Automate API example.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//    
//    All changes to this file might be lost on the next application update.
//    If you are going to modify this file please make a copy using the "Duplicate" command.
//
//    The "Sample Martingale cBot" creates a random Sell or Buy order. If the Stop loss is hit, a new 
//    order of the same type (Buy / Sell) is created with double the Initial Volume amount. The cBot will 
//    continue to double the volume amount for  all orders created until one of them hits the take Profit. 
//    After a Take Profit is hit, a new random Buy or Sell order is created with the Initial Volume amount.
//
// -------------------------------------------------------------------------------------------------

using System;
using cAlgo.API;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EqualClosePriceBot : Robot
    {
        [Parameter("Order Price", DefaultValue = 0, MinValue = 0, Step = 1)]
        public double OrderPrice { get; set; }
        [Parameter("Breakout Period", DefaultValue = 24 * 60 * 60, MinValue = 0, Step = 1)]
        public int BreakoutPeriod { get; set; }

        protected override void OnBar()
        {
            if (Bars.Count < 3)
                return;

            Bar bar0 = Bars.Last(0);
            double high0 = bar0.High;
            double low0 = bar0.Low;

            if (high0 == Bars.Last(1).High && high0 == Bars.Last(2).High) {
                // ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, "Stop Buy", StopLoss, OrderPrice);
                Print("Tirple-High Signal: {0}", bar0);
                Bar? prev = ResistanceLevel(high0, true);
                if (prev != null) {
                    Print("Win! {0}...{1}-{2}", prev?.OpenTime, Bars.Last(2).OpenTime, bar0.OpenTime);     
                }
            }
            if (low0 == Bars.Last(1).Low && low0 == Bars.Last(2).Low) {
                // ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, "Stop Buy", StopLoss, OrderPrice);
                Print("Tirple-Low Signal: {0}", bar0);
                Bar? prev = ResistanceLevel(low0, false);
                if (prev != null) {
                    Print("Win! {0}...{1}-{2}", prev?.OpenTime, Bars.Last(2).OpenTime, bar0.OpenTime);     
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

    }
    
    
}
