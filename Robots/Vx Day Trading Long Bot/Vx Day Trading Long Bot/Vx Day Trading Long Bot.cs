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
    [Robot(AccessRights = AccessRights.None)]
    public class KitBotLong : Robot
    {
        [Parameter(DefaultValue = 360)]
        public int DaysOfAnalysis { get; set; }
        
        [Parameter(DefaultValue = 0.15)]
        public double ATRRate { get; set; }
        
        [Parameter(DefaultValue = 50)]
        public int MinVolatilityGroupLevel { get; set; }
        
        [Parameter(DefaultValue = 50)]
        public int MaxVolatilityGroupLevel { get; set; }
        
        [Parameter(DefaultValue = 0.05)]
        public double RiskLimitRelativeToBalance { get; set; }
        
        [Parameter(DefaultValue = 0.005)]
        public double StopLossAndTakeProfitATRFactor { get; set; }
        
        [Parameter(DefaultValue = 40)]
        public double TakeProfitFactor { get; set; }
        
        [Parameter(DefaultValue = "00:30:00")]
        public string MaxTimeFromOpen { get; set; }
        
        [Parameter(DefaultValue = 1)]
        public double StopLossCorrectionFactor { get; set; }
        
        [Parameter(DefaultValue = 3)]
        public double StopLossCorrectorThreshold { get; set; }
        
        private TimeSpan _maxTimeFromOpen;
        
        private Bars _bars15m;
        private Bars _bars1m;
        
        private AverageTrueRange _averageTrueRange;
        private ExponentialMovingAverage _exponentialMovingAverage6;
        private ExponentialMovingAverage _exponentialMovingAverage12;
        private StopLossCorrector _stopLossCorrector;
        
        private readonly List<BarsSignalObserver> _intradayObservers = new();

        protected override void OnStart()
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
            
            _maxTimeFromOpen = !string.IsNullOrWhiteSpace(MaxTimeFromOpen) ? TimeSpan.Parse(MaxTimeFromOpen) : TimeSpan.Zero;
            
            _bars15m = MarketData.GetBars(TimeFrame.Minute15);
            _bars1m = MarketData.GetBars(TimeFrame.Minute);

            _averageTrueRange = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            _exponentialMovingAverage6 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, 6);
            _exponentialMovingAverage12 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, 12);
            
            _stopLossCorrector = new StopLossCorrector(StopLossCorrectorThreshold);
            _stopLossCorrector.PositionFound += (sender, args) =>
            {
                ModifyPosition(args, args.EntryPrice * StopLossCorrectionFactor, args.TakeProfit);
            };
            _stopLossCorrector.BindTo(_bars1m, Positions);
        }
        
        protected override void OnBar()
        {
            ClearDayliData();
        
            var atr = _averageTrueRange.Result.Last(5);
            
            var dayliSignalCondition = new DayliSignalCondition(DaysOfAnalysis, atr * ATRRate, MinVolatilityGroupLevel, MaxVolatilityGroupLevel);
            var dayliSignalResult = dayliSignalCondition.Check(Bars) as DayliSignalResult;
            
            if (dayliSignalResult.Signals.Any())
            {
                if (1 == 1 || _exponentialMovingAverage6.Result.Last() > _exponentialMovingAverage12.Result.Last())
                {
                    foreach (var signal in dayliSignalResult.Signals.Where(x => x.Type == SignalType.LongBreakdown))
                    {
                        Print($"Dayli signal: {signal.Level} {signal.Type}, foundAt: {signal.FoundAt:d} [{dayliSignalResult.CurrentBar.VolatilityQuantile}] from {signal.Bar.OpenTime:d}, balance {Account.Balance}");
                        var min15Observer = new BarsSignalObserver(new Minute15SignalCondition(signal.Level));
                        min15Observer.Success += (sender, args) =>
                        {
                            var signal = args.Signals.First();
                            Print($"15 minute signal: {signal.Level}, found at {signal.FoundAt} from {signal.Bar.OpenTime}, balance {Account.Balance}");
                            PlaceOrder(TradeParametersFactory.Create(Symbol, Account.Balance, atr, RiskLimitRelativeToBalance, TakeProfitFactor, signal.Level, StopLossAndTakeProfitATRFactor), Bars.LastBar.OpenTime.AddDays(1));
                            min15Observer.Unbind();
                            _intradayObservers.Remove(min15Observer);
                        };
                        min15Observer.BindTo(_bars15m);
                        _intradayObservers.Add(min15Observer);
                    }
                }
                else
                {
                    Print("Dayli signal found but ignored due to EMA");
                }
            }
            else
            {
                Print($"{dayliSignalResult.CurrentBar.Bar.OpenTime:d} [{dayliSignalResult.CurrentBar.VolatilityQuantile}], balance {Account.Balance}");
            }
        }

        protected override void OnStop()
        {
            ClearDayliData();

            if (_stopLossCorrector != null)
            {
                _stopLossCorrector.Unbind();
            }
        }
        
        private void ClearDayliData()
        {
            foreach (var o in PendingOrders)
            {
                CancelPendingOrder(o);
            }
            
            foreach (var o in _intradayObservers)
            {
                o.Unbind();
            }
            _intradayObservers.Clear();
        }
        
        private TradeOperation PlaceOrder(TradeParameters parameters, DateTime? expirationDate = null)
        {
            return PlaceLimitOrderAsync(TradeType.Buy, SymbolName, parameters.VolumeInUnits, parameters.TargetPrice, string.Empty, parameters.StopLossPips, parameters.TakeProfitPips, expirationDate);
        }
    }
    
    class TradeParameters
    {
        public double TargetPrice { get; set; }
        public double StopLoss { get; set; }
        public double StopLossPips { get; set; }
        public double TakeProfit { get; set; }
        public double TakeProfitPips { get; set; }
        public double VolumeInUnits { get; set; }
    }
    
    class TradeParametersFactory
    {
        public static TradeParameters Create(Symbol symbol, double balance, double atr, double riskLimitRelativeToBalance, double takeProfitFactor, double targetPrice, double stopLossAndTakeProfitATRFactor)
        {
            var stopLoss = targetPrice - atr * stopLossAndTakeProfitATRFactor;
            var takeProfit = targetPrice + atr * stopLossAndTakeProfitATRFactor * takeProfitFactor;

            //var stopLossPips = Math.Abs(targetPrice - stopLoss) / symbol.PipSize;
            //var takeProfitPips = Math.Abs(takeProfit - targetPrice) / symbol.PipSize;
            var stopLossPips = Math.Abs(targetPrice - stopLoss) * symbol.PipSize;
            var takeProfitPips = Math.Abs(takeProfit - targetPrice) * symbol.PipSize;

            var riskLimit = balance * riskLimitRelativeToBalance;

            var volumeInUnits = symbol.NormalizeVolumeInUnits(riskLimit / (targetPrice - stopLoss));
            
            return new TradeParameters
            {
                TargetPrice = targetPrice,
                StopLoss = stopLoss,
                StopLossPips = stopLossPips,
                TakeProfit = takeProfit,
                TakeProfitPips = takeProfitPips,
                VolumeInUnits = volumeInUnits
            };
        }
    }
    
    class StopLossCorrector
    {
        public StopLossCorrector(double stopLossCorrectionThreshold)
        {
            _stopLossCorrectionThreshold = stopLossCorrectionThreshold;
        }

        public event EventHandler<Position> PositionFound;

        private readonly double _stopLossCorrectionThreshold;
        private readonly List<BarsSignalObserver> _observers = new();
        private Bars _bars;
        private Positions _positions;
        
    
        public void BindTo(Bars bars, Positions positions)
        {
            _bars = bars;
            _positions = positions;
            _positions.Opened += OnPositionsOpened;
            _positions.Closed += OnPositionsClosed;
        }
        
        public void Unbind()
        {
            if (PositionFound != null)
            {
                foreach (var invocation in PositionFound.GetInvocationList())
                {
                    PositionFound -= invocation as EventHandler<Position>;
                }
            }
            
            if (_positions != null)
            {
                UnbindPositions();
            }

            foreach (var observer in _observers.ToList())
            {
                UnbindObserver(observer);
            }
        }
        
        public void OnPositionsOpened(PositionOpenedEventArgs args)
        {
            var observer = new BarsSignalObserver(new StopLossSignalCondition(args.Position, _stopLossCorrectionThreshold));
            observer.Success += (sender, args) =>
            {
                UnbindObserver(observer);
                PositionFound?.Invoke(this, (args as StopLossSignalResult).Position);
            };
            observer.BindTo(_bars);
            _observers.Add(observer);
        }
        
        public void OnPositionsClosed(PositionClosedEventArgs args)
        {
            var observer = _observers.FirstOrDefault(x => (x.Condition as StopLossSignalCondition).Position == args.Position);
            if (observer != null)
            {
                UnbindObserver(observer);
            }
        }
        
        private void UnbindPositions()
        {
            if (_positions != null)
            {
                _positions.Opened -= OnPositionsOpened;
                _positions.Closed -= OnPositionsClosed;
            }
        }
        
        private void UnbindObserver(BarsSignalObserver observer)
        {
            observer.Unbind();
            _observers.Remove(observer);
        }        
    }
    
    class StopLossSignalResult : ISignalResult
    {
        public Position Position { get; set; }
        public ICollection<Signal> Signals { get; }
        public bool Valid { get; } = true;
    }
    
    class StopLossSignalCondition : ISignalCondition
    {
        public StopLossSignalCondition(Position position, double stopLossCorrectionThreshold)
        {
            Position = position;
            _threshold = (Position.EntryPrice - Position.StopLoss) * stopLossCorrectionThreshold;
        }
        
        public Position Position { get; }
        private readonly double? _threshold;

        public ISignalResult Check(Bars bars)
        {
            if (_threshold.HasValue && bars.LastBar.Open - Position.EntryPrice > _threshold)
            {
                return new StopLossSignalResult
                {
                    Position = Position
                };
            }
            return null;
        }
    }
    
    class Minute15SignalResult : BaseSignalResult
    {
    }
    
    class Minute15SignalCondition : ISignalCondition
    {
        public Minute15SignalCondition(double level)
        {
            _level = level;
        }
        
        private readonly double _level;

        public ISignalResult Check(Bars bars)
        {
            var signalBar = bars.Last(1);
            if (signalBar.Close > _level)
            {
                return new Minute15SignalResult
                {
                    Signals = new List<Signal>{new Signal {
                        Type = SignalType.LongBreakdown,
                        FoundAt = bars.Last().OpenTime,
                        Level = _level
                    }}
                };
            }
            return null;
        }
        /*
                private void Check(BarOpenedEventArgs args)
        {
            var lastBar = args.Bars.Last();
            var signalBar = args.Bars.Last(1);
            if (signalBar.Close > Level)
            {
                LevelFound?.Invoke(this, new LongCheckerLevelFoundArgs
                {
                    FoundAt = lastBar.OpenTime,
                    Level = Level
                });
            }
        }
        */
    }
    
    
    class DayliSignalResult : BaseSignalResult
    {
        public DayliBarDescriptor CurrentBar { get; set; }
    }
    
    class DayliSignalCondition : ISignalCondition
    {
        public DayliSignalCondition(int daysOfAnalysis, double atrRate, int minVolatilityGroupLevel, int maxVolatilityGroupLevel)
        {
            _daysOfAnalysis = daysOfAnalysis;
            _atrRate = atrRate;
            _minVolatilityGroupLevel = minVolatilityGroupLevel;
            _maxVolatilityGroupLevel = maxVolatilityGroupLevel;
        }
        
        private readonly int _daysOfAnalysis;
        private readonly double _atrRate;
        private readonly int _minVolatilityGroupLevel;
        private readonly int _maxVolatilityGroupLevel;
        
        public ISignalResult Check(Bars bars)
        {
            var data = bars.TakeLast(_daysOfAnalysis + 1).Take(_daysOfAnalysis).Select(x => new DayliBarDescriptor(_minVolatilityGroupLevel, _maxVolatilityGroupLevel) {
                Bar = x,
                Volatility = x.High - x.Low,
                VolatilityQuantile = 0
            }).ToArray();

            var quantiles = Statistics.Quantiles(data.Select(x => x.Volatility).ToArray(), 100);
            
            var currentBar = data.Last();
            
            currentBar.VolatilityQuantile = quantiles.Where(x => currentBar.Volatility > x).Count();
            
            var res = new DayliSignalResult
            {
                CurrentBar = currentBar
            };

            if (currentBar.VolatilityGroup == VolatilityGroup.Low)
            {
                for (var i = data.Length - 1; i >= 0; i--)
                {
                    var previousData = data[i];
                    previousData.VolatilityQuantile = quantiles.Where(x => previousData.Volatility > x).Count();
                    if (previousData.VolatilityGroup == VolatilityGroup.High)
                    {
                        if (Math.Abs(previousData.Bar.High - currentBar.Bar.Close) < _atrRate && currentBar.Bar.Close < previousData.Bar.High)
                        {
                            res.Signals.Add(new Signal
                            {
                                Type = currentBar.Bar.Open < currentBar.Bar.Close ? SignalType.LongBreakdown : SignalType.LongFalse,
                                FoundAt = currentBar.Bar.OpenTime,
                                Bar = previousData.Bar,
                                Level = previousData.Bar.High
                            });
                        }
                        
                        /*if (Math.Abs(previousData.Bar.Low - currentBar.Bar.Close) < _atrRate && currentBar.Bar.Close < previousData.Bar.Low)
                        {
                            res.Signals.Add(new Signal
                            {
                                Type = currentBar.Bar.Open < currentBar.Bar.Close ? SignalType.LongBreakdown : SignalType.LongFalse,
                                FoundAt = currentBar.Bar.OpenTime,
                                Bar = previousData.Bar,
                                Level = previousData.Bar.Low
                            });
                        }*/
                    }
                }
            }

            return res;
        }
    }
    
    class BarsSignalObserver
    {
        public BarsSignalObserver(ISignalCondition condition)
        {
            Condition = condition;
        }
        
        public event EventHandler<ISignalResult> Success;
        
        public ISignalCondition Condition { get; }

        private Bars _boundTo;
        
        public void BindTo(Bars bars)
        {
            _boundTo = bars;
            _boundTo.BarOpened += Check;
        }
        
        public void Unbind()
        {
            if (_boundTo != null)
            {
                _boundTo.BarOpened -= Check;
                _boundTo = null;
            }
        }
        
        private void Check(BarOpenedEventArgs args)
        {
            ISignalResult result;
            if ((result = Condition.Check(args.Bars)) != null && result.Valid)
            {
                Success?.Invoke(this, result);
            }
        }
    }
    
    abstract class BaseSignalResult : ISignalResult
    {
        public BaseSignalResult()
        {
            Signals = new List<Signal>();
        }

        public ICollection<Signal> Signals { get; set; }
        public bool Valid => Signals.Count > 0;
    }
    
    interface ISignalResult
    {
        public ICollection<Signal> Signals { get; }
        public bool Valid { get; }
    }
    
    interface ISignalCondition
    {
        public ISignalResult Check(Bars bars);
    }
    
    class Signal
    {
        public SignalType Type { get; set; }
        public DateTime FoundAt { get; set; }
        public Bar Bar { get; set; }
        public double Level { get; set; }
    }
    
    class DayliBarDescriptor
    {
        public DayliBarDescriptor(int minVolatilityGroupLevel, int maxVolatilityGroupLevel)
        {
            _minVolatilityGroupLevel = minVolatilityGroupLevel;
            _maxVolatilityGroupLevel = maxVolatilityGroupLevel;
        }
        
        public Bar Bar { get; set; }
        public double Volatility { get; set; }
        public double VolatilityQuantile { get; set; }
        
        private readonly int _minVolatilityGroupLevel;
        private readonly int _maxVolatilityGroupLevel;
        
        public VolatilityGroup VolatilityGroup
        {
            get
            {
                return VolatilityQuantile <= _minVolatilityGroupLevel
                    ? VolatilityGroup.Low
                    : VolatilityQuantile >= _maxVolatilityGroupLevel
                        ? VolatilityGroup.High
                        : VolatilityGroup.Middle;
            }
        }
    }
    
    enum SignalType
    {
        LongBreakdown,
        LongFalse
    }
    
    enum VolatilityGroup
    {
        Low,
        Middle,
        High
    }
    
    static class Statistics
    {
        public static IEnumerable<double> Quantiles(double[] data, int n)
        {
            var ld = data.Length;
            var m = ld + 1;
            var result = new double[n - 1];
            for (var i = 1; i < n; i++)
            {
                var j = i * m / n;
                j = j < 1 ? 1 : j > ld - 1 ? ld - 1 : j;
                var delta = i * m - j * n;
                var interpolated = (data[j - 1] * (n - delta) + data[j] * delta) / n;
                result[i - 1] = interpolated;
            }
            return result;
        }
    }

    static class EnumerableEx
    {
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}