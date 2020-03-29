// NOTE: bug in .net ADO client - Exception on inserting Guids
//    Exception has occurred: CLR/System.NotSupportedException
// An unhandled exception of type 'System.NotSupportedException' occurred in ClickHouse.Ado.dll: 'Unknown column type UUID'
//    at ClickHouse.Ado.Impl.ColumnTypes.ColumnType.Create(String name)
//    at ClickHouse.Ado.Impl.Data.ColumnInfo.Read(ProtocolFormatter formatter, Int32 rows)
//    at ClickHouse.Ado.Impl.Data.Block.Read(ProtocolFormatter formatter)
//    at ClickHouse.Ado.Impl.ProtocolFormatter.ReadPacket(Response rv)
//    at ClickHouse.Ado.Impl.ProtocolFormatter.ReadSchema()
//    at ClickHouse.Ado.ClickHouseCommand.Execute(Boolean readResponse, ClickHouseConnection connection)
//    at ClickHouse.Ado.ClickHouseCommand.ExecuteNonQuery()
//    at ClickHouse.Net.ClickHouseDatabase.<>c__DisplayClass26_0`1.<BulkInsert>b__0(ClickHouseCommand cmd)
//    at ClickHouse.Net.ClickHouseDatabase.Execute(Action`1 body, String commandText)
//    at ClickHouse.Net.ClickHouseDatabase.BulkInsert[T](String tableName, IEnumerable`1 columns, IEnumerable`1 bulk)
//    at InsertTool.Program.Main(String[] args)

using ClickHouse.Ado;
using ClickHouse.Net;
using ClickHouse.Net.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InsertTool
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
            using(var db = new ClickHouseDatabase(
                new ClickHouseConnectionSettings(ConnectionString),
                new ClickHouseCommandFormatter(),
                new ClickHouseConnectionFactory(),
                null,
                new DefaultPropertyBinder()))
                {
                    db.Open();

                    var data = new EventData {
                        Uid = Guid.NewGuid(),
                        GroupId = Guid.NewGuid(),
                        Reference = "Test" + Guid.NewGuid(),
                        State = State.created,
                        Errors = Enumerable.Empty<string>().ToList(),
                        OccuredAt = DateTime.UtcNow,
                        InsertedAt = DateTime.UtcNow,
                    };

                    var cols = new[] { "Uid","GroupId","Reference","State","Errors","OccuredAt","InsertedAt" };
                    var bulkdata = new[] { data };

                    db.BulkInsert(tableName, cols, bulkdata);

                    db.Close();
                }
        }

        class EventData {
            public Guid Uid;
            public Guid GroupId;
            public String Reference;
            public State State;
            public List<String> Errors;
            public DateTime OccuredAt;
            public DateTime InsertedAt;
        }

        enum State {
            created = 1,
            requested = 2,
            received = 3,
            approved = 4,
            rejected = 5
        }
    }
}
