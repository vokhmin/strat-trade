using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.IO;
using System.Runtime.Serialization.Json;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class KitBotLong : Robot
    {
        [Parameter(DefaultValue = 360)]
        public int LongDaysOfAnalysis { get; set; }
        
        [Parameter(DefaultValue = 31)]
        public int ShortDaysOfAnalysis { get; set; }
        
        [Parameter(DefaultValue = 0.3)]
        public double ATRRate { get; set; }
        
        [Parameter(DefaultValue = 50)]//35
        public int MinVolatilityGroupLevel { get; set; }
        
        [Parameter(DefaultValue = 50)]//65
        public int MaxVolatilityGroupLevel { get; set; }
        
        [Parameter(DefaultValue = 0.3)]//.05
        public double RiskLimitRelativeToBalance { get; set; }
        
        [Parameter(DefaultValue = 0.005)]
        public double StopLossAndTakeProfitATRFactor { get; set; }
        
        [Parameter(DefaultValue = 0.01)]
        public double StopLossAndTakeProfitPipsFactor { get; set; }
        
        [Parameter(DefaultValue = 40)]
        public double TakeProfitFactor { get; set; }
        
        [Parameter(DefaultValue = 1)]
        public double StopLossCorrectionFactor { get; set; }
        
        [Parameter(DefaultValue = 3)]
        public double StopLossCorrectorThreshold { get; set; }
        
        private Bars _bars15m;
        private Bars _bars1m;
        
        private AverageTrueRange _averageTrueRange;
        private PositionManager _positionManager;
        
        private readonly List<BarsSignalObserver> _intradayObservers = new();

        protected override void OnStart()
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
            
            _bars15m = MarketData.GetBars(TimeFrame.Minute15);
            _bars1m = MarketData.GetBars(TimeFrame.Minute);

            _averageTrueRange = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            
            _positionManager = new PositionManager(StopLossCorrectorThreshold, Print);
            _positionManager.StopLossCorrection += (sender, args) =>
            {
                ModifyPosition(args, args.EntryPrice * StopLossCorrectionFactor, args.TakeProfit);
            };
            _positionManager.PartialPositionClose += (sender, args) =>
            {
                ClosePosition(args.Position, args.Volume);
            };
            _positionManager.BindTo(_bars1m, Positions);
        }
        
        protected override void OnBar()
        {
            ClearDayliData();
        
            var atr = _averageTrueRange.Result.Last(5);
            
            var longDayliSignalCondition = new DayliSignalCondition(LongDaysOfAnalysis, atr * ATRRate, MinVolatilityGroupLevel, MaxVolatilityGroupLevel);
            var longDayliSignalResult = longDayliSignalCondition.Check(Bars) as DayliSignalResult;
            
            var shortDayliSignalCondition = new DayliSignalCondition(ShortDaysOfAnalysis, atr * ATRRate, MinVolatilityGroupLevel, MaxVolatilityGroupLevel);
            var shortDayliSignalResult = shortDayliSignalCondition.Check(Bars) as DayliSignalResult;
            
            var signals = longDayliSignalResult.Signals.Union(shortDayliSignalResult.Signals).ToList();
            
            if (signals.Any())
            {
                foreach (var signal in signals.Where(x => x.Type == SignalType.LongBreakdown || x.Type == SignalType.ShortBreakdown))
                {
                    //Print($"Dayli signal: {signal.Level} {signal.Type}, foundAt: {signal.FoundAt} [{dayliSignalResult.CurrentBar.VolatilityQuantile}] from {signal.Bar.OpenTime:d}, balance {Account.Balance}");
                    if (signal.Type == SignalType.LongBreakdown)
                    {
                        var min15Observer = new BarsSignalObserver(new LongMinute15SignalCondition(signal.Level));
                        min15Observer.Success += (sender, args) =>
                        {
                            var signal = args.Signals.First();
                            Print($"15 minute signal: {signal.Level}, found at {signal.FoundAt} from {signal.Bar.OpenTime}, balance {Account.Balance}");
                            var tradeParameters = TradeParametersFactory.CreateLong(
                                Symbol,
                                Account.Balance,
                                atr,
                                RiskLimitRelativeToBalance,
                                StopLossAndTakeProfitATRFactor,
                                TakeProfitFactor,
                                StopLossAndTakeProfitPipsFactor,
                                signal.Level
                            );
    
                            PlaceOrder(TradeType.Buy, tradeParameters, Bars.LastBar.OpenTime.AddDays(1));
    
                            min15Observer.Unbind();
                            _intradayObservers.Remove(min15Observer);
                        };
                        min15Observer.BindTo(_bars15m);
                        _intradayObservers.Add(min15Observer);
                    }
                    else
                    {
                        var min15Observer = new BarsSignalObserver(new ShortMinute15SignalCondition(signal.Level));
                        min15Observer.Success += (sender, args) =>
                        {
                            var signal = args.Signals.First();
                            Print($"15 minute signal: {signal.Level}, found at {signal.FoundAt} from {signal.Bar.OpenTime}, balance {Account.Balance}");
                            var tradeParameters = TradeParametersFactory.CreateShort(
                                Symbol,
                                Account.Balance,
                                atr,
                                RiskLimitRelativeToBalance,
                                StopLossAndTakeProfitATRFactor,
                                TakeProfitFactor,
                                StopLossAndTakeProfitPipsFactor,
                                signal.Level
                            );
    
                            PlaceOrder(TradeType.Sell, tradeParameters, Bars.LastBar.OpenTime.AddDays(1));
    
                            min15Observer.Unbind();
                            _intradayObservers.Remove(min15Observer);
                        };
                        min15Observer.BindTo(_bars15m);
                        _intradayObservers.Add(min15Observer);
                    }
                }
            }
            else
            {
                //Print($"{dayliSignalResult.CurrentBar.Bar.OpenTime:d} [{dayliSignalResult.CurrentBar.VolatilityQuantile}], balance {Account.Balance}");
            }
        }

        protected override void OnStop()
        {
            ClearDayliData();

            if (_positionManager != null)
            {
                _positionManager.Unbind();
            }
        }
        
        private void ClearDayliData()
        {
            foreach (var o in PendingOrders)
            {
                CancelPendingOrder(o);
            }
            
            foreach (var p in Positions)
            {
                ClosePosition(p);
            }
            
            foreach (var o in _intradayObservers)
            {
                o.Unbind();
            }
            _intradayObservers.Clear();
        }
        
        private TradeOperation PlaceOrder(TradeType tradeType, TradeParameters parameters, DateTime? expirationDate = null)
        {
            var comment = parameters.StopLoss.ToString();
            return PlaceLimitOrderAsync(tradeType, SymbolName, parameters.VolumeInUnits, parameters.TargetPrice, string.Empty, parameters.StopLossPips, parameters.TakeProfitPips, expirationDate, comment);
        }
    }
    
    class PartialPositionCloseSignal : ISignalResult
    {
        public double Volume { get; set; }
        public ICollection<Signal> Signals { get; }
        public bool Valid { get; } = true;
    }
    
    class PartialLongPositionCloseCondition : ISignalCondition
    {
        public PartialLongPositionCloseCondition(Position position, Action<string> printAction = null)
        {
            Position = position;
            _takeProfitDelta = Position.TakeProfit - Position.EntryPrice;
            _initialVolume = Position.VolumeInUnits;
            _partialTakeProfitvolume = Position.VolumeInUnits / 5;
            printAction($"Deserialize from {position.Comment}");
            _stopLoss = double.Parse(position.Comment);
            _printAction = printAction;
        }
        
        public Position Position { get; private set; }
        
        private readonly double? _takeProfitDelta;
        
        private readonly double _initialVolume;
        private readonly double _partialTakeProfitvolume;
        private readonly double _stopLoss;
        
        private readonly Action<string> _printAction;

        public ISignalResult Check(Bars bars)
        {
            _printAction?.Invoke($"CurrentPrice: {Position.CurrentPrice}, StopLoss: {Position.StopLoss}, TakeProfit: {Position.TakeProfit}, Ratio: {(Position.CurrentPrice - Position.EntryPrice) / _takeProfitDelta}");
            
            var ratio = (Position.CurrentPrice - Position.EntryPrice) / _takeProfitDelta;
            if (ratio > 1)
            {
                return null;
            }
            
            // Stop Loss logic
            if (Position.CurrentPrice < _stopLoss)
            {
                _printAction($"Alarm! Close position at level {_stopLoss}");
                return new PartialPositionCloseSignal { Volume = Position.VolumeInUnits };
            }
            
            if (ratio > 0.2 && Position.VolumeInUnits / _initialVolume > 0.8 ||
                ratio > 0.4 && Position.VolumeInUnits / _initialVolume > 0.6 ||
                ratio > 0.6 && Position.VolumeInUnits / _initialVolume > 0.4 ||
                ratio > 0.8)
            {
                return new PartialPositionCloseSignal { Volume = Position.VolumeInUnits - _partialTakeProfitvolume };
            }

            return null;
        }
    }
    
    
    class PartialShortPositionCloseCondition : ISignalCondition
    {
        public PartialShortPositionCloseCondition(Position position, Action<string> printAction = null)
        {
            Position = position;
            _takeProfitDelta = Position.EntryPrice - Position.TakeProfit;
            _initialVolume = Position.VolumeInUnits;
            _partialTakeProfitvolume = Position.VolumeInUnits / 5;
            printAction($"Deserialize from {position.Comment}");
            _stopLoss = double.Parse(position.Comment);
            _printAction = printAction;
        }
        
        public Position Position { get; private set; }
        
        private readonly double? _takeProfitDelta;
        
        private readonly double _initialVolume;
        private readonly double _partialTakeProfitvolume;
        private readonly double _stopLoss;
        
        private readonly Action<string> _printAction;

        public ISignalResult Check(Bars bars)
        {
            _printAction?.Invoke($"CurrentPrice: {Position.CurrentPrice}, StopLoss: {Position.StopLoss}, TakeProfit: {Position.TakeProfit}, Ratio: {(Position.EntryPrice - Position.CurrentPrice) / _takeProfitDelta}");
            
            var ratio = (Position.EntryPrice - Position.CurrentPrice) / _takeProfitDelta;
            if (ratio > 1)
            {
                return null;
            }
            
            // Stop Loss logic
            if (Position.CurrentPrice > _stopLoss)
            {
                _printAction($"Alarm! Close position at level {_stopLoss}");
                return new PartialPositionCloseSignal { Volume = Position.VolumeInUnits };
            }
            
            if (ratio > 0.2 && Position.VolumeInUnits / _initialVolume > 0.8 ||
                ratio > 0.4 && Position.VolumeInUnits / _initialVolume > 0.6 ||
                ratio > 0.6 && Position.VolumeInUnits / _initialVolume > 0.4 ||
                ratio > 0.8)
            {
                return new PartialPositionCloseSignal { Volume = Position.VolumeInUnits - _partialTakeProfitvolume };
            }

            return null;
        }
    }
    
    [Serializable]
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
    
        public static TradeParameters CreateLong(
            Symbol symbol,
            double balance,
            double atr,
            double riskLimitRelativeToBalance,
            double stopLossAndTakeProfitATRFactor,
            double takeProfitFactor,
            double stopLossAndTakeProfitPipsFactor,
            double targetPrice)
        {
            var stopLoss = targetPrice - atr * stopLossAndTakeProfitATRFactor;
            var takeProfit = targetPrice + atr * stopLossAndTakeProfitATRFactor * takeProfitFactor;

            var stopLossPips = (Math.Abs(targetPrice - stopLoss) / symbol.PipSize) * stopLossAndTakeProfitPipsFactor;
            var takeProfitPips = (Math.Abs(takeProfit - targetPrice) / symbol.PipSize) * stopLossAndTakeProfitPipsFactor;

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

        public static TradeParameters CreateShort(
            Symbol symbol,
            double balance,
            double atr,
            double riskLimitRelativeToBalance,
            double stopLossAndTakeProfitATRFactor,
            double takeProfitFactor,
            double stopLossAndTakeProfitPipsFactor,
            double targetPrice)
        {
            var stopLoss = targetPrice + atr * stopLossAndTakeProfitATRFactor;
            var takeProfit = targetPrice - atr * stopLossAndTakeProfitATRFactor * takeProfitFactor;

            var stopLossPips = (Math.Abs(targetPrice - stopLoss) / symbol.PipSize) * stopLossAndTakeProfitPipsFactor;
            var takeProfitPips = (Math.Abs(takeProfit - targetPrice) / symbol.PipSize) * stopLossAndTakeProfitPipsFactor;

            var riskLimit = balance * riskLimitRelativeToBalance;

            var volumeInUnits = symbol.NormalizeVolumeInUnits(riskLimit / (stopLoss - targetPrice));
            
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
    
    class PartialPositionCloseEventArgs
    {
        public Position Position { get; set; }
        public double Volume { get; set; }
    }
    
    class PositionManager
    {
        public PositionManager(double stopLossCorrectionThreshold, Action<string> printAction)
        {
            _stopLossCorrectionThreshold = stopLossCorrectionThreshold;
            _printAction = printAction;
        }

        public event EventHandler<Position> StopLossCorrection;
        public event EventHandler<PartialPositionCloseEventArgs> PartialPositionClose;

        private readonly double _stopLossCorrectionThreshold;
        private readonly List<Tuple<Position, BarsSignalObserver>> _observers = new();
        private Bars _bars;
        private Positions _positions;
        
        private readonly Action<string> _printAction;
    
        public void BindTo(Bars bars, Positions positions)
        {
            _bars = bars;
            _positions = positions;
            _positions.Opened += OnPositionsOpened;
            _positions.Closed += OnPositionsClosed;
        }
        
        public void Unbind()
        {
            if (StopLossCorrection != null)
            {
                foreach (var invocation in StopLossCorrection.GetInvocationList())
                {
                    StopLossCorrection -= invocation as EventHandler<Position>;
                }
            }
            
            if (PartialPositionClose != null)
            {
                foreach (var invocation in PartialPositionClose.GetInvocationList())
                {
                    PartialPositionClose -= invocation as EventHandler<PartialPositionCloseEventArgs>;
                }
            }
            
            if (_positions != null)
            {
                UnbindPositions();
            }

            foreach (var observer in _observers.ToList())
            {
                UnbindObserver(observer.Item2);
            }
        }
        
        public void OnPositionsOpened(PositionOpenedEventArgs args)
        {
            var longPosition = args.Position.TakeProfit > args.Position.EntryPrice;
            _printAction($"Position opened: {args.Position}");
            var slObserver = new BarsSignalObserver(longPosition
                ? new LongStopLossSignalCondition(args.Position, _stopLossCorrectionThreshold)
                : new ShortStopLossSignalCondition(args.Position, _stopLossCorrectionThreshold));
            slObserver.Success += (sender, args) =>
            {
                UnbindObserver(slObserver);
                StopLossCorrection?.Invoke(this, (args as StopLossSignalResult).Position);
            };
            slObserver.BindTo(_bars);
            _observers.Add(new (args.Position, slObserver));
            
            var tpObserver = new BarsSignalObserver(longPosition
                ? new PartialLongPositionCloseCondition(args.Position, _printAction)
                : new PartialShortPositionCloseCondition(args.Position, _printAction));
            tpObserver.Success += (sender, args1) =>
            {
                //UnbindObserver(tpObserver);
                PartialPositionClose?.Invoke(this, new PartialPositionCloseEventArgs { Position = args.Position, Volume = (args1 as PartialPositionCloseSignal).Volume });
            };
            tpObserver.BindTo(_bars);
            _observers.Add(new (args.Position, tpObserver));
        }
        
        public void OnPositionsClosed(PositionClosedEventArgs args)
        {
            _printAction($"Position closed: {args.Position} {args.Position.StopLoss}");
            var observers = _observers.Where(x => x.Item1 == args.Position).ToList();
            foreach (var o in observers)
            {
                UnbindObserver(o.Item2);
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
            _observers.Remove(_observers.First(x => x.Item2 == observer));
        }
    }
    
    class StopLossSignalResult : ISignalResult
    {
        public Position Position { get; set; }
        public ICollection<Signal> Signals { get; }
        public bool Valid { get; } = true;
    }
    
    class LongStopLossSignalCondition : ISignalCondition
    {
        public LongStopLossSignalCondition(Position position, double stopLossCorrectionThreshold)
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
    
    class ShortStopLossSignalCondition : ISignalCondition
    {
        public ShortStopLossSignalCondition(Position position, double stopLossCorrectionThreshold)
        {
            Position = position;
            _threshold = (Position.StopLoss - Position.EntryPrice) * stopLossCorrectionThreshold;
        }
        
        public Position Position { get; }
        private readonly double? _threshold;

        public ISignalResult Check(Bars bars)
        {
            if (_threshold.HasValue && Position.EntryPrice - bars.LastBar.Open < _threshold)
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
    
    class LongMinute15SignalCondition : ISignalCondition
    {
        public LongMinute15SignalCondition(double level)
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
                    Signals = new List<Signal>{
                        new Signal
                        {
                            Type = SignalType.LongBreakdown,
                            FoundAt = bars.Last().OpenTime,
                            Level = _level
                        }
                    }
                };
            }
            return null;
        }
    }
    
    class ShortMinute15SignalCondition : ISignalCondition
    {
        public ShortMinute15SignalCondition(double level)
        {
            _level = level;
        }
        
        private readonly double _level;

        public ISignalResult Check(Bars bars)
        {
            var signalBar = bars.Last(1);
            if (signalBar.Close < _level)
            {
                return new Minute15SignalResult
                {
                    Signals = new List<Signal>{
                        new Signal
                        {
                            Type = SignalType.ShortBreakdown,
                            FoundAt = bars.Last().OpenTime,
                            Level = _level
                        }
                    }
                };
            }
            return null;
        }
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
                        if (previousData.VolatilityGroup == VolatilityGroup.High)
                        {
                            if (Math.Abs(previousData.Bar.High - currentBar.Bar.Close) < _atrRate)
                            {
                                if (currentBar.Bar.Close < previousData.Bar.High)
                                {
                                    res.Signals.Add(new Signal
                                    {
                                        Type = currentBar.Bar.Open < currentBar.Bar.Close ? SignalType.LongBreakdown : SignalType.LongFalse,
                                        FoundAt = currentBar.Bar.OpenTime,
                                        Bar = previousData.Bar,
                                        Level = previousData.Bar.High
                                    });
                                }
                                else
                                {
                                    res.Signals.Add(new Signal
                                    {
                                        Type = currentBar.Bar.Open < currentBar.Bar.Close ? SignalType.ShortFalse : SignalType.ShortBreakdown,
                                        FoundAt = currentBar.Bar.OpenTime,
                                        Bar = previousData.Bar,
                                        Level = previousData.Bar.High
                                    });
                                }
                            }
                            
                            if (Math.Abs(previousData.Bar.Low - currentBar.Bar.Close) < _atrRate)
                            {
                                if (currentBar.Bar.Close < previousData.Bar.Low)
                                {
                                    /*res.Signals.Add(new Signal
                                    {
                                        Type = currentBar.Bar.Open < currentBar.Bar.Close ? SignalType.LongBreakdown : SignalType.LongFalse,
                                        FoundAt = currentBar.Bar.OpenTime,
                                        Bar = previousData.Bar,
                                        Level = previousData.Bar.Low
                                    });*/
                                }
                                else
                                {
                                    res.Signals.Add(new Signal
                                    {
                                        Type = currentBar.Bar.Open < currentBar.Bar.Close ? SignalType.ShortBreakdown : SignalType.ShortFalse,
                                        FoundAt = currentBar.Bar.OpenTime,
                                        Bar = previousData.Bar,
                                        Level = previousData.Bar.Low
                                    });
                                }
                            }
                        }
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
        LongFalse,
        ShortBreakdown,
        ShortFalse
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
    
    static class JsonSerializer
    {
        public static string ToJson<T>(T @object) where T : class, new()
        {
            using var ms = new MemoryStream();

            var ser = new DataContractJsonSerializer(typeof(T));
            ser.WriteObject(ms, @object);
            byte[] json = ms.ToArray();
            ms.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
        }
        
        public static T FromJson<T>(string json) where T : class, new()
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var ser = new DataContractJsonSerializer(typeof(T));
            var res = ser.ReadObject(ms) as T;
            ms.Close();
            return res;
        }
    }
}