using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestAIStrategyCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            // ============================ ТУМБЛЕРЫ НАСТРОЕК ============================
            // Выбор инструмента: true = Gold, false = Brent
            bool useGold = false;   // <-- Меняй здесь для переключения между графиками

            // Флаги для разрешения направлений (можно подстроить под стратегии)
            bool allowLongGlobal = true;
            bool allowShortGlobal = false;

            // Настройки плеча и комиссии
            decimal initialCapital = 10000m;
            decimal commissionRate = 0.0003m;
            decimal leverage = 5m;

            // Параметры перебора стратегий (можно менять прямо здесь)
            int[] breakoutOptions = { 10, 15, 20, 25, 30 };
            decimal[] atrOptions = { 1.5m, 2.0m, 2.5m, 3.0m, 3.5m, 4.0m };
            int[] rsiOptions = { 10, 14, 18, 22 };
            // ===========================================================================

            // ---------------------- Загрузка файлов в зависимости от тумблера ----------------------
            List<Candle> history = new List<Candle>();
            if (useGold)
            {
                string[] goldFiles = {
                    //@"Gold_1day_01012016_25052026.csv"
                    //@"gold_daily.csv"
                    //@"H:\SSD\GOLD_100101_141231.csv",
                    //@"H:\SSD\GOLD_150101_191231.csv",
                    //@"H:\SSD\GOLD_200101_241231.csv",
                    //@"H:\SSD\GOLD_250101_260522.csv"
                };
                foreach (var f in goldFiles)
                    if (File.Exists(f)) history.AddRange(Controller.ParserDay(f));
                    else Console.WriteLine($"Файл не найден: {f}");
            }
            else
            {
                string[] brentFiles = {
                    //@"_Brent_Crude_Oil_1day_11052001_25052026.csv"
                    @"BRENT_DAY.csv"
                    //@"H:\SSD\BZ_100101_141231.csv",
                    //@"H:\SSD\BZ_150101_191231.csv",
                    //@"H:\SSD\BZ_200101_241231.csv",
                    //@"H:\SSD\BZ_250101_260521.csv"
                };
                foreach (var f in brentFiles)
                    if (File.Exists(f)) history.AddRange(Controller.ParserDay(f));
                    else Console.WriteLine($"Файл не найден: {f}");
            }

            if (history.Count == 0)
            {
                Console.WriteLine("Ошибка: не загружено ни одной свечи. Проверь пути к CSV.");
                return;
            }

            history = history.OrderBy(c => c.Date).ToList();
            decimal testShare = 10000m;

            // =====================================================================
            // 1. ПРОБОЙ КАНАЛА
            // =====================================================================
            Console.WriteLine($"\n📈 РЕЗУЛЬТАТЫ СТРАТЕГИИ [ПРОБОЙ КАНАЛА] (Плечо {leverage}x):");
            int bestBrkOpt = 15;
            decimal bestBrkBalance = 0m;

            foreach (int brkPeriod in breakoutOptions)
            {
                var engine = new BreakoutStrategy(testShare, commissionRate, brkPeriod);
                engine.SetLeverage(leverage);
                engine.AllowLong = allowLongGlobal;
                engine.AllowShort = allowShortGlobal;

                for (int i = brkPeriod + 1; i < history.Count; i++)
                    engine.Update(history.Take(i + 1).ToList());
                engine.ForceClose(history.Last().Close, history.Last().Date);

                decimal profitPercent = ((engine.Balance - testShare) / testShare) * 100m;
                Console.WriteLine($"  Период {brkPeriod,2} дней -> Прибыль: {profitPercent.ToString("+0.0;-0.0;0.0"),7}% | Баланс: ${engine.Balance:F2} (Просадка: {engine.MaxDrawdown:F1}%) Сделок: {engine.TotalTrades}");
                if (engine.Balance > bestBrkBalance) { bestBrkBalance = engine.Balance; bestBrkOpt = brkPeriod; }
            }

            // =====================================================================
            // 2. ИМПУЛЬС MA + ATR
            // =====================================================================
            Console.WriteLine($"\n⚡ РЕЗУЛЬТАТЫ СТРАТЕГИИ [ИМПУЛЬС MA + ATR] (Плечо {leverage}x):");
            decimal bestAtrOpt = 3.5m;
            decimal bestAtrBalance = 0m;

            foreach (decimal atrMult in atrOptions)
            {
                var engine = new MomentumStrategy(testShare, commissionRate, atrMult);
                engine.SetLeverage(leverage);
                engine.AllowLong = allowLongGlobal;
                engine.AllowShort = allowShortGlobal;

                for (int i = 50; i < history.Count; i++)
                    engine.Update(history.Take(i + 1).ToList());
                engine.ForceClose(history.Last().Close, history.Last().Date);

                decimal profitPercent = ((engine.Balance - testShare) / testShare) * 100m;
                Console.WriteLine($"  Множитель ATR {atrMult:F1}x -> Прибыль: {profitPercent.ToString("+0.0;-0.0;0.0"),7}% | Баланс: ${engine.Balance:F2} (Просадка: {engine.MaxDrawdown:F1}%) Сделок: {engine.TotalTrades}");
                if (engine.Balance > bestAtrBalance) { bestAtrBalance = engine.Balance; bestAtrOpt = atrMult; }
            }

            // =====================================================================
            // 3. КОНТР-ТРЕНД / ИМПУЛЬС RSI
            // =====================================================================
            Console.WriteLine($"\n🔄 РЕЗУЛЬТАТЫ СТРАТЕГИИ [КОНТР-ТРЕНД RSI] (Плечо {leverage}x):");
            int bestRsiOpt = 14;
            decimal bestRsiBalance = 0m;

            foreach (int rsiPeriod in rsiOptions)
            {
                var engine = new CounterTrendStrategy(testShare, commissionRate, rsiPeriod, isInverted: true);
                engine.SetLeverage(leverage);
                engine.AllowLong = allowLongGlobal;
                engine.AllowShort = allowShortGlobal;

                for (int i = 50; i < history.Count; i++)
                    engine.Update(history.Take(i + 1).ToList());
                engine.ForceClose(history.Last().Close, history.Last().Date);

                decimal profitPercent = ((engine.Balance - testShare) / testShare) * 100m;
                Console.WriteLine($"  Период RSI {rsiPeriod,2} дней -> Прибыль: {profitPercent.ToString("+0.0;-0.0;0.0"),7}% | Баланс: ${engine.Balance:F2} (Просадка: {engine.MaxDrawdown:F1}%) Сделок: {engine.TotalTrades}");
                if (engine.Balance > bestRsiBalance) { bestRsiBalance = engine.Balance; bestRsiOpt = rsiPeriod; }
            }

            // =====================================================================
            // 4. СБОРНЫЙ ПОРТФЕЛЬ
            // =====================================================================
            Console.WriteLine("\n===============================================================================================");
            Console.WriteLine("🏆 ИТОГОВЫЙ СИНХРОННЫЙ СБОРНЫЙ ПОРТФЕЛЬ ИЗ ЛУЧШИХ ВАРИАНТОВ:");
            Console.WriteLine($"Выбранная комбинация: Пробой = {bestBrkOpt} дн. | ATR = {bestAtrOpt:F1}x | RSI = {bestRsiOpt} дн.");
            Console.WriteLine("===============================================================================================");

            decimal finalShare = initialCapital / 3m;
            var bestBreakout = new BreakoutStrategy(finalShare, commissionRate, bestBrkOpt);
            var bestMomentum = new MomentumStrategy(finalShare, commissionRate, bestAtrOpt);
            var bestCounter = new CounterTrendStrategy(finalShare, commissionRate, bestRsiOpt, isInverted: true);

            bestBreakout.SetLeverage(leverage);
            bestMomentum.SetLeverage(leverage);
            bestCounter.SetLeverage(leverage);

            bestBreakout.AllowLong = allowLongGlobal;
            bestBreakout.AllowShort = allowShortGlobal;
            bestMomentum.AllowLong = allowLongGlobal;
            bestMomentum.AllowShort = allowShortGlobal;
            bestCounter.AllowLong = allowLongGlobal;
            bestCounter.AllowShort = allowShortGlobal;

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
            int totalTradesPortfolio = bestBreakout.TotalTrades + bestMomentum.TotalTrades + bestCounter.TotalTrades;

            Console.WriteLine($"Итоговый баланс портфеля: ${finalTotalBalance:F2} ({finalProfitPercent:+0.0;-0.0;0.0}%)");
            Console.WriteLine($"МАКСИМАЛЬНАЯ ПРОСАДКА ПОРТФЕЛЯ: {portfolioMaxDrawdown:F1}%");
            Console.WriteLine($"ОБЩЕЕ КОЛИЧЕСТВО СДЕЛОК ПОРТФЕЛЯ: {totalTradesPortfolio}");
            Console.WriteLine("===============================================================================================");

            // ==================== СИГНАЛЫ НА ПОСЛЕДНИЙ ДЕНЬ ====================
            Console.WriteLine($"\nℹ️  ФОРМИРОВАНИЕ ТОРГОВЫХ СИГНАЛОВ НА {history.Last().Date:yyyy-MM-dd}");
            Console.WriteLine("----------------------------------------------------------------------");

            var lastCandle = history.Last();

            // Пробой
            var brkWindow = history.Skip(history.Count - (bestBrkOpt + 1)).Take(bestBrkOpt).ToList();
            decimal brkHigh = brkWindow.Max(c => c.Close);
            decimal brkLow = brkWindow.Min(c => c.Close);
            Console.Write("1. Стратегия [Пробой канала]: ");
            if (lastCandle.Close > brkHigh) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (>${brkHigh})"); }
            else if (lastCandle.Close < brkLow) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (<${brkLow})"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine($"ВНЕ РЫНКА (Канал: ${brkLow} - ${brkHigh})"); }
            Console.ResetColor();

            // Импульс
            decimal ma10Now = history.Skip(history.Count - 10).Average(c => c.Close);
            decimal ma30Now = history.Skip(history.Count - 30).Average(c => c.Close);
            var prevHistory = history.Take(history.Count - 1).ToList();
            decimal ma10Prev = prevHistory.Skip(prevHistory.Count - 10).Average(c => c.Close);
            decimal ma30Prev = prevHistory.Skip(prevHistory.Count - 30).Average(c => c.Close);

            int momAtrPeriod = 14;
            var momAtrWindow = history.Skip(history.Count - momAtrPeriod).Take(momAtrPeriod).ToList();
            decimal momTotalRange = 0m;
            for (int i = 1; i < momAtrWindow.Count; i++) momTotalRange += Math.Abs(momAtrWindow[i].Close - momAtrWindow[i - 1].Close);
            decimal momCurrentAtr = momTotalRange / (momAtrPeriod - 1);

            Console.Write("2. Стратегия [Импульс MA + ATR]: ");
            if (ma10Prev <= ma30Prev && ma10Now > ma30Now) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (Стоп: ${lastCandle.Close - (bestAtrOpt * momCurrentAtr):F2})"); }
            else if (ma10Prev >= ma30Prev && ma10Now < ma30Now) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (Стоп: ${lastCandle.Close + (bestAtrOpt * momCurrentAtr):F2})"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; string dir = (ma10Now > ma30Now) ? "ЛОНГ" : "ШОРТ"; Console.WriteLine($"УДЕРЖАНИЕ / ПОИСК ВХОДА (Тренд {dir})"); }
            Console.ResetColor();

            // RSI (трендовый вариант - isInverted=true)
            var rsiWindow = history.Skip(history.Count - (bestRsiOpt + 1)).Take(bestRsiOpt + 1).ToList();
            decimal sumGain = 0m, sumLoss = 0m;
            for (int i = 1; i < rsiWindow.Count; i++)
            {
                decimal diff = rsiWindow[i].Close - rsiWindow[i - 1].Close;
                if (diff > 0) sumGain += diff; else sumLoss += Math.Abs(diff);
            }
            decimal avgGain = sumGain / bestRsiOpt, avgLoss = sumLoss / bestRsiOpt;
            decimal rsiNow = avgLoss != 0m ? 100m - (100m / (1m + (avgGain / avgLoss))) : 50m;

            Console.Write("3. Стратегия [Импульс RSI]: ");
            if (rsiNow > 70m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"ВХОД В ЛОНГ (Разгон тренда, RSI={rsiNow:F1} > 70)"); }
            else if (rsiNow < 30m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ВХОД В ШОРТ (Капитуляция, RSI={rsiNow:F1} < 30)"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine($"ВНЕ РЫНКА (RSI: {rsiNow:F1})"); }
            Console.ResetColor();

            Console.WriteLine("======================================================================");
            Console.ReadLine();
        }
    }
}