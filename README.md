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
```SQL
CREATE DATABASE datasets
```

```SQL
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
	`OccurredAt` DateTime,
	`InsertedAt` DateTime
 ) ENGINE = MergeTree()
 PARTITION BY toYYYYMM(OccurredAt)
 ORDER BY (OccurredAt, GroupId, Uid);
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

CSV format is:
```csv
Uid, GroupId, Reference, State, Errors, OccurredAt, InsertedAt
```

Example SQL Insert:
```SQL
INSERT INTO datasets.events VALUES('86218b86-f8ac-44d6-84ec-e8a0a65bc41a','410578df-9c6c-4b17-82c1-6f648b070795',0,'created',[],toDateTime('2010/03/28 13:42:56'),toDateTime('2020/03/28 13:42:56'));
```

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
```SQL
SELECT count(*)
FROM datasets.ontime
WHERE toString(Carrier) = 'HA'
GROUP BY Carrier
```
1005566 1 rows in set. Elapsed: 1.760 sec. Processed 183.95 million rows, 367.93 MB (104.50 million rows/s., 209.02 MB/s.)

#### Count for UniqueCarrier
```SQL
SELECT count(*)
FROM datasets.ontime
WHERE toString(UniqueCarrier) = '9E'
GROUP BY UniqueCarrier
```
1547825 1 rows in set. Elapsed: 2.595 sec. Processed 183.95 million rows, 1.29 GB (70.90 million rows/s., 496.27 MB/s.)

### Dataset: events

#### Count All
```SQL
SELECT count(*)
FROM events
```
44999011 0.004 sec

#### Count for GroupId
```SQL
SELECT count(*)
FROM datasets.events
WHERE GroupId = 'cbb0dff4-8934-4cad-af8b-5dc74aa5a8f4'
```
74343 1 rows in set. Elapsed: 0.073 sec. Processed 24.19 million rows, 387.10 MB (329.08 million rows/s., 5.27 GB/s.)

### Query Aggregated State Totals (Directly)

**NOTE:** MAY CRASHES SERVER IN CONTAINER IF DATA > ~20million

```SQL
SELECT
    GroupId,
    countIf(State = 'created') as CreatedCount,
    countIf(State = 'requested') as RequestedCount,
    countIf(State = 'received') as ReceivedCount, 
    countIf(State = 'approved') as ApprovedCount,
    countIf(State = 'rejected') as RejectedCount,
    max(OccurredAt) as LastOccurredAt
FROM (
    SELECT
        State,
        GroupId,
        OccurredAt
    FROM datasets.events
    GROUP BY
        GroupId,
        Uid,
        State,
        OccurredAt
)
GROUP BY GroupId
```
8500 rows in set. Elapsed: 9.321 sec. Processed 27.50 million rows, 1.02 GB (2.95 million
 rows/s., 109.17 MB/s.)

This is getting slow, lets try improving it (by maybe 2000x) with views below.

see /scripts for other queries

## Further Usage (views)

### Create derived table to be updated
```SQL
CREATE TABLE datasets.current_event_state (
    `GroupId` UUID,
    `CreatedCount` UInt64,
    `RequestedCount` UInt64,
    `ReceivedCount` UInt64,
    `ApprovedCount` UInt64,
    `RejectedCount` UInt64,
    `LastOccurredAt` DateTime
 ) ENGINE = SummingMergeTree()
 PARTITION BY toYYYYMM(LastOccurredAt)
 ORDER BY (GroupId, LastOccurredAt);
```

### Create the materialized view that will update the above table
```SQL
CREATE MATERIALIZED VIEW datasets.current_event_state_mv
TO current_event_state
AS SELECT
    GroupId,
    countIf(State = 'created') as CreatedCount,
    countIf(State = 'requested') as RequestedCount,
    countIf(State = 'received') as ReceivedCount, 
    countIf(State = 'approved') as ApprovedCount,
    countIf(State = 'rejected') as RejectedCount,
    max(OccurredAt) as LastOccurredAt
FROM (
    SELECT
        State,
        GroupId,
        OccurredAt
    FROM datasets.events
    GROUP BY
        GroupId,
        Uid,
        State,
        OccurredAt 
)
GROUP BY GroupId
```

### (Optional) Update derived table if there was existing data prior.

Normally it would have a date constraint up to the point currently processing data is arriving.
```SQL
INSERT INTO current_event_state
SELECT
    GroupId,
    countIf(State = 'created') as CreatedCount,
    countIf(State = 'requested') as RequestedCount,
    countIf(State = 'received') as ReceivedCount, 
    countIf(State = 'approved') as ApprovedCount,
    countIf(State = 'rejected') as RejectedCount,
    max(OccurredAt) as LastOccurredAt
FROM (
    SELECT
        State,
        GroupId,
        OccurredAt
    FROM datasets.events
    GROUP BY
        GroupId,
        Uid,
        State,
        OccurredAt
)
GROUP BY GroupId
```

### Query raw view data for GroupId

```SQL
SELECT
    GroupId,
    CreatedCount,
    RequestedCount,
    ReceivedCount, 
    ApprovedCount,
    RejectedCount,
    LastOccurredAt
FROM current_event_state
WHERE GroupId = 'cbb0dff4-8934-4cad-af8b-5dc74aa5a8f4'
```
1 rows in set. Elapsed: 0.003 sec. Processed 2.80 thousand rows, 167.76 KB (872.58 thousa
nd rows/s., 52.35 MB/s.)

### Query Aggregated State Totals (From View)
```SQL
SELECT
    sum(CreatedCount) AS TotalCreatedCount,
    sum(RequestedCount) AS TotalRequestedCount,
    sum(ReceivedCount) AS TotalReceivedCount, 
    sum(ApprovedCount) AS TotalApprovedCount,
    sum(RejectedCount) AS TotalRejectedCount
FROM current_event_state
```
1 rows in set. Elapsed: 0.004 sec. Processed 25.30 thousand rows, 1.01 MB (5.48 million r
ows/s., 219.37 MB/s.)

## License
[MIT](https://choosealicense.com/licenses/mit/)

## Notes
- Insert perf may be affected since generated data is not sorted by date
- TODO: generation perf could be optimised, also not taking advantage of multiple cores, not needed yet