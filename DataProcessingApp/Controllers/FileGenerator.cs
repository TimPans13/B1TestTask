using DataProcessingApp.Models;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class FileGenerator
{
    private static readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random());
    private readonly ILogger _logger;
    private readonly int stringLength = 10;
    private readonly int maxInt = 50000000;  // Половина от максимума так как числа дрожны быть чётными
    private readonly int maxDouble = 20;     // Максимальное значение для генерации чисел с плавающей запятой

    public FileGenerator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Метод для генерации файлов
    /// <param name="fileCount">Количество файлов для создания</param>
    /// <param name="linesPerFile">Количество строк в каждом файле</param>
    /// <param name="fileDirectory">Директория для сохранения файлов</param>
    public void GenerateFiles(int fileCount, int linesPerFile, string fileDirectory)
    {
        try
        {
            if (!Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            // Параллельная генерация файлов
            Parallel.ForEach(Enumerable.Range(1, fileCount), i =>
            {
                string filePath = Path.Combine(fileDirectory, $"file{i}.txt");
                GenerateFile(filePath, linesPerFile);
            });

            _logger.Information($"{fileCount} файлов успешно создано.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка при создании файлов: {ex.Message}");
        }
    }

    // Метод для генерации содержимого файла
    /// <param name="filePath">Путь к файлу</param>
    /// <param name="lines">Количество строк в файле</param>
    private void GenerateFile(string filePath, int lines)
    {
        try
        {
            // Запись в файл с использованием параллельной обработки строк
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                Parallel.For(0, lines, i =>
                {
                    DataModel data = GenerateRandomData();
                    string line = GenerateDataLine(data);
                    lock (writer)
                    {
                        writer.WriteLine(line);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка при создании файла {filePath}: {ex.Message}");
        }
        Console.WriteLine("File created");
    }

    // Метод для генерации случайных данных
    private DataModel GenerateRandomData()
    {
        return new DataModel
        {
            Date = GenerateRandomDateTicks(),
            LatinChars = GenerateRandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", stringLength),
            RussianChars = GenerateRandomString("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя", stringLength),
            EvenInt = GenerateRandomEvenInt(),
            FloatNumber = GenerateRandomFloatNumber()
        };
    }

    // Метод для генерации строки данных
    private string GenerateDataLine(DataModel data)
    {
        return $"{data.Date}||{data.LatinChars}||{data.RussianChars}||{data.EvenInt}||{data.FloatNumber:F8}||";
    }

    // Метод для генерации случайной даты
    private string GenerateRandomDateTicks()
    {
        DateTimeOffset startDate = DateTimeOffset.Now.AddYears(-5);
        int range = (int)(DateTimeOffset.Now - startDate).TotalDays;
        DateTimeOffset randomDate = startDate.AddDays(threadLocalRandom.Value.Next(range));
        return randomDate.ToString("dd.MM.yyyy");
    }

    // Метод для генерации случайной строки
    private string GenerateRandomString(string chars, int length)
    {
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[threadLocalRandom.Value.Next(s.Length)]).ToArray());
    }

    // Метод для генерации случайного четного числа
    private int GenerateRandomEvenInt()
    {
        return threadLocalRandom.Value.Next(1, maxInt) * 2;
    }

    // Метод для генерации случайного числа с плавающей запятой
    private double GenerateRandomFloatNumber()
    {
        double multiplier = maxDouble - 1;
        return threadLocalRandom.Value.NextDouble() * multiplier + 1;
    }
}
