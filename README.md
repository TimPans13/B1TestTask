Перед запуском следует выполнить скрипт в базе данных(так же приложен в DbScript.txt)

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DataProcessingDB;')
BEGIN
  CREATE DATABASE DataProcessingDB;
END;
GO

USE DataProcessingDB;
GO

IF OBJECT_ID('DataModels', 'U') IS NOT NULL
    DROP TABLE DataModels;
GO

CREATE TABLE DataModels
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    Date NVARCHAR(255),
    LatinChars NVARCHAR(10),
    RussianChars NVARCHAR(10),
    EvenInt INT,
    FloatNumber FLOAT
);
GO



Используемые Библиотеки:

Microsoft.EntityFrameworkCore
Serilog
Serilog.Extensions.Logging
Serilog.Sinks.Console
Serilog.Sinks.File
Microsoft.EntityFrameworkCore.Design
Microsoft.EntityFrameworkCore.Tools
Microsoft.Extensions.Configuration
Microsoft.Extensions.Configuration.Json
Newtonsoft.Json
System.Data.SqlClient
