using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;
using System.Text;

namespace BenchmarksSerializeDeserialize;

// Класс для подавления вывода
public class OutputSuppressor : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly StringWriter _stringWriter;

    public OutputSuppressor()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;
        _stringWriter = new StringWriter();

        Console.SetOut(_stringWriter);
        Console.SetError(_stringWriter);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _stringWriter.Dispose();
    }

    public string GetOutput()
    {
        return _stringWriter.ToString();
    }
}

internal class Program
{
    // Флаги для управления выводом
    private static bool ShowVerboseInfo = true;
    private static bool ShowSystemInfo = true;
    private static bool ShowAnalysis = true;
    private static bool ShowRecommendations = true;
    private static bool ExportResultsToFile = true;
    private static bool ShowBenchmarkOutput = false; // По умолчанию скрываем вывод бенчмарка

    static void Main(string[] args)
    {
        // Парсим аргументы командной строки
        ParseCommandLineArgs(args);

        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "UserProfile Serialization Benchmark";

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        БЕНЧМАРК СРАВНЕНИЯ JSON vs BINARY SERIALIZATION       ║");
        Console.WriteLine("║               UserProfile Serialization Test                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        ShowMenu();
    }

    static void ParseCommandLineArgs(string[] args)
    {
        foreach (var arg in args)
        {
            switch (arg.ToLower())
            {
                case "--quiet":
                case "-q":
                    ShowVerboseInfo = false;
                    ShowSystemInfo = false;
                    ShowAnalysis = false;
                    ShowRecommendations = false;
                    ExportResultsToFile = false;
                    break;
                case "--minimal":
                case "-m":
                    ShowVerboseInfo = false;
                    ShowSystemInfo = true;
                    ShowAnalysis = true;
                    ShowRecommendations = true;
                    ExportResultsToFile = true;
                    break;
                case "--verbose":
                case "-v":
                    ShowVerboseInfo = true;
                    ShowSystemInfo = true;
                    ShowAnalysis = true;
                    ShowRecommendations = true;
                    ExportResultsToFile = true;
                    break;
                case "--show-benchmark-output":
                case "-sbo":
                    ShowBenchmarkOutput = true;
                    break;
                case "--no-export":
                case "-ne":
                    ExportResultsToFile = false;
                    break;
                case "--no-system":
                case "-ns":
                    ShowSystemInfo = false;
                    break;
                case "--no-analysis":
                case "-na":
                    ShowAnalysis = false;
                    break;
                case "--no-recommendations":
                case "-nr":
                    ShowRecommendations = false;
                    break;
                case "--help":
                case "-h":
                case "-?":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
Использование: BenchmarksSerializeDeserialize [опции]

Опции:
  -q, --quiet          Минимальный вывод (только результаты)
  -m, --minimal        Компактный вывод (без деталей)
  -v, --verbose        Полный вывод (по умолчанию)
  -sbo, --show-benchmark-output  Показывать вывод бенчмарка (по умолчанию скрыт)
  -ne, --no-export     Не экспортировать результаты в CSV
  -ns, --no-system     Не показывать системную информацию
  -na, --no-analysis   Не показывать анализ
  -nr, --no-recommendations  Не показывать рекомендации
  -h, --help           Показать эту справку

Примеры:
  BenchmarksSerializeDeserialize -q        # Только результаты
  BenchmarksSerializeDeserialize -m -ne    # Минимальный вывод без экспорта
  BenchmarksSerializeDeserialize -v        # Полный вывод (по умолчанию)
");
    }

    static void ShowMenu()
    {
        Console.WriteLine($"\n📌 Текущий режим вывода: {(ShowVerboseInfo ? "Полный" : ShowSystemInfo ? "Компактный" : "Минимальный")}");
        Console.WriteLine($"   Экспорт: {(ExportResultsToFile ? "Включен" : "Выключен")}");
        Console.WriteLine($"   Вывод бенчмарка: {(ShowBenchmarkOutput ? "Показывать" : "Скрыт")}");
        Console.WriteLine();

        Console.WriteLine("Выберите режим тестирования:");
        Console.WriteLine(" 1. Полный бенчмарк (все методы)");
        Console.WriteLine(" 2. Только сериализация");
        Console.WriteLine(" 3. Только десериализация");
        Console.WriteLine(" 4. Только размер данных");
        Console.WriteLine(" 5. Тестирование списков");
        Console.WriteLine(" 6. Тестирование с разной длиной имени");
        Console.WriteLine(" 7. Сравнение Roundtrip (сериализация + десериализация)");
        Console.WriteLine(" 8. Все режимы последовательно");
        Console.WriteLine(" 9. Изменить режим вывода");
        Console.WriteLine(" 0. Выход");
        Console.Write("\nВаш выбор: ");

        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                RunFullBenchmark();
                break;
            case "2":
                RunSerializationOnly();
                break;
            case "3":
                RunDeserializationOnly();
                break;
            case "4":
                RunSizeOnly();
                break;
            case "5":
                RunListBenchmark();
                break;
            case "6":
                RunLongNameBenchmark();
                break;
            case "7":
                RunRoundtripBenchmark();
                break;
            case "8":
                RunAllModes();
                break;
            case "9":
                ChangeOutputMode();
                break;
            case "0":
                Console.WriteLine("\nДо свидания!");
                return;
            default:
                Console.WriteLine("\n❌ Неверный выбор. Запускаем полный бенчмарк...");
                RunFullBenchmark();
                break;
        }

        Console.WriteLine("\n\nНажмите любую клавишу для возврата в меню...");
        Console.ReadKey();
        Console.Clear();
        ShowMenu();
    }

    static void ChangeOutputMode()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   НАСТРОЙКА РЕЖИМА ВЫВОДА                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.WriteLine($"1. Полный режим (verbose)     [{(ShowVerboseInfo ? "✅" : " ")}]");
        Console.WriteLine($"2. Компактный режим (minimal) [{(ShowVerboseInfo ? " " : ShowSystemInfo ? "✅" : " ")}]");
        Console.WriteLine($"3. Минимальный режим (quiet)  [{(ShowVerboseInfo ? " " : ShowSystemInfo ? " " : "✅")}]");
        Console.WriteLine($"4. Экспорт в CSV              [{(ExportResultsToFile ? "✅" : " ")}]");
        Console.WriteLine($"5. Показать системную инфо    [{(ShowSystemInfo ? "✅" : " ")}]");
        Console.WriteLine($"6. Показать анализ            [{(ShowAnalysis ? "✅" : " ")}]");
        Console.WriteLine($"7. Показать рекомендации      [{(ShowRecommendations ? "✅" : " ")}]");
        Console.WriteLine($"8. Показывать вывод бенчмарка [{(ShowBenchmarkOutput ? "✅" : " ")}]");
        Console.WriteLine($"9. Сбросить настройки");
        Console.WriteLine("0. Назад");
        Console.Write("\nВаш выбор: ");

        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                ShowVerboseInfo = true;
                ShowSystemInfo = true;
                ShowAnalysis = true;
                ShowRecommendations = true;
                ExportResultsToFile = true;
                Console.WriteLine("\n✅ Установлен полный режим");
                break;
            case "2":
                ShowVerboseInfo = false;
                ShowSystemInfo = true;
                ShowAnalysis = true;
                ShowRecommendations = true;
                ExportResultsToFile = true;
                Console.WriteLine("\n✅ Установлен компактный режим");
                break;
            case "3":
                ShowVerboseInfo = false;
                ShowSystemInfo = false;
                ShowAnalysis = false;
                ShowRecommendations = false;
                ExportResultsToFile = false;
                Console.WriteLine("\n✅ Установлен минимальный режим");
                break;
            case "4":
                ExportResultsToFile = !ExportResultsToFile;
                Console.WriteLine($"\n✅ Экспорт в CSV: {(ExportResultsToFile ? "Включен" : "Выключен")}");
                break;
            case "5":
                ShowSystemInfo = !ShowSystemInfo;
                Console.WriteLine($"\n✅ Системная информация: {(ShowSystemInfo ? "Показывать" : "Скрыта")}");
                break;
            case "6":
                ShowAnalysis = !ShowAnalysis;
                Console.WriteLine($"\n✅ Анализ: {(ShowAnalysis ? "Показывать" : "Скрыт")}");
                break;
            case "7":
                ShowRecommendations = !ShowRecommendations;
                Console.WriteLine($"\n✅ Рекомендации: {(ShowRecommendations ? "Показывать" : "Скрыты")}");
                break;
            case "8":
                ShowBenchmarkOutput = !ShowBenchmarkOutput;
                Console.WriteLine($"\n✅ Вывод бенчмарка: {(ShowBenchmarkOutput ? "Показывать" : "Скрыт")}");
                break;
            case "9":
                ShowVerboseInfo = true;
                ShowSystemInfo = true;
                ShowAnalysis = true;
                ShowRecommendations = true;
                ExportResultsToFile = true;
                ShowBenchmarkOutput = false;
                Console.WriteLine("\n✅ Настройки сброшены на полный режим");
                break;
            case "0":
                return;
            default:
                Console.WriteLine("\n❌ Неверный выбор");
                break;
        }

        Console.WriteLine("\nНажмите любую клавишу для продолжения...");
        Console.ReadKey();
        ChangeOutputMode();
    }

    static Summary RunBenchmarkSafely<TBenchmark>(IConfig config) where TBenchmark : class
    {
        if (!ShowBenchmarkOutput)
        {
            using (var suppressor = new OutputSuppressor())
            {
                return BenchmarkRunner.Run<TBenchmark>(config);
            }
        }
        else
        {
            return BenchmarkRunner.Run<TBenchmark>(config);
        }
    }

    static void RunFullBenchmark()
    {
        Console.WriteLine("\n🚀 Запуск полного бенчмарка...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintSummary(summary);
        if (ExportResultsToFile) ExportResults(summary);
    }

    static void RunSerializationOnly()
    {
        Console.WriteLine("\n🚀 Запуск бенчмарка сериализации...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintSummary(summary);
        if (ExportResultsToFile) ExportResults(summary);
    }

    static void RunDeserializationOnly()
    {
        Console.WriteLine("\n🚀 Запуск бенчмарка десериализации...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintSummary(summary);
        if (ExportResultsToFile) ExportResults(summary);
    }

    static void RunSizeOnly()
    {
        Console.WriteLine("\n🚀 Запуск бенчмарка размера данных...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintSizeComparison(summary);
    }

    static void RunListBenchmark()
    {
        Console.WriteLine("\n🚀 Запуск бенчмарка для списков...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintSummary(summary);
        if (ExportResultsToFile) ExportResults(summary);
    }

    static void RunLongNameBenchmark()
    {
        Console.WriteLine("\n🚀 Запуск бенчмарка с разной длиной имени...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintLongNameComparison(summary);
    }

    static void RunRoundtripBenchmark()
    {
        Console.WriteLine("\n🚀 Запуск бенчмарка Roundtrip...\n");
        var config = CreateConfig();
        var summary = RunBenchmarkSafely<UserProfileSerializationBenchmark>(config);
        PrintSummary(summary);
        if (ExportResultsToFile) ExportResults(summary);
    }

    static void RunAllModes()
    {
        Console.WriteLine("\n🚀 Запуск всех режимов бенчмарка...\n");

        Console.WriteLine("\n1. Полный бенчмарк");
        RunFullBenchmark();

        Console.WriteLine("\n2. Бенчмарк списков");
        RunListBenchmark();

        Console.WriteLine("\n3. Бенчмарк с разной длиной имени");
        RunLongNameBenchmark();

        Console.WriteLine("\n4. Бенчмарк Roundtrip");
        RunRoundtripBenchmark();
    }

    static IConfig CreateConfig()
    {
        return ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithOptions(ConfigOptions.JoinSummary)
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.StdDev)
            .AddColumn(StatisticColumn.Median)
            .AddColumn(StatisticColumn.Min)
            .AddColumn(StatisticColumn.Max)
            .AddDiagnoser(MemoryDiagnoser.Default);
    }

    static void PrintSystemInfo(Summary summary, StreamWriter writer = null)
    {
        if(writer != null)
        {
            try
            {
                writer.WriteLine("\n" + new string('═', 110));
                writer.WriteLine("                    РЕЗУЛЬТАТЫ БЕНЧМАРКА");
                writer.WriteLine(new string('═', 110));

                writer.WriteLine($"\n📊 Системная информация:");

                // ОС
                writer.WriteLine($"  • ОС: {RuntimeInformation.OSDescription}");

                // Процессор
                var processorName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                if (!string.IsNullOrEmpty(processorName))
                {
                    writer.WriteLine($"  • Процессор: {processorName}");
                }
                else
                {
                    writer.WriteLine($"  • Процессор: {RuntimeInformation.ProcessArchitecture}");
                }

                // Ядра
                writer.WriteLine($"  • Ядер: {Environment.ProcessorCount}");

                // Архитектура
                writer.WriteLine($"  • Архитектура: {RuntimeInformation.ProcessArchitecture}");

                // .NET версия
                writer.WriteLine($"  • .NET: {Environment.Version}");

                // Runtime
                writer.WriteLine($"  • Runtime: {RuntimeInformation.FrameworkDescription}");

                // Дополнительная информация из BenchmarkDotNet
                try
                {
                    writer.WriteLine($"  • BenchmarkDotNet: {summary.HostEnvironmentInfo.RuntimeVersion}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"  ⚠️ Не удалось получить системную информацию: {ex.Message}");
            }


            return;
        }

        try
        {
            Console.WriteLine("\n" + new string('═', 110));
            Console.WriteLine("                    РЕЗУЛЬТАТЫ БЕНЧМАРКА");
            Console.WriteLine(new string('═', 110));

            Console.WriteLine($"\n📊 Системная информация:");

            // ОС
            Console.WriteLine($"  • ОС: {RuntimeInformation.OSDescription}");

            // Процессор
            var processorName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            if (!string.IsNullOrEmpty(processorName))
            {
                Console.WriteLine($"  • Процессор: {processorName}");
            }
            else
            {
                Console.WriteLine($"  • Процессор: {RuntimeInformation.ProcessArchitecture}");
            }

            // Ядра
            Console.WriteLine($"  • Ядер: {Environment.ProcessorCount}");

            // Архитектура
            Console.WriteLine($"  • Архитектура: {RuntimeInformation.ProcessArchitecture}");

            // .NET версия
            Console.WriteLine($"  • .NET: {Environment.Version}");

            // Runtime
            Console.WriteLine($"  • Runtime: {RuntimeInformation.FrameworkDescription}");

            // Дополнительная информация из BenchmarkDotNet
            try
            {
                Console.WriteLine($"  • BenchmarkDotNet: {summary.HostEnvironmentInfo.RuntimeVersion}");
            }
            catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️ Не удалось получить системную информацию: {ex.Message}");
        }
    }

    // Вспомогательный метод для проверки Baseline
    static bool IsBaseline(BenchmarkReport report)
    {
        try
        {
            // Пробуем получить свойство Baseline через рефлексию
            var method = report.BenchmarkCase.Descriptor.WorkloadMethod;
            var baselineProperty = method.GetType().GetProperty("Baseline");
            if (baselineProperty != null)
            {
                return (bool)baselineProperty.GetValue(method);
            }

            // Альтернативный способ через Attributes
            var attributes = method.GetCustomAttributes(false);
            foreach (var attr in attributes)
            {
                var attrType = attr.GetType();
                if (attrType.Name == "BenchmarkAttribute")
                {
                    var baselineProp = attrType.GetProperty("Baseline");
                    if (baselineProp != null)
                    {
                        return (bool)baselineProp.GetValue(attr);
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    static void PrintSummary(Summary summary)
    {
        if (summary == null || !summary.Reports.Any())
        {
            Console.WriteLine("❌ Нет данных для отображения");
            return;
        }

        // Вывод системной информации (только если включено)
        if (ShowSystemInfo)
        {
            PrintSystemInfo(summary);
        }
        else if (ShowVerboseInfo)
        {
            // В полном режиме показываем только заголовок без системной информации
            Console.WriteLine("\n" + new string('═', 110));
            Console.WriteLine("                    РЕЗУЛЬТАТЫ БЕНЧМАРКА");
            Console.WriteLine(new string('═', 110));
        }

        Console.WriteLine("\n" + new string('─', 110));
        Console.WriteLine("РЕЗУЛЬТАТЫ ТЕСТОВ:");
        Console.WriteLine(new string('─', 110));

        // В минимальном режиме показываем только самые важные колонки
        if (!ShowVerboseInfo && !ShowSystemInfo)
        {
            Console.WriteLine($"{"Метод",-50} {"Среднее",-15} {"Память",-15}");
            Console.WriteLine(new string('─', 110));
        }
        else
        {
            Console.WriteLine($"{"Метод",-50} {"Среднее",-15} {"Ошибка",-15} {"StdDev",-15} {"Gen0",-12} {"Память",-12} {"⭐"}");
            Console.WriteLine(new string('─', 110));
        }

        var reports = summary.Reports
            .Where(r => r.ResultStatistics != null)
            .OrderBy(r => r.ResultStatistics.Mean)
            .ToList();

        foreach (var report in reports)
        {
            var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            var stats = report.ResultStatistics;
            var gcStats = report.GcStats;
            var gen0Collections = gcStats.Gen0Collections;

            long bytesAllocated = 0;
            if (report.Metrics.TryGetValue("BytesAllocatedPerOperation", out var metric))
            {
                bytesAllocated = (long)metric.Value;
            }

            // Исправленный способ проверки Baseline
            bool isBaseline = false;
            try
            {
                // Пробуем получить Baseline из атрибута
                var method = report.BenchmarkCase.Descriptor.WorkloadMethod;
                var attr = method.GetCustomAttributes(false)
                    .FirstOrDefault(a => a.GetType().Name == "BenchmarkAttribute");
                if (attr != null)
                {
                    var baselineProp = attr.GetType().GetProperty("Baseline");
                    if (baselineProp != null)
                    {
                        isBaseline = (bool)baselineProp.GetValue(attr);
                    }
                }
            }
            catch { }

            var baselineMarker = isBaseline ? "⭐" : " ";

            if (!ShowVerboseInfo && !ShowSystemInfo)
            {
                // Минимальный режим
                Console.WriteLine($"{methodName,-50} " +
                    $"{stats.Mean,15:F4} ms " +
                    $"{bytesAllocated,15:F0} B");
            }
            else
            {
                // Полный или компактный режим
                Console.WriteLine($"{methodName,-50} " +
                    $"{stats.Mean,15:F4} ms " +
                    $"{stats.StandardError,15:F4} ms " +
                    $"{stats.StandardDeviation,15:F4} ms " +
                    $"{gen0Collections,12:F2} " +
                    $"{bytesAllocated,12:F0} B " +
                    $"{baselineMarker,2}");
            }
        }

        // Анализ и рекомендации (только если включены)
        if (ShowAnalysis)
        {
            PrintAnalysis(reports);
        }
        else if (ShowRecommendations)
        {
            // Если анализ выключен, но рекомендации включены - показываем только рекомендации
            PrintRecommendationsOnly(reports);
        }
    }

    static void PrintRecommendationsOnly(List<BenchmarkReport> reports,StreamWriter writer = null)
    {
        if (!reports.Any()) return;

        if (writer != null)
        {
            writer.WriteLine("\n" + new string('─', 110));
            writer.WriteLine("РЕКОМЕНДАЦИИ:");
            writer.WriteLine(new string('─', 110));

    

            var bestMethod = reports.OrderBy(r => r.ResultStatistics.Mean)
                .ThenBy(r => GetAllocatedBytes(r))
                .First();

            writer.WriteLine($"\n✅ Рекомендуемый метод: {bestMethod.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            writer.WriteLine($"   • Время: {bestMethod.ResultStatistics.Mean:F4} ms");
            writer.WriteLine($"   • Память: {GetAllocatedBytes(bestMethod):F0} B");
        }
        else
        {


            Console.WriteLine("\n" + new string('─', 110));
            Console.WriteLine("РЕКОМЕНДАЦИИ:");
            Console.WriteLine(new string('─', 110));

            var bestMethod = reports.OrderBy(r => r.ResultStatistics.Mean)
                .ThenBy(r => GetAllocatedBytes(r))
                .First();

            Console.WriteLine($"\n✅ Рекомендуемый метод: {bestMethod.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            Console.WriteLine($"   • Время: {bestMethod.ResultStatistics.Mean:F4} ms");
            Console.WriteLine($"   • Память: {GetAllocatedBytes(bestMethod):F0} B");
        }

        long GetAllocatedBytes(BenchmarkReport report)
        {
            if (report.Metrics.TryGetValue("BytesAllocatedPerOperation", out var metric))
            {
                return (long)metric.Value;
            }
            return 0;
        }
    }

    static void PrintAnalysis(List<BenchmarkReport> reports, StreamWriter writer = null)
    {
        if (!reports.Any()) return;

        if (writer != null)
        {
            writer.WriteLine("\n" + new string('─', 110));
            writer.WriteLine("АНАЛИЗ РЕЗУЛЬТАТОВ:");
            writer.WriteLine(new string('─', 110));

            var fastest = reports.First();
            var slowest = reports.Last();
            var leastMemory = reports.OrderBy(r => GetAllocatedBytes(r)).First();
            var mostMemory = reports.OrderByDescending(r => GetAllocatedBytes(r)).First();

            writer.WriteLine($"\n🏆 Самый быстрый: {fastest.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            writer.WriteLine($"   Время: {fastest.ResultStatistics.Mean:F4} ms");
            writer.WriteLine($"   Память: {GetAllocatedBytes(fastest):F0} B");

            writer.WriteLine($"\n🐢 Самый медленный: {slowest.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            writer.WriteLine($"   Время: {slowest.ResultStatistics.Mean:F4} ms");
            writer.WriteLine($"   Память: {GetAllocatedBytes(slowest):F0} B");

            writer.WriteLine($"\n💾 Минимум памяти: {leastMemory.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            writer.WriteLine($"   Память: {GetAllocatedBytes(leastMemory):F0} B");

            writer.WriteLine($"\n💸 Максимум памяти: {mostMemory.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            writer.WriteLine($"   Память: {GetAllocatedBytes(mostMemory):F0} B");

            // Сравнение JSON vs Binary
            var jsonReports = reports.Where(r => r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("Json"));
            var binaryReports = reports.Where(r => r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("Binary"));

            if (jsonReports.Any() && binaryReports.Any())
            {
                writer.WriteLine("\n📈 Сравнение JSON vs Binary:");

                foreach (var json in jsonReports)
                {
                    var jsonName = json.BenchmarkCase.Descriptor.WorkloadMethod.Name;
                    var binaryName = jsonName.Replace("Json", "Binary");

                    var binary = binaryReports.FirstOrDefault(b =>
                        b.BenchmarkCase.Descriptor.WorkloadMethod.Name == binaryName ||
                        b.BenchmarkCase.Descriptor.WorkloadMethod.Name.Replace("Binary", "Json") == jsonName);

                    if (binary != null && json.ResultStatistics != null && binary.ResultStatistics != null)
                    {
                        var speedup = binary.ResultStatistics.Mean / json.ResultStatistics.Mean;
                        var jsonMemory = GetAllocatedBytes(json);
                        var binaryMemory = GetAllocatedBytes(binary);
                        var memoryRatio = jsonMemory > 0 && binaryMemory > 0 ? (double)jsonMemory / binaryMemory : 0;

                        writer.WriteLine($"\n  📌 {jsonName}");
                        writer.WriteLine($"     ⚡ Бинарная сериализация быстрее в {speedup:F2}x раз");
                        if (memoryRatio > 0)
                        {
                            writer.WriteLine($"     💾 JSON использует в {memoryRatio:F2}x больше памяти");
                        }
                    }
                }
            }

            // Вывод рекомендаций (только если включены)
            if (ShowRecommendations)
            {
                writer.WriteLine("\n" + new string('─', 110));
                writer.WriteLine("РЕКОМЕНДАЦИИ:");
                writer.WriteLine(new string('─', 110));

                var bestMethod = reports.OrderBy(r => r.ResultStatistics.Mean)
                    .ThenBy(r => GetAllocatedBytes(r))
                    .First();

                var worstMethod = reports.OrderByDescending(r => r.ResultStatistics.Mean)
                    .ThenByDescending(r => GetAllocatedBytes(r))
                    .First();

                writer.WriteLine($"\n✅ Рекомендуемый метод: {bestMethod.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
                writer.WriteLine($"   • Время: {bestMethod.ResultStatistics.Mean:F4} ms");
                writer.WriteLine($"   • Память: {GetAllocatedBytes(bestMethod):F0} B");

                writer.WriteLine($"\n⚠️ Наименее эффективный метод: {worstMethod.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
                writer.WriteLine($"   • Время: {worstMethod.ResultStatistics.Mean:F4} ms");
                writer.WriteLine($"   • Память: {GetAllocatedBytes(worstMethod):F0} B");
            }


            return;
        }
        else
        {


            Console.WriteLine("\n" + new string('─', 110));
            Console.WriteLine("АНАЛИЗ РЕЗУЛЬТАТОВ:");
            Console.WriteLine(new string('─', 110));

            

            var fastest = reports.First();
            var slowest = reports.Last();
            var leastMemory = reports.OrderBy(r => GetAllocatedBytes(r)).First();
            var mostMemory = reports.OrderByDescending(r => GetAllocatedBytes(r)).First();

            Console.WriteLine($"\n🏆 Самый быстрый: {fastest.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            Console.WriteLine($"   Время: {fastest.ResultStatistics.Mean:F4} ms");
            Console.WriteLine($"   Память: {GetAllocatedBytes(fastest):F0} B");

            Console.WriteLine($"\n🐢 Самый медленный: {slowest.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            Console.WriteLine($"   Время: {slowest.ResultStatistics.Mean:F4} ms");
            Console.WriteLine($"   Память: {GetAllocatedBytes(slowest):F0} B");

            Console.WriteLine($"\n💾 Минимум памяти: {leastMemory.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            Console.WriteLine($"   Память: {GetAllocatedBytes(leastMemory):F0} B");

            Console.WriteLine($"\n💸 Максимум памяти: {mostMemory.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
            Console.WriteLine($"   Память: {GetAllocatedBytes(mostMemory):F0} B");

            // Сравнение JSON vs Binary
            var jsonReports = reports.Where(r => r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("Json"));
            var binaryReports = reports.Where(r => r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("Binary"));

            if (jsonReports.Any() && binaryReports.Any())
            {
                Console.WriteLine("\n📈 Сравнение JSON vs Binary:");

                foreach (var json in jsonReports)
                {
                    var jsonName = json.BenchmarkCase.Descriptor.WorkloadMethod.Name;
                    var binaryName = jsonName.Replace("Json", "Binary");

                    var binary = binaryReports.FirstOrDefault(b =>
                        b.BenchmarkCase.Descriptor.WorkloadMethod.Name == binaryName ||
                        b.BenchmarkCase.Descriptor.WorkloadMethod.Name.Replace("Binary", "Json") == jsonName);

                    if (binary != null && json.ResultStatistics != null && binary.ResultStatistics != null)
                    {
                        var speedup = binary.ResultStatistics.Mean / json.ResultStatistics.Mean;
                        var jsonMemory = GetAllocatedBytes(json);
                        var binaryMemory = GetAllocatedBytes(binary);
                        var memoryRatio = jsonMemory > 0 && binaryMemory > 0 ? (double)jsonMemory / binaryMemory : 0;

                        Console.WriteLine($"\n  📌 {jsonName}");
                        Console.WriteLine($"     ⚡ Бинарная сериализация быстрее в {speedup:F2}x раз");
                        if (memoryRatio > 0)
                        {
                            Console.WriteLine($"     💾 JSON использует в {memoryRatio:F2}x больше памяти");
                        }
                    }
                }
            }

            // Вывод рекомендаций (только если включены)
            if (ShowRecommendations)
            {
                Console.WriteLine("\n" + new string('─', 110));
                Console.WriteLine("РЕКОМЕНДАЦИИ:");
                Console.WriteLine(new string('─', 110));

                var bestMethod = reports.OrderBy(r => r.ResultStatistics.Mean)
                    .ThenBy(r => GetAllocatedBytes(r))
                    .First();

                var worstMethod = reports.OrderByDescending(r => r.ResultStatistics.Mean)
                    .ThenByDescending(r => GetAllocatedBytes(r))
                    .First();

                Console.WriteLine($"\n✅ Рекомендуемый метод: {bestMethod.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
                Console.WriteLine($"   • Время: {bestMethod.ResultStatistics.Mean:F4} ms");
                Console.WriteLine($"   • Память: {GetAllocatedBytes(bestMethod):F0} B");

                Console.WriteLine($"\n⚠️ Наименее эффективный метод: {worstMethod.BenchmarkCase.Descriptor.WorkloadMethod.Name}");
                Console.WriteLine($"   • Время: {worstMethod.ResultStatistics.Mean:F4} ms");
                Console.WriteLine($"   • Память: {GetAllocatedBytes(worstMethod):F0} B");
            }
        }

        long GetAllocatedBytes(BenchmarkReport report)
        {
            if (report.Metrics.TryGetValue("BytesAllocatedPerOperation", out var metric))
            {
                return (long)metric.Value;
            }
            return 0;
        }
    }

    static void PrintSizeComparison(Summary summary)
    {
        if (summary == null || !summary.Reports.Any())
        {
            Console.WriteLine("❌ Нет данных для отображения");
            return;
        }

        Console.WriteLine("\n" + new string('═', 80));
        Console.WriteLine("           СРАВНЕНИЕ РАЗМЕРА ДАННЫХ");
        Console.WriteLine(new string('═', 80));

        var reports = summary.Reports
            .Where(r => r.ResultStatistics != null)
            .OrderBy(r => r.ResultStatistics.Mean);

        Console.WriteLine($"{"Метод",-50} {"Размер",-15} {"Среднее время",-15}");
        Console.WriteLine(new string('─', 80));

        foreach (var report in reports)
        {
            var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            var stats = report.ResultStatistics;

            Console.WriteLine($"{methodName,-50} " +
                $"{stats.Mean,15:F0} B " +
                $"{stats.Mean,15:F4} ms");
        }
    }

    static void PrintLongNameComparison(Summary summary)
    {
        if (summary == null || !summary.Reports.Any())
        {
            Console.WriteLine("❌ Нет данных для отображения");
            return;
        }

        Console.WriteLine("\n" + new string('═', 90));
        Console.WriteLine("           ВЛИЯНИЕ ДЛИНЫ ИМЕНИ НА РАЗМЕР");
        Console.WriteLine(new string('═', 90));

        var reports = summary.Reports
            .Where(r => r.ResultStatistics != null)
            .OrderBy(r =>
            {
                var param = r.BenchmarkCase.Parameters;
                if (param != null && param.Items.Any(p => p.Name == "NameLength"))
                {
                    return (int)param.Items.First(p => p.Name == "NameLength").Value;
                }
                return 0;
            });

        Console.WriteLine($"{"Метод",-45} {"Длина",-10} {"Размер",-15} {"Время",-15}");
        Console.WriteLine(new string('─', 90));

        foreach (var report in reports)
        {
            var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;

            int length = 0;
            var param = report.BenchmarkCase.Parameters;
            if (param != null && param.Items.Any(p => p.Name == "NameLength"))
            {
                length = (int)param.Items.First(p => p.Name == "NameLength").Value;
            }

            var stats = report.ResultStatistics;

            Console.WriteLine($"{methodName,-45} " +
                $"{length,10} " +
                $"{stats.Mean,15:F0} B " +
                $"{stats.Mean,15:F4} ms");
        }
    }

    static void ExportResults(Summary summary)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = $"benchmark_results_{timestamp}.csv";

            using var writer = new StreamWriter(fileName);
            if (summary == null || !summary.Reports.Any())
            {
                writer.WriteLine("❌ Нет данных для отображения");
                return;
            }

            // Вывод системной информации (только если включено)
            if (ShowSystemInfo)
            {
                PrintSystemInfo(summary,writer);
            }
            else if (ShowVerboseInfo)
            {
                // В полном режиме показываем только заголовок без системной информации
                writer.WriteLine("\n" + new string('═', 110));
                writer.WriteLine("                    РЕЗУЛЬТАТЫ БЕНЧМАРКА");
                writer.WriteLine(new string('═', 110));
            }

            writer.WriteLine("\n" + new string('─', 110));
            writer.WriteLine("РЕЗУЛЬТАТЫ ТЕСТОВ:");
            writer.WriteLine(new string('─', 110));

            // В минимальном режиме показываем только самые важные колонки
            if (!ShowVerboseInfo && !ShowSystemInfo)
            {
                writer.WriteLine($"{"Метод",-50} {"Среднее",-15} {"Память",-15}");
                writer.WriteLine(new string('─', 110));
            }
            else
            {
                writer.WriteLine($"{"Метод",-50} {"Среднее",-15} {"Ошибка",-15} {"StdDev",-15} {"Gen0",-12} {"Память",-12} {"⭐"}");
                writer.WriteLine(new string('─', 110));
            }

            var reports = summary.Reports
                .Where(r => r.ResultStatistics != null)
                .OrderBy(r => r.ResultStatistics.Mean)
                .ToList();

            foreach (var report in reports)
            {
                var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
                var stats = report.ResultStatistics;
                var gcStats = report.GcStats;
                var gen0Collections = gcStats.Gen0Collections;

                long bytesAllocated = 0;
                if (report.Metrics.TryGetValue("BytesAllocatedPerOperation", out var metric))
                {
                    bytesAllocated = (long)metric.Value;
                }

                // Исправленный способ проверки Baseline
                bool isBaseline = false;
                try
                {
                    // Пробуем получить Baseline из атрибута
                    var method = report.BenchmarkCase.Descriptor.WorkloadMethod;
                    var attr = method.GetCustomAttributes(false)
                        .FirstOrDefault(a => a.GetType().Name == "BenchmarkAttribute");
                    if (attr != null)
                    {
                        var baselineProp = attr.GetType().GetProperty("Baseline");
                        if (baselineProp != null)
                        {
                            isBaseline = (bool)baselineProp.GetValue(attr);
                        }
                    }
                }
                catch { }

                var baselineMarker = isBaseline ? "⭐" : " ";

                if (!ShowVerboseInfo && !ShowSystemInfo)
                {
                    // Минимальный режим
                    writer.WriteLine($"{methodName,-50} " +
                        $"{stats.Mean,15:F4} ms " +
                        $"{bytesAllocated,15:F0} B");
                }
                else
                {
                    // Полный или компактный режим
                    writer.WriteLine($"{methodName,-50} " +
                        $"{stats.Mean,15:F4} ms " +
                        $"{stats.StandardError,15:F4} ms " +
                        $"{stats.StandardDeviation,15:F4} ms " +
                        $"{gen0Collections,12:F2} " +
                        $"{bytesAllocated,12:F0} B " +
                        $"{baselineMarker,2}");
                }
            }

            // Анализ и рекомендации (только если включены)
            if (ShowAnalysis)
            {
                PrintAnalysis(reports,writer);
            }
            else if (ShowRecommendations)
            {
                // Если анализ выключен, но рекомендации включены - показываем только рекомендации
                PrintRecommendationsOnly(reports,writer);
            }




            Console.WriteLine($"\n📁 Результаты сохранены в файл: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n⚠️ Не удалось сохранить результаты: {ex.Message}");
        }
    }
}