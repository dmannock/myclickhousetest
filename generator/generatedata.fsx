// Notes: insert perf may be affected since generated data is not sorted by date
// TODO: generation perf could be optimised, not needed yet

// Database:
// CREATE TABLE datasets.events (
// 	`Uid` UUID,
// 	`GroupId` UUID,
// 	`Reference` String,
// 	`State` Enum(
// 		'created' = 1,
// 		'requested' = 2,
// 		'received' = 3,
// 		'approved' = 4,
// 		'rejected' = 5
// 	),
// 	`Errors` Array(String),
// 	`OccuredAt` DateTime,
// 	`InsertedAt` DateTime DEFAULT now()
//  ) ENGINE = MergeTree()
//  PARTITION BY toYYYYMM(OccuredAt)
//  ORDER BY (OccuredAt, GroupId, Uid);

// CREATE TABLE datasets.current_event_state (
//     `GroupId` UUID,
//     `CreatedCount` UInt64,
//     `RequestedCount` UInt64,
//     `ReceivedCount` UInt64,
//     `ApprovedCount` UInt64,
//     `RejectedCount` UInt64,
//     `LastOccuredAt` DateTime
//  ) ENGINE = SummingMergeTree()
//  PARTITION BY toYYYYMM(LastOccuredAt)
//  ORDER BY (GroupId, LastOccuredAt);

// CREATE MATERIALIZED VIEW datasets.current_event_state_mv
// TO current_event_state
// AS SELECT
//     GroupId,
//     countIf(State = 'created') as CreatedCount,
//     countIf(State = 'requested') as RequestedCount,
//     countIf(State = 'received') as ReceivedCount, 
//     countIf(State = 'approved') as ApprovedCount,
//     max(OccuredAt) as LastOccuredAt
// FROM (
//     SELECT
//         State,
//         GroupId,
//         OccuredAt
//     FROM datasets.events
//     GROUP BY
//         Uid,
//         State,
//         GroupId,
//         OccuredAt
// )
// GROUP BY GroupId

//manually load old data
//normally have date constraint up to the point currently processing data is arriving
//NOTE: STILL CRASHES SERVER IN CONTAINER IF DATA > ~20million
// INSERT INTO current_event_state
// SELECT
//     GroupId,
//     countIf(State = 'created') as CreatedCount,
//     countIf(State = 'requested') as RequestedCount,
//     countIf(State = 'received') as ReceivedCount, 
//     countIf(State = 'approved') as ApprovedCount,
//     max(OccuredAt) as LastOccuredAt
// FROM (
//     SELECT
//         State,
//         GroupId,
//         OccuredAt
//     FROM datasets.events
//     GROUP BY
//         Uid,
//         State,
//         GroupId,
//         OccuredAt
// )
// GROUP BY GroupId

//query from view for group
// SELECT
//     GroupId,
//     CreatedCount,
//     RequestedCount,
//     ReceivedCount, 
//     ApprovedCount,
//     LastOccuredAt
// FROM current_event_state
// WHERE GroupId = 'cbb0dff4-8934-4cad-af8b-5dc74aa5a8f4'

//query aggregated summary
// SELECT
//     sum(CreatedCount),
//     sum(RequestedCount),
//     sum(ReceivedCount), 
//     sum(ApprovedCount),
// FROM current_event_state

// similar perf as orig
// CREATE TABLE datasets.events_v2 (
// 	`Uid` UUID,
// 	`GroupId` UUID,
// 	`Reference` String,
// 	`State` Enum(
// 		'created' = 1,
// 		'requested' = 2,
// 		'received' = 3,
// 		'approved' = 4,
// 		'rejected' = 5
// 	),
// 	`Errors` Array(String),
// 	`OccuredAt` DateTime,
// 	`InsertedAt` DateTime
//  ) ENGINE = MergeTree()
//  PARTITION BY toYYYYMM(OccuredAt)
//  ORDER BY (GroupId, Uid, toDate(OccuredAt));

// csv output:  Uid, GroupId, Reference, State, Errors, OccuredAt, InsertedAt
// as example insert:
// INSERT INTO datasets.events VALUES('86218b86-f8ac-44d6-84ec-e8a0a65bc41a','410578df-9c6c-4b17-82c1-6f648b070795',0,'created',[],toDateTime('2010/03/28 13:42:56'),toDateTime('2020/03/28 13:42:56'));
open System
open System.IO
//config
let numberOfIds = 10_000_00
let numberOfGroups = 100
let outputFile = "gendata.csv"
let occuredAtEnd = DateTime.UtcNow
let occuredAtStart = occuredAtEnd.AddYears(-10)
//Note: stored as enum - normalise to lowercase
//single state flow handled - not worrying about state flow splits e.g. approved OR rejected
let states = ["created"; "requested"; "received"; "approved"] //; "rejected"]

let toDbDate (dt: DateTime) = dt.ToString(Globalization.CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)

let genUid() = Guid.NewGuid()

let genGroups n = Seq.init n (fun _ -> genUid()) |> Seq.cache

let selectRandomItem gs = 
    let rnd = Random()
    Seq.item (rnd.Next(gs |> Seq.length)) gs

let getPrevousStates states state = states |> List.takeWhile (fun s -> s <> state)

let genRandomStateIncludingPrevious states = 
    let randState = selectRandomItem states
    getPrevousStates states randState @ [randState]

let genConsecutiveDatesBetween (min: DateTime) (max: DateTime) num =
    let diff = max - min
    let equallyDivided = diff.TotalDays / (float num)
    let rnd = Random()
    let genNoise() = equallyDivided |> Math.Floor |> int |> rnd.Next |> float
    [0..num - 1] |> List.map (fun i -> min.AddDays(float i * (genNoise())))

let genConsecutiveDates = genConsecutiveDatesBetween occuredAtStart occuredAtEnd

let genItem states groups i =
    let uid = genUid()
    let group = (selectRandomItem groups)
    let consecutiveStates = genRandomStateIncludingPrevious states
    let consecutiveDates = genConsecutiveDates (consecutiveStates |> List.length)
    let errors: string list = []
    (consecutiveStates, consecutiveDates)
    ||> List.zip
    |> List.map (fun (state, occuredAt) -> uid, group, i, state, errors, occuredAt, DateTime.UtcNow)

//generates 1 or more items each, hence the minCount, averages to ~2x
let generator states groups minCount = 
    Seq.init minCount (fun i -> genItem states groups i |> Seq.ofList)
    |> Seq.collect id

let formatline (uid, groupid, reference, state, errors, occuredat, insertedat) =
    String.Format("{0},{1},{2},{3},{4},{5},{6}", uid, groupid, reference, state, errors, occuredat |> toDbDate, insertedat |> toDbDate)

//run
let groups = (genGroups numberOfGroups)
let generatedItems = generator states groups numberOfIds

let formatted = generatedItems |> Seq.map formatline
File.WriteAllLines(outputFile, formatted)