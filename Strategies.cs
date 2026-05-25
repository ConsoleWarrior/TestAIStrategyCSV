using System;
using System.Collections.Generic;
using System.Linq;

namespace TestAIStrategyCSV
{
    public class Candle
    {
        public DateTime Date { get; set; }
        public decimal Close { get; set; }
    }

    public abstract class BaseStrategy
    {
        protected decimal RiskMultiplier = 1.0m;
        private decimal _baseLeverage = 1.0m;

        // Флаги управления направлениями (тумблеры)
        public bool AllowLong { get; set; } = true;
        public bool AllowShort { get; set; } = true;

        public decimal Balance { get; protected set; }
        public int TotalTrades { get; protected set; }
        public decimal MaxDrawdown { get; protected set; }
        private decimal _peakBalance;

        protected decimal CommissionRate;
        protected int CurrentPosition = 0;
        protected decimal EntryPrice = 0m;

        public BaseStrategy(decimal startCapital, decimal commission)
        {
            Balance = startCapital;
            _peakBalance = startCapital;
            MaxDrawdown = 0m;
            CommissionRate = commission;
        }

        public void SetLeverage(decimal leverage)
        {
            _baseLeverage = leverage;
            RiskMultiplier = leverage;
        }

        protected void Trade(int targetPosition, decimal currentPrice, DateTime date)
        {
            // Блокируем вход, если направление запрещено флагами
            if (targetPosition == 1 && !AllowLong) targetPosition = 0;
            if (targetPosition == -1 && !AllowShort) targetPosition = 0;

            if (targetPosition == CurrentPosition) return;

            if (CurrentPosition != 0)
            {
                decimal tradeReturn = CurrentPosition == 1
                    ? (currentPrice - EntryPrice) / EntryPrice
                    : (EntryPrice - currentPrice) / EntryPrice;

                decimal adjustedReturn = tradeReturn * RiskMultiplier;

                decimal positionVolume = Balance * RiskMultiplier;
                decimal commission = positionVolume * CommissionRate;

                Balance = Balance * (1 + adjustedReturn) - commission;
                TotalTrades++;

                UpdateDrawdown();
            }

            CurrentPosition = targetPosition;
            EntryPrice = currentPrice;

            if (CurrentPosition != 0)
            {
                decimal positionVolume = Balance * RiskMultiplier;
                Balance -= positionVolume * CommissionRate;
            }

            UpdateDrawdown();
        }

        private void UpdateDrawdown()
        {
            if (Balance > _peakBalance)
            {
                _peakBalance = Balance;
                MaxDrawdown = 0m;
                RiskMultiplier = _baseLeverage;
            }
            else if (_peakBalance > 0)
            {
                decimal currentDrawdown = (_peakBalance - Balance) / _peakBalance * 100m;

                if (currentDrawdown > MaxDrawdown)
                {
                    MaxDrawdown = currentDrawdown;
                }

                if (currentDrawdown > 35.0m)
                    RiskMultiplier = _baseLeverage * 0.33m;
                else
                    RiskMultiplier = _baseLeverage;
            }
        }

        public void ForceClose(decimal finalPrice, DateTime date)
        {
            if (CurrentPosition != 0) Trade(0, finalPrice, date);
        }
    }

    public class BreakoutStrategy : BaseStrategy
    {
        private int _period;

        public BreakoutStrategy(decimal capital, decimal comm, int period) : base(capital, comm)
        {
            _period = period;
        }

        public void Update(List<Candle> history)
        {
            if (history.Count < _period + 1) return;

            var currentCandle = history.Last();
            var window = history.Skip(history.Count - (_period + 1)).Take(_period).ToList();

            if (window.Count == 0) return;

            decimal maxHigh = window.Max(c => c.Close);
            decimal minLow = window.Min(c => c.Close);

            int signal = CurrentPosition;
            if (currentCandle.Close > maxHigh) signal = 1;
            else if (currentCandle.Close < minLow) signal = -1;

            Trade(signal, currentCandle.Close, currentCandle.Date);
        }
    }

    public class MomentumStrategy : BaseStrategy
    {
        private decimal _extremePriceSinceEntry = 0m;
        private decimal _atrMultiplier;

        public MomentumStrategy(decimal capital, decimal comm, decimal atrMultiplier) : base(capital, comm)
        {
            _atrMultiplier = atrMultiplier;
        }

        public void Update(List<Candle> history)
        {
            if (history.Count < 31) return;
            var currentCandle = history.Last();

            decimal ma10 = history.Skip(history.Count - 10).Average(c => c.Close);
            decimal ma30 = history.Skip(history.Count - 30).Average(c => c.Close);
            int signal = (ma10 > ma30) ? 1 : -1;

            int atrPeriod = 14;
            var atrWindow = history.Skip(history.Count - atrPeriod).Take(atrPeriod).ToList();

            decimal totalRange = 0m;
            for (int i = 1; i < atrWindow.Count; i++)
                totalRange += Math.Abs(atrWindow[i].Close - atrWindow[i - 1].Close);
            decimal currentAtr = totalRange / (atrPeriod - 1);

            if (currentAtr == 0m) currentAtr = currentCandle.Close * 0.01m;

            if (CurrentPosition == 1)
            {
                if (currentCandle.Close > _extremePriceSinceEntry)
                    _extremePriceSinceEntry = currentCandle.Close;

                decimal atrStopPrice = _extremePriceSinceEntry - (_atrMultiplier * currentAtr);
                if (currentCandle.Close < atrStopPrice) signal = 0;
            }
            else if (CurrentPosition == -1)
            {
                if (currentCandle.Close < _extremePriceSinceEntry || _extremePriceSinceEntry == 0m)
                    _extremePriceSinceEntry = currentCandle.Close;

                decimal atrStopPrice = _extremePriceSinceEntry + (_atrMultiplier * currentAtr);
                if (currentCandle.Close > atrStopPrice) signal = 0;
            }
            else if (CurrentPosition == 0)
            {
                _extremePriceSinceEntry = currentCandle.Close;
            }

            Trade(signal, currentCandle.Close, currentCandle.Date);
        }
    }

    public class CounterTrendStrategy : BaseStrategy
    {
        private int _rsiPeriod;
        private bool _isInverted;

        public CounterTrendStrategy(decimal capital, decimal comm, int rsiPeriod, bool isInverted) : base(capital, comm)
        {
            _rsiPeriod = rsiPeriod;
            _isInverted = isInverted;
        }

        public void Update(List<Candle> history)
        {
            if (history.Count < _rsiPeriod + 1) return;

            var currentCandle = history.Last();
            decimal sumGain = 0m, sumLoss = 0m;
            var rsiWindow = history.Skip(history.Count - (_rsiPeriod + 1)).Take(_rsiPeriod + 1).ToList();

            for (int i = 1; i < rsiWindow.Count; i++)
            {
                decimal difference = rsiWindow[i].Close - rsiWindow[i - 1].Close;
                if (difference > 0) sumGain += difference;
                else sumLoss += Math.Abs(difference);
            }

            decimal averageGain = sumGain / _rsiPeriod;
            decimal averageLoss = sumLoss / _rsiPeriod;
            decimal rsi = averageLoss != 0m ? 100m - (100m / (1m + (averageGain / averageLoss))) : 50m;

            int signal = CurrentPosition;

            if (CurrentPosition == 0)
            {
                if (rsi < 30m) signal = _isInverted ? -1 : 1;
                else if (rsi > 70m) signal = _isInverted ? 1 : -1;
            }
            else if (CurrentPosition == 1)
            {
                if (_isInverted ? rsi < 60m : rsi >= 50m) signal = 0;
            }
            else if (CurrentPosition == -1)
            {
                if (_isInverted ? rsi > 40m : rsi <= 50m) signal = 0;
            }

            Trade(signal, currentCandle.Close, currentCandle.Date);
        }
    }
}