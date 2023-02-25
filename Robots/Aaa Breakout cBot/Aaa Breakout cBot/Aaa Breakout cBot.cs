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

            if (high0 == Bars.Last(1).High && high0 == Bars.Last(2).High) {
                // ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, "Stop Buy", StopLoss, OrderPrice);
                Print("Tirple-Close Signal: {}", bar0);
                Bar prev;
                if (prev = IsResistanceLevel(high0) == null) {
                    Print("Win! {0}...{1}-{2}", prev.OpenTime, bar1.OpenTime, bar3.OpenTime);                        
                    break;
                }
            }
        }

        private Bar IsResistanceLevel(double level) {
            for (int i = 0; i < BreakoutPeriod; i++) {
                Bar bar = bars.Last(3 + i);
                if (bar.High == level) {
                    return bars;
                }
            }
            return null;
        }

    }
    
}
