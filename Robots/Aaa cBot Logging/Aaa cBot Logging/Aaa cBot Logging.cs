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
    public class AaacBotLogging : Robot
    {
    
        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate
            Print("The bot is started...");

        }
        
        protected override void OnStop() 
        {
            Print("The bot is stopped.");
        }

        protected override void OnBar()
        {
            var closedBar = Bars.Last(1);
            // Print("Bars count: " + Bars.Count + "; ClosedBar: " + closedBar);
            HandleShortBar(closedBar);
            var bars5m = MarketData.GetBars(TimeFrame.Minute5);
            if (bars5m.Last().OpenTime == Bars.Last().OpenTime) {
                HandleLongBar(bars5m.Last(1));
            }
        }
        
        void HandleShortBar(Bar bar)
        {
             Print("Bars count: " + Bars.Count + "; Short ClosedBar: " + bar);
        }
        
        void HandleLongBar(Bar bar)
        {
            Print("Long ClosedBar: " + bar);
        }
        
    }
}