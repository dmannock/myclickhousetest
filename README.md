# My Clickhouse Test

Test of ClickHouse db (usage, tooling, platforms, performance, etc).

Includes:
- Data Generator script
- Query Scripts
- .NET Connection example apps

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download)
- clickhouse-server
- clickhouse-client
- [WSL](https://docs.microsoft.com/en-us/windows/wsl/install-win10) (optional)
- [Powershell Core](https://github.com/PowerShell/PowerShell) (optional)

### Data generator script
Runs on any dotnet environment that supports fsi ([dotnet core SDK out of the box](https://dotnet.microsoft.com/download)).

## Usage
Setup server, connect and create required databases & tables.

### Create schema
```
CREATE DATABASE datasets
```

```
CREATE TABLE datasets.events (
	`Uid` UUID,
	`GroupId` UUID,
	`Reference` String,
	`State` Enum(
		'created' = 1,
		'requested' = 2,
		'received' = 3,
		'approved' = 4,
		'rejected' = 5
	),
	`Errors` Array(String),
	`OccuredAt` DateTime,
	`InsertedAt` DateTime
 ) ENGINE = MergeTree()
 PARTITION BY toYYYYMM(OccuredAt)
 ORDER BY (OccuredAt, GroupId, Uid);
```

### Data Generator script
Change config options at the top of the file then run
```
dotnet fsi generatedata.fsx
```
An output of 5 million items takes ~30seconds (640MB csv file) on an old pc.

### Insert Data
```
clickhouse-client --query "INSERT INTO datasets.events FORMAT CSV" < gendata.csv
```
Should take several seconds.

### Apps / Tools
```
dotnet run
```

## Examples
### Dataset: ontime

#### Count All
```
183953732 0.021 sec
```

#### Count for OriginState
1213878 Elapsed: 1.998 sec. Processed 183.95 million rows, 367.93 MB (92.05 million rows/s., 184.11 MB/s.)

#### Count for Carrier
```
SELECT count(*)
FROM datasets.ontime
WHERE toString(Carrier) = 'HA'
GROUP BY Carrier
```
1005566 1 rows in set. Elapsed: 1.760 sec. Processed 183.95 million rows, 367.93 MB (104.50 million rows/s., 209.02 MB/s.)

#### Count for UniqueCarrier
```
SELECT count(*)
FROM datasets.ontime
WHERE toString(UniqueCarrier) = '9E'
GROUP BY UniqueCarrier
```
1547825 1 rows in set. Elapsed: 2.595 sec. Processed 183.95 million rows, 1.29 GB (70.90 million rows/s., 496.27 MB/s.)

### Dataset: events

#### Count All
```
SELECT count(*)
FROM events
```
44999011 0.004 sec

#### Count for GroupId
```
SELECT count(*)
FROM datasets.events
WHERE GroupId = 'cbb0dff4-8934-4cad-af8b-5dc74aa5a8f4'
```
74343 1 rows in set. Elapsed: 0.073 sec. Processed 24.19 million rows, 387.10 MB (329.08 million rows/s., 5.27 GB/s.)

see /scripts for other queries

## License
[MIT](https://choosealicense.com/licenses/mit/)