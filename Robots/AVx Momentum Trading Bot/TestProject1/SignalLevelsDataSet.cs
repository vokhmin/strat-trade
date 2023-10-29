// SignalLevelDataSet.cs
using System;
using System.Linq;
using cAlgo.API;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

public class SignalLevelsDataSet
{
    private double HiPercentile = 70;
    private double LoPercentile = 30;
    List<Bar> bars = new List<Bar>();
    public Double[] volatility;

    public SignalLevelsDataSet InitByLastBars(Bars bars, int Count)
    {
        this.bars = new List<Bar>(Count);
        volatility = new double[Count];
        for (int i = 0; i < Count; i++)
        {
            this.bars.Add(bars.Last(i));
            var index = Count - i + 1;
            volatility[i] = (bars.HighPrices.Last(index) - bars.LowPrices.Last(index)) / 2;
        }
        return this;
    }

    public Bar[] getHighLevels()
    {
        return new Bar[0];
    }

    public Bar[] getLowLevels()
    {
        return new Bar[0];
    }

    public OrderTrigger? detectSignal(Bar bar, double delta)
    {
        return null;
    }
}    
