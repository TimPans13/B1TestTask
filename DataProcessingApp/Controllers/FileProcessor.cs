using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DataProcessingApp.Controllers
{
    public class FileProcessor
    {
        private readonly ILogger _logger;
        private readonly object _fileLock = new object();

        public FileProcessor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Метод для обработки файлов
        /// <param name="inputDirectory">Директория с входными файлами</param>
        /// <param name="outputFilePath">Путь к файлу, в который будет сохранен результат</param>
        /// <param name="stringToRemove">Строка, которую необходимо удалить из файлов</param>
        public async Task ProcessFilesAsync(string inputDirectory, string outputFilePath, string stringToRemove)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
                {
                    throw new Exception("Incorrect path");
                }

                string[] inputFiles = Directory.GetFiles(inputDirectory, "*.txt");

                // Параллельная обработка входных файлов
                var mergeTasks = inputFiles.Select(file => ProcessFileAsync(file, stringToRemove, outputFilePath)).ToArray();
                await Task.WhenAll(mergeTasks);

                _logger.Information($"Files processed successfully. Merged content saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing files: {ex.Message}");
            }
        }

        // Метод для обработки отдельного файла
        /// <param name="inputFilePath">Путь к входному файлу</param>
        /// <param name="stringToRemove">Строка, которую необходимо удалить</param>
        /// <param name="outputFilePath">Путь к файлу для сохранения результата</param>
        private async Task ProcessFileAsync(string inputFilePath, string stringToRemove, string outputFilePath)
        {
            try
            {
                string[] lines = await File.ReadAllLinesAsync(inputFilePath);

                // Удаление строк, содержащих указанную строку
                int removedLinesCount = RemoveLinesContainingString(lines, stringToRemove);

                lock (_fileLock)
                {
                    // Добавление непустых строк в выходной файл
                    AppendLinesToFile(lines.Where(line => !string.IsNullOrWhiteSpace(line)), outputFilePath);
                }

                _logger.Information($"File processed: {inputFilePath}. {removedLinesCount} lines containing '{stringToRemove}' removed.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing file {inputFilePath}: {ex.Message}");
            }
        }

        // Метод для удаления строк, содержащих указанную строку
        private int RemoveLinesContainingString(string[] lines, string stringToRemove)
        {
            int count = 0;

            // Параллельный цикл для эффективного удаления строк
            Parallel.For(0, lines.Length, i =>
            {
                if (lines[i].Contains(stringToRemove, StringComparison.Ordinal))
                {
                    lines[i] = string.Empty;
                    Interlocked.Increment(ref count);
                }
            });

            return count;
        }

        // Метод для добавления строк в файл
        private void AppendLinesToFile(IEnumerable<string> lines, string filePath)
        {
            var stringBuilder = new StringBuilder();

            // Формирование строки из коллекции и добавление в файл
            foreach (var line in lines)
            {
                stringBuilder.AppendLine(line);
            }

            File.AppendAllText(filePath, stringBuilder.ToString());
        }
    }
}
