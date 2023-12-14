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
    private readonly int maxInt = 50000000;
    private readonly int maxDouble = 20;

    public FileGenerator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void GenerateFiles(int fileCount, int linesPerFile, string fileDirectory)
    {

        try
        {
            if (!Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

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

    private void GenerateFile(string filePath, int lines)
    {
        try
        {
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

    private string GenerateDataLine(DataModel data)
    {
        return $"{data.Date}||{data.LatinChars}||{data.RussianChars}||{data.EvenInt}||{data.FloatNumber:F8}||";
    }

    private string GenerateRandomDateTicks()
    {
        DateTimeOffset startDate = DateTimeOffset.Now.AddYears(-5);
        int range = (int)(DateTimeOffset.Now - startDate).TotalDays;
        DateTimeOffset randomDate = startDate.AddDays(threadLocalRandom.Value.Next(range));
        return randomDate.ToString("dd.MM.yyyy");
    }

    private string GenerateRandomString(string chars, int length)
    {
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[threadLocalRandom.Value.Next(s.Length)]).ToArray());
    }

    private int GenerateRandomEvenInt()
    {
        return threadLocalRandom.Value.Next(1, maxInt) * 2;
    }

    private double GenerateRandomFloatNumber()
    {
        double multiplier = maxDouble - 1;
        return threadLocalRandom.Value.NextDouble() * multiplier + 1;
    }
}
