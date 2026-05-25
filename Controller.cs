using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TestAIStrategyCSV
{
    public class Controller
    {
        // Список поддерживаемых форматов дат (с дефисами, слэшами, точками и временем)
        private static readonly string[] DateFormats = new[]
        {
            "yyyyMMdd", "yyyy-MM-dd", "dd.MM.yyyy", "MM/dd/yyyy",
            "yyyyMMdd HHmmss", "yyyy-MM-dd HH:mm:ss", "dd.MM.yyyy HH:mm:ss",
            "yyyy-MM-dd HH:mm", "dd.MM.yyyy HH:mm"
        };

        public static List<Candle> ParserDay(string filePath)
        {
            var history = new List<Candle>();
            if (!File.Exists(filePath)) return history;

            string[] lines = File.ReadAllLines(filePath);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] columns = lines[i].Split(';', ',');

                try
                {
                    // ИСПРАВЛЕНО: Гибкий парсинг даты из массива форматов
                    if (!DateTime.TryParseExact(columns[2].Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        // Резервный вариант, если формат совсем нестандартный
                        date = DateTime.Parse(columns[2].Trim(), CultureInfo.InvariantCulture);
                    }
                    
                    decimal close = decimal.Parse(columns[4].Trim(), CultureInfo.InvariantCulture);
                    //if (int.Parse(columns[5].Trim(), CultureInfo.InvariantCulture) > 2)
                    //    history.Add(new Candle { Date = date, Close = close });
                    if (history.Count == 0) history.Add(new Candle { Date = date, Close = close });
                    else
                    if (history[history.Count - 1].Close != close )
                        history.Add(new Candle { Date = date, Close = close });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга строки {i + 1}: {ex.Message}");
                }
            }

            // Очистка от дубликатов: для дневного таймфрейма оставляем только последнюю свечу дня
            history = history
                .GroupBy(c => c.Date.Date)
                .Select(g => g.OrderBy(c => c.Date).Last())
                .OrderBy(c => c.Date)
                .ToList();

            Console.WriteLine($"[Day] Считано и очищено: {history.Count} свечей.");
            return history;
        }

        public static List<Candle> ParserHour(string filePath)
        {
            var history = new List<Candle>();
            if (!File.Exists(filePath)) return history;

            string[] lines = File.ReadAllLines(filePath);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] columns = lines[i].Split(';', ',');

                try
                {
                    // Если дата и время в CSV разделены на две колонки (например, Col0: Date, Col1: Time)
                    string dateStr = columns[0].Trim();
                    if (columns.Length > 1 && (columns[1].Contains(":") || (columns[1].Length == 6 && int.TryParse(columns[1], out _))))
                    {
                        dateStr += " " + columns[1].Trim();
                    }

                    if (!DateTime.TryParseExact(dateStr, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        date = DateTime.Parse(dateStr, CultureInfo.InvariantCulture);
                    }

                    // ИСПРАВЛЕНО: Если время склеилось, индекс цены закрытия может сместиться. 
                    // Проверяем 5-й или последний доступный индекс.
                    int closeIndex = columns.Length > 5 ? 5 : columns.Length - 1;
                    decimal close = decimal.Parse(columns[closeIndex].Trim(), CultureInfo.InvariantCulture);

                    history.Add(new Candle { Date = date, Close = close });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга строки {i + 1}: {ex.Message}");
                }
            }

            // Для часовых данных сортируем по точному времени, дубликаты не схлопываем
            history = history.OrderBy(c => c.Date).ToList();
            Console.WriteLine($"[Hour] Считано часовых свечей: {history.Count}");
            return history;
        }
    }
}
