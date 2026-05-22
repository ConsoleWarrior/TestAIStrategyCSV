using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TestAIStrategyCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            // ======================== НАСТРОЙКИ СИМУЛЯЦИИ ========================
            string filePath1 = @"H:\SSD\BZ_000101_041201.csv";
            string filePath2 = @"H:\SSD\BZ_050101_091231.csv";
            string filePath3 = @"H:\SSD\BZ_100101_141231.csv";
            string filePath4 = @"H:\SSD\BZ_150101_191231.csv";
            string filePath5 = @"H:\SSD\BZ_200101_241231.csv";
            string filePath11 = @"H:\SSD\BZ_250101_260521.csv";

            string filePath6 = @"H:\SSD\GOLD_000101_041231.csv";
            string filePath7 = @"H:\SSD\GOLD_050101_091231.csv";
            string filePath8 = @"H:\SSD\GOLD_100101_141231.csv";
            string filePath9 = @"H:\SSD\GOLD_150101_191231.csv";
            string filePath10 = @"H:\SSD\GOLD_200101_241231.csv";
            string filePath12 = @"H:\SSD\GOLD_250101_260522.csv";

            decimal initialCapital = 10000m;
            decimal commissionRate = 0.0003m;

            // =====================================================================

            if (!File.Exists(filePath1))
            {
                Console.WriteLine($"Ошибка: Файл не найден по пути {filePath1}");
                return;
            }

            var history = Controller.ParserDay(filePath6); //GOLD
            history.AddRange(Controller.ParserDay(filePath7));
            history.AddRange(Controller.ParserDay(filePath8));
            history.AddRange(Controller.ParserDay(filePath9));
            history.AddRange(Controller.ParserDay(filePath10));
            history.AddRange(Controller.ParserDay(filePath12));

            //var history = Controller.ParserDay(filePath1); //BRENT
            //history.AddRange(Controller.ParserDay(filePath2));
            //history.AddRange(Controller.ParserDay(filePath3));
            //history.AddRange(Controller.ParserDay(filePath4));
            //history.AddRange(Controller.ParserDay(filePath5));
            //history.AddRange(Controller.ParserDay(filePath11));


            Console.WriteLine($"Загружено {history.Count} свечей. Запуск Мультиробота...");

            Console.WriteLine($"Загружено {history.Count} свечей. Запуск раздельного анализа...");

            decimal testShare = 10000m; // Базовый капитал для теста каждого отдельного варианта

            // =====================================================================
            // 1. АНАЛИЗ ВАРИАНТОВ СТРАТЕГИИ ПРОБОЯ
            // =====================================================================
            Console.WriteLine("\n📈 РЕЗУЛЬТАТЫ СТРАТЕГИИ [ПРОБОЙ КАНАЛА] (Плечо 1.5х):");
            int[] breakoutOptions = { 10, 15, 20, 25, 30 };
            int bestBrkOpt = 15;
            decimal bestBrkBalance = 0m;

            foreach (int brkPeriod in breakoutOptions)
            {
                var engine = new BreakoutStrategy(testShare, commissionRate, brkPeriod);
                engine.SetLeverage(1.5m);

                for (int i = brkPeriod + 1; i < history.Count; i++)
                {
                    engine.Update(history.Take(i + 1).ToList());
                }
                engine.ForceClose(history.Last().Close, history.Last().Date);

                decimal profitPercent = ((engine.Balance - testShare) / testShare) * 100m;
                Console.WriteLine($"  Период {brkPeriod,2} дней -> Прибыль: {profitPercent.ToString("+0.0;-0.0;0.0"),7}% | Итоговый баланс: ${engine.Balance:F2} (Макс.Просадка: {engine.MaxDrawdown:F1}%)");

                if (engine.Balance > bestBrkBalance)
                {
                    bestBrkBalance = engine.Balance;
                    bestBrkOpt = brkPeriod;
                }
            }

            // =====================================================================
            // 2. АНАЛИЗ ВАРИАНТОВ СТРАТЕГИИ ИМПУЛЬСА (ATR)
            // =====================================================================
            Console.WriteLine("\n⚡ РЕЗУЛЬТАТЫ СТРАТЕГИИ [ИМПУЛЬС MA + ATR] (Плечо 1.5х):");
            decimal[] atrOptions = { 1.5m, 2.0m, 2.5m, 3.0m, 3.5m, 4.0m };
            decimal bestAtrOpt = 3.5m;
            decimal bestAtrBalance = 0m;

            foreach (decimal atrMult in atrOptions)
            {
                var engine = new MomentumStrategy(testShare, commissionRate, atrMult);
                engine.SetLeverage(1.5m);

                for (int i = 50; i < history.Count; i++)
                {
                    engine.Update(history.Take(i + 1).ToList());
                }
                engine.ForceClose(history.Last().Close, history.Last().Date);

                decimal profitPercent = ((engine.Balance - testShare) / testShare) * 100m;
                Console.WriteLine($"  Множитель ATR {atrMult:F1}x -> Прибыль: {profitPercent.ToString("+0.0;-0.0;0.0"),7}% | Итоговый баланс: ${engine.Balance:F2} (Макс.Просадка: {engine.MaxDrawdown:F1}%)");

                if (engine.Balance > bestAtrBalance)
                {
                    bestAtrBalance = engine.Balance;
                    bestAtrOpt = atrMult;
                }
            }

            // =====================================================================
            // 3. АНАЛИЗ ВАРИАНТОВ СТРАТЕГИИ КОНТРТРЕНДА (RSI)
            // =====================================================================
            Console.WriteLine("\n🔄 РЕЗУЛЬТАТЫ СТРАТЕГИИ [КОНТР-ТРЕНД RSI] (Плечо 1.5х):");
            int[] rsiOptions = { 10, 14, 18, 22 };
            int bestRsiOpt = 14;
            decimal bestRsiBalance = 0m;

            foreach (int rsiPeriod in rsiOptions)
            {
                //var engine = new CounterTrendStrategy(testShare, commissionRate, rsiPeriod);
                var engine = new CounterTrendStrategy(testShare, commissionRate, rsiPeriod, isInverted: true);

                engine.SetLeverage(1.5m);

                for (int i = 50; i < history.Count; i++)
                {
                    engine.Update(history.Take(i + 1).ToList());
                }
                engine.ForceClose(history.Last().Close, history.Last().Date);

                decimal profitPercent = ((engine.Balance - testShare) / testShare) * 100m;
                Console.WriteLine($"  Период RSI {rsiPeriod,2} дней -> Прибыль: {profitPercent.ToString("+0.0;-0.0;0.0"),7}% | Итоговый баланс: ${engine.Balance:F2} (Макс.Просадка: {engine.MaxDrawdown:F1}%)");

                if (engine.Balance > bestRsiBalance)
                {
                    bestRsiBalance = engine.Balance;
                    bestRsiOpt = rsiPeriod;
                }
            }

            // =====================================================================
            // 4. СИНХРОННЫЙ ПРОГОН ЛУЧШЕГО ПОРТФЕЛЯ (КАПИТАЛ ДЕЛИТСЯ НА 3)
            // =====================================================================
            Console.WriteLine("\n===============================================================================================");
            Console.WriteLine("🏆 ИТОГОВЫЙ СИНХРОННЫЙ СБОРНЫЙ ПОРТФЕЛЬ ИЗ ЛУЧШИХ ВАРИАНТОВ:");
            Console.WriteLine($"Выбранная комбинация: Пробой = {bestBrkOpt} дн. | ATR = {bestAtrOpt:F1}x | RSI = {bestRsiOpt} дн.");
            Console.WriteLine("===============================================================================================");

            decimal finalShare = initialCapital / 3m;
            var bestBreakout = new BreakoutStrategy(finalShare, commissionRate, bestBrkOpt);
            var bestMomentum = new MomentumStrategy(finalShare, commissionRate, bestAtrOpt);
            var bestCounter = new CounterTrendStrategy(finalShare, commissionRate, bestRsiOpt, isInverted: true);

            bestBreakout.SetLeverage(1.5m);
            bestMomentum.SetLeverage(1.5m);
            bestCounter.SetLeverage(1.5m);

            int finalStartIdx = Math.Max(50, Math.Max(bestBrkOpt + 1, bestRsiOpt + 1));
            for (int i = finalStartIdx; i < history.Count; i++)
            {
                var currentHistory = history.Take(i + 1).ToList();
                bestBreakout.Update(currentHistory);
                bestMomentum.Update(currentHistory);
                bestCounter.Update(currentHistory);
            }
            bestBreakout.ForceClose(history.Last().Close, history.Last().Date);
            bestMomentum.ForceClose(history.Last().Close, history.Last().Date);
            bestCounter.ForceClose(history.Last().Close, history.Last().Date);

            decimal finalTotalBalance = bestBreakout.Balance + bestMomentum.Balance + bestCounter.Balance;
            decimal finalProfitPercent = ((finalTotalBalance - initialCapital) / initialCapital) * 100m;
            decimal portfolioMaxDrawdown = (bestBreakout.MaxDrawdown + bestMomentum.MaxDrawdown + bestCounter.MaxDrawdown) / 3m;

            Console.WriteLine($"Итоговый максимальный баланс портфеля: ${finalTotalBalance:F2} ({finalProfitPercent:+0.0;-0.0;0.0}%)");
            Console.WriteLine($"МАКСИМАЛЬНАЯ ИСТОРИЧЕСКАЯ ПРОСАДКА ПОРТФЕЛЯ: {portfolioMaxDrawdown:F1}%");
            Console.WriteLine("===============================================================================================");

            // ==================== БЛОК ТОРГОВЫХ РЕКОМЕНДАЦИЙ НА СЕГОДНЯ ====================
            Console.WriteLine($"\nℹ️  ФОРМИРОВАНИЕ ТОРГОВЫХ СИГНАЛОВ НА СЕГОДНЯ ({history.Last().Date:yyyy-MM-dd})");
            Console.WriteLine("----------------------------------------------------------------------");

            var finalHistory = history.ToList();
            var lastCandle = finalHistory.Last();

            // 1. Сигнал Пробоя
            var breakoutWindow = finalHistory.Skip(finalHistory.Count - (bestBrkOpt + 1)).Take(bestBrkOpt).ToList();
            decimal brkHigh = breakoutWindow.Max(c => c.Close);
            decimal brkLow = breakoutWindow.Min(c => c.Close);

            Console.Write("1. Стратегия [Пробой канала]: ");
            if (lastCandle.Close > brkHigh) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (>${brkHigh})"); }
            else if (lastCandle.Close < brkLow) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (<${brkLow})"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine($"ВНЕ РЫНКА (Канал: ${brkLow} - ${brkHigh})"); }
            Console.ResetColor();

            // 2. Сигнал Импульса
            decimal ma10Now = finalHistory.Skip(finalHistory.Count - 10).Average(c => c.Close);
            decimal ma30Now = finalHistory.Skip(finalHistory.Count - 30).Average(c => c.Close);
            var prevHistory = finalHistory.Take(finalHistory.Count - 1).ToList();
            decimal ma10Prev = prevHistory.Skip(prevHistory.Count - 10).Average(c => c.Close);
            decimal ma30Prev = prevHistory.Skip(prevHistory.Count - 30).Average(c => c.Close);

            int momAtrPeriod = 14;
            var momAtrWindow = finalHistory.Skip(finalHistory.Count - momAtrPeriod).Take(momAtrPeriod).ToList();
            decimal momTotalRange = 0m;
            for (int i = 1; i < momAtrWindow.Count; i++) momTotalRange += Math.Abs(momAtrWindow[i].Close - momAtrWindow[i - 1].Close);
            decimal momCurrentAtr = momTotalRange / (momAtrPeriod - 1);

            Console.Write("2. Стратегия [Импульс MA + ATR]: ");
            if (ma10Prev <= ma30Prev && ma10Now > ma30Now) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (Пересечение! Стоп: ${lastCandle.Close - (bestAtrOpt * momCurrentAtr):F2})"); }
            else if (ma10Prev >= ma30Prev && ma10Now < ma30Now) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (Пересечение! Стоп: ${lastCandle.Close + (bestAtrOpt * momCurrentAtr):F2})"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; string dir = (ma10Now > ma30Now) ? "ЛОНГ" : "ШОРТ"; Console.WriteLine($"УДЕРЖАНИЕ / ПОИСК ВХОДА (Тренд {dir})"); }
            Console.ResetColor();

            // 3. Сигнал Контртренда
            var rsiWindow = finalHistory.Skip(finalHistory.Count - (bestRsiOpt + 1)).Take(bestRsiOpt + 1).ToList();
            decimal finalGain = 0m, finalLoss = 0m;
            for (int i = 1; i < rsiWindow.Count; i++)
            {
                decimal diff = rsiWindow[i].Close - rsiWindow[i - 1].Close;
                if (diff > 0) finalGain += diff; else finalLoss += Math.Abs(diff);
            }
            decimal avgGain = finalGain / bestRsiOpt, avgLoss = finalLoss / bestRsiOpt;
            decimal rsiNow = avgLoss != 0m ? 100m - (100m / (1m + (avgGain / avgLoss))) : 50m;

            //Console.Write("3. Стратегия [Контр-тренд RSI]: ");
            //if (rsiNow < 30m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (Перепроданность, RSI={rsiNow:F1})"); }
            //else if (rsiNow > 70m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (Перекупленность, RSI={rsiNow:F1})"); }
            //else { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine($"ВНЕ РЫНКА (RSI нейтрален: {rsiNow:F1})"); }
            //Console.ResetColor();
            Console.Write("3. Стратегия [Импульс RSI]: ");
            if (rsiNow > 70m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (Разгон тренда, RSI={rsiNow:F1} > 70)"); }
            else if (rsiNow < 30m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (Капитуляция рынка, RSI={rsiNow:F1} < 30)"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine($"ВНЕ РЫНКА (Ожидание импульса, RSI: {rsiNow:F1})"); }
            Console.ResetColor();

            Console.WriteLine("======================================================================");
            Console.ReadLine();
        }


    }

    public class Candle
    {
        public DateTime Date { get; set; }
        public decimal Close { get; set; }
    }

    public abstract class BaseStrategy
    {
        protected decimal RiskMultiplier = 1.0m;
        private decimal _baseLeverage = 1.0m;

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
            if (targetPosition == CurrentPosition) return;

            if (CurrentPosition != 0)
            {
                decimal tradeReturn = CurrentPosition == 1
                    ? (currentPrice - EntryPrice) / EntryPrice
                    : (EntryPrice - currentPrice) / EntryPrice;

                decimal adjustedReturn = tradeReturn * RiskMultiplier;

                // ИСПРАВЛЕНО: Комиссия считается от реального объема позы, а не вычитается из всего баланса
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
                RiskMultiplier = _baseLeverage; // ИСПРАВЛЕНО: Возврат к целевому плечу при новом пике
            }
            else if (_peakBalance > 0)
            {
                decimal currentDrawdown = (_peakBalance - Balance) / _peakBalance * 100m;

                if (currentDrawdown > MaxDrawdown)
                {
                    MaxDrawdown = currentDrawdown;
                }

                // ИСПРАВЛЕНО: Динамический выход из "Режима паники", если просадка уменьшилась
                if (currentDrawdown > 35.0m)
                {
                    RiskMultiplier = _baseLeverage * 0.33m;
                }
                else
                {
                    RiskMultiplier = _baseLeverage;
                }
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

            // ИСПРАВЛЕНО: ATR считает последние 14 свечей, включая ТЕКУЩУЮ свечу (без задержки на сутки)
            int atrPeriod = 14;
            var atrWindow = history.Skip(history.Count - atrPeriod).Take(atrPeriod).ToList();

            decimal totalRange = 0m;
            for (int i = 1; i < atrWindow.Count; i++)
            {
                totalRange += Math.Abs(atrWindow[i].Close - atrWindow[i - 1].Close);
            }
            decimal currentAtr = totalRange / (atrPeriod - 1);

            if (currentAtr == 0m) currentAtr = currentCandle.Close * 0.01m;

            // ИСПРАВЛЕНО: Симметричный трейлинг-стоп по ATR как для Лонга, так и для Шорта
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
        private bool _isInverted; // Флаг: true = по тренду, false = контр-тренд

        // Добавили параметр isInverted в конструктор
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
            var rsiWindow = history.Skip(history.Count - (_rsiPeriod + 1)).Take(_rsiPeriod + 1).ToList(); // (в коде используйте _rsiPeriod + 1)

            for (int i = 1; i < rsiWindow.Count; i++)
            {
                decimal difference = rsiWindow[i].Close - rsiWindow[i - 1].Close;
                if (difference > 0) sumGain += difference; else sumLoss += Math.Abs(difference);
            }

            decimal averageGain = sumGain / _rsiPeriod;
            decimal averageLoss = sumLoss / _rsiPeriod;
            decimal rsi = averageLoss != 0m ? 100m - (100m / (1m + (averageGain / averageLoss))) : 50m;

            int signal = CurrentPosition;

            // Автоматическое переключение логики одной строкой!
            if (CurrentPosition == 0)
            {
                if (rsi < 30m) signal = _isInverted ? -1 : 1;  // Трендовый шорт ИЛИ контр-трендовый лонг
                else if (rsi > 70m) signal = _isInverted ? 1 : -1; // Трендовый лонг ИЛИ контр-трендовый шорт
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
