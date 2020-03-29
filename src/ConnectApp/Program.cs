using ClickHouse.Ado;
using ClickHouse.Net;
using ClickHouse.Net.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectApp
{
class Program
    {
        private const string host = "localhost";
        private const string port = "9000";
        private const string username = "default";
        private const string password = "";
        private const string database = "datasets";
        private const string tableName = "events";
        private static readonly string ConnectionString = 
            $"Compress=True;CheckCompressedHash=False;Compressor=lz4;Host={host};Port={port};User={username};Password={password};SocketTimeout=600000;Database={database};";

        static void Main(string[] args)
        {
            var db = new ClickHouseDatabase(
                new ClickHouseConnectionSettings(ConnectionString),
                new ClickHouseCommandFormatter(),
                new ClickHouseConnectionFactory(),
                null,
                new DefaultPropertyBinder());
                
            var columns = db.DescribeTable(tableName).ToArray();

            var text = string.Join("\n", columns.Select(c => $"{c.Name}: {c.Type}"));

            Console.WriteLine($"table description: {text}");
            // db.Open();
            // need to check database / table exists first?

            // var command = objToInsert.GetInsertCommand();
            // db.ExecuteNonQuery(command);

            // command = $"SELECT count(*) FROM {tableName}";
            // var resultItem = db.ExecuteQueryMapping<long>(command, convention: new UnderscoreNamingConvention()).Single();
            // db.Close();
            
        }
    }
}
