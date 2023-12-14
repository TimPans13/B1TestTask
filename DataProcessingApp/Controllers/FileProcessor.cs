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

        public async Task ProcessFilesAsync(string inputDirectory, string outputFilePath, string stringToRemove)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
                {
                    throw new Exception("Incorrect path");
                }

                string[] inputFiles = Directory.GetFiles(inputDirectory, "*.txt");

                var mergeTasks = inputFiles.Select(file => ProcessFileAsync(file, stringToRemove, outputFilePath)).ToArray();
                await Task.WhenAll(mergeTasks);

                _logger.Information($"Files processed successfully. Merged content saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing files: {ex.Message}");
            }
        }

        private async Task ProcessFileAsync(string inputFilePath, string stringToRemove, string outputFilePath)
        {
            try
            {
                string[] lines = await File.ReadAllLinesAsync(inputFilePath);

                int removedLinesCount = RemoveLinesContainingString(lines, stringToRemove);

                lock (_fileLock)
                {
                    AppendLinesToFile(lines.Where(line => !string.IsNullOrWhiteSpace(line)), outputFilePath);
                }

                _logger.Information($"File processed: {inputFilePath}. {removedLinesCount} lines containing '{stringToRemove}' removed.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing file {inputFilePath}: {ex.Message}");
            }
        }

        private int RemoveLinesContainingString(string[] lines, string stringToRemove)
        {
            int count = 0;

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

        private void AppendLinesToFile(IEnumerable<string> lines, string filePath)
        {
            var stringBuilder = new StringBuilder();
            foreach (var line in lines)
            {
                stringBuilder.AppendLine(line);
            }

            File.AppendAllText(filePath, stringBuilder.ToString());
        }
    }
}
