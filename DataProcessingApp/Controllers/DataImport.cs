using DataProcessingApp.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace DataProcessingApp
{
    public class DataImport
    {
        private readonly ILogger _logger;

        public DataImport(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        // Метод для импорта данных из файла в базу данных
        /// <param name="filePath">Путь к файлу с данными</param>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        public async Task ImportDataAsync(string filePath, string connectionString)
        {
            const int batchSize = 100000;//оптимально количество
            int rowsAffected = 0;
            int totalRows = 0;

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    totalRows = File.ReadLines(filePath).Count();
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Использование SqlBulkCopy для эффективной вставки данных в таблицу
                    using (var bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = "DataModels";

                        DataTable dataTable = new DataTable();
                        dataTable.Columns.Add("Date", typeof(DateTime));
                        dataTable.Columns.Add("LatinChars", typeof(string));
                        dataTable.Columns.Add("RussianChars", typeof(string));
                        dataTable.Columns.Add("EvenInt", typeof(int));
                        dataTable.Columns.Add("FloatNumber", typeof(double));

                        bulkCopy.ColumnMappings.Add("Date", "Date");
                        bulkCopy.ColumnMappings.Add("LatinChars", "LatinChars");
                        bulkCopy.ColumnMappings.Add("RussianChars", "RussianChars");
                        bulkCopy.ColumnMappings.Add("EvenInt", "EvenInt");
                        bulkCopy.ColumnMappings.Add("FloatNumber", "FloatNumber");

                        // Чтение файла и добавление данных в DataTable, с последующей вставкой пакетами
                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            while (!reader.EndOfStream)
                            {
                                string originalLine = reader.ReadLine();
                                if (originalLine == null)
                                    break;

                                string[] values = originalLine.Split(new[] { "||" }, StringSplitOptions.None);

                                // Проверка и преобразование данных перед добавлением в DataTable
                                if (values.Length >= 5 &&
                                    DateTime.TryParseExact(values[0], "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue) &&
                                    int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int evenIntValue) &&
                                    double.TryParse(values[4].Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double floatNumberValue))
                                {
                                    DataRow row = dataTable.NewRow();

                                    row["Date"] = dateValue;
                                    row["LatinChars"] = values[1];
                                    row["RussianChars"] = values[2];
                                    row["EvenInt"] = evenIntValue;
                                    row["FloatNumber"] = floatNumberValue;

                                    dataTable.Rows.Add(row);

                                    rowsAffected++;

                                    if (rowsAffected % batchSize == 0)
                                    {
                                        bulkCopy.WriteToServer(dataTable);
                                        Console.WriteLine($"Rows added to the database: {rowsAffected} out of {totalRows}");
                                        dataTable.Clear();
                                    }
                                }
                            }

                            // Вставка оставшихся строк, если их количество не кратно batchSize
                            if (rowsAffected % batchSize != 0)
                            {
                                bulkCopy.WriteToServer(dataTable);
                                Console.WriteLine($"Rows added to the database: {rowsAffected} out of {totalRows}");
                            }
                        }
                    }
                }

                _logger.Information($"{totalRows} строк успешно обработано.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during import: {ex.Message}");
            }
        }

        // Метод для вычисления суммы и медианы из базы данных
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <returns>Кортеж с суммой и медианой</returns>
        public async Task<(long, double)> CalculateSumAndMedianAsync(string connectionString)
        {
            (long sum, double median) result = (0, 0);

            try
            {
                // Открытие соединения с базой данных
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Выполнение SQL-запроса для вычисления суммы и медианы. При изменении ошибки будет сложно отследить
                    string sql = "SELECT SUM(CAST(EvenInt AS bigint)), AVG(FloatNumber) FROM DataModels";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        var reader = await command.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            result.sum = reader.GetInt64(0);
                            result.median = reader.GetDouble(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error: " + ex.Message);
            }

            return result;
        }
    }
}
