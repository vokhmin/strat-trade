// PercentileCalculatorTests.cs
using cAlgo.API;
using NUnit.Framework;
using System;
using System.Collections;

[TestFixture]
public class SignalLevelsDataSetTests {

    [Test]
    public void DetectSignal_SimpleCase() {
        cAlgo.API.Bars bars = new TestBars();
        int N = 30;
        SignalLevelsDataSet slds = new SignalLevelsDataSet().InitByLastBars(bars, N);
    }

}

internal class TestBars : Bars
{
    List<Bar> bars = new List<Bar>();
    private TimeFrame timeFrame;
    private string symbolName;

    public TestBars init(TimeFrame timeFrame, List<Bar> bars) {
        this.timeFrame = timeFrame;
        this.bars = bars;
        return this;
    }

    // Bars interface implementation

    public Bar this[int index] => this.bars[index];

    public Bar LastBar => this.bars.Last();

    public int Count => this.Count;

    public TimeFrame TimeFrame => this.timeFrame;

    public string SymbolName => this.symbolName;

    public DataSeries OpenPrices => throw new NotImplementedException();

    public DataSeries HighPrices => throw new NotImplementedException();

    public DataSeries LowPrices => throw new NotImplementedException();

    public DataSeries ClosePrices => throw new NotImplementedException();

    public DataSeries TickVolumes => throw new NotImplementedException();

    public DataSeries MedianPrices => throw new NotImplementedException();

    public DataSeries TypicalPrices => throw new NotImplementedException();

    public DataSeries WeightedPrices => throw new NotImplementedException();

    public TimeSeries OpenTimes => throw new NotImplementedException();

    public event Action<BarsHistoryLoadedEventArgs> HistoryLoaded;
    public event Action<BarsHistoryLoadedEventArgs> Reloaded;
    public event Action<BarsTickEventArgs> Tick;
    public event Action<BarOpenedEventArgs> BarOpened;
    public event Action<BarClosedEventArgs> BarClosed;

    public IEnumerator<Bar> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public Bar Last(int index)
    {
        throw new NotImplementedException();
    }

    public int LoadMoreHistory()
    {
        throw new NotImplementedException();
    }

    public void LoadMoreHistoryAsync()
    {
        throw new NotImplementedException();
    }

    public void LoadMoreHistoryAsync(Action<BarsHistoryLoadedEventArgs> callback)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}