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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class AaacBot : Robot
    {
        [Parameter(DefaultValue = "Hello world!")]
        public string Message { get; set; }
        [Parameter(DefaultValue = 60 * 24)]
        public int BreakoutPeriod { get; set; }
        
        private Bars bars;

        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate

            Print(Message);
            bars = MarketData.GetBars(this.TimeFrame);
            for(int i = 0; i < 12; i++) {
                Print("{0} - {1}", i, bars.Last(i));
            }
        }

        protected override void OnBar()
        {
            //Print("OnBar: {0}", Bars.LastBar);
            //Print("Last bar3: {0}", bars.Last(0)); 
            //Print("Last bar2: {0}", bars.Last(1));
            //Print("Last bar1: {0}", bars.Last(2)); 

            var level = bars.Last(0).High;
            var bar3 = bars.Last(0);
            var bar2 = bars.Last(1);
            var bar1 = bars.Last(2);
            if (level == bar2.High && level == bar1.High) {
            //if (level == bar2.High) {
                Print("Triple High: {0}, {1}..{2}", level, bar3, bar1);
                Print("Last bar3: {0}", bars.Last(0)); 
                Print("Last bar2: {0}", bars.Last(1));
                Print("Last bar1: {0}", bars.Last(2)); 
                for (int i = 0; i < BreakoutPeriod; i++) {
                    if (bars.Last(3 + i).High == level) {
                        OpenBreakoutUp(bar3);
                        Print("Win! {0}...{1}-{2}", bars.Last(3 + i).OpenTime, bar1.OpenTime, bar3.OpenTime);                        
                        break;
                    }
                }
            }
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        protected void OpenBreakoutUp(Bar signalBar) {
            double volume = 1;
            double targetPrice = signalBar.High + 0.01;
            double stopLimitRangePips = 100;
            var label = "" + signalBar.OpenTime;
            int? stopLossPips = null;
            int? takeProfitPips = null;
            DateTime? expiration = signalBar.OpenTime.AddDays(1);
            Print("Breakout Up! Signal Bar: {0}. Open Stop-Buy order with price: ", signalBar, signalBar.High + 0.01);    
            PlaceStopLimitOrder(TradeType.Buy, this.Symbol.Name, volume, targetPrice, stopLimitRangePips, label, stopLossPips, takeProfitPips,expiration);           
        }
    }
    
}