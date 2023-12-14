using DataProcessingApp;
using DataProcessingApp.Controllers;
using DataProcessingApp.Models;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Data.SqlClient;
using System.Reflection.Emit;
class Programm
{
    static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();
        var logger = Log.Logger;


        FileGenerator fileGenerator = new FileGenerator(logger);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GeneratedFiles");
        string mergedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MergedFile.txt");
        fileGenerator.GenerateFiles(100, 100000, path);


        FileProcessor fileProcessor = new FileProcessor(logger);
        await fileProcessor.ProcessFilesAsync(path, mergedPath, "abc");


        string connectionString = "Data Source=.;Initial Catalog=DataProcessingDB;Integrated Security=True";
        try
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                DataImport data=new DataImport(logger);
                await connection.OpenAsync();
                await data.ImportDataAsync(mergedPath, connectionString);

                (long sum, double median) result = await data.CalculateSumAndMedianAsync(connectionString);

                Console.WriteLine($"Sum: {result.sum}, Median: {result.median}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

}