open System
let numberOfIds = 1_000_000
let numberOfGroups = 100
let outputFile = "gendata.csv"
let occurredAtEnd = DateTime.UtcNow
let occurredAtStart = occurredAtEnd.AddYears(-10)

//Note: stored as enum in db - normalise to lowercase
//single state flow handled - not worrying about state flow splits e.g. approved OR rejected
let states = [|"created"; "requested"; "received"; "approved"|] //; "rejected"|]

let rnd = Random()
let toDbDate (dt: DateTime) = dt.ToString(Globalization.CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)

let genGroups n = Array.init n (fun _ -> Guid.NewGuid())

let selectRandomItem gs = Array.item (rnd.Next(gs |> Array.length)) gs

let genRandomStateIncludingPrevious states = 
    states |> Array.take (states |> Array.length |> rnd.Next)

//good enough, head = min date e.g. zero randomness
let genConsecutiveDatesBetween (min: DateTime) (max: DateTime) num =
    let equallyDivided = (max - min).TotalDays / (float num)
    let genNoise() = equallyDivided * rnd.NextDouble()
    let nextDate = float >> (*) (genNoise()) >> min.AddDays
    [|0..num - 1|] |> Array.map nextDate

let genItem dateGenerator states groups i =
    let uid = Guid.NewGuid()
    let group = (selectRandomItem groups)
    let consecutiveStates = genRandomStateIncludingPrevious states
    let consecutiveDates = dateGenerator (Array.length consecutiveStates)
    let errors: string list = []
    (consecutiveStates, consecutiveDates)
    ||> Array.zip
    |> Array.map (fun (state, occurredAt) -> uid, group, i, state, errors, occurredAt, DateTime.UtcNow)

//generates 1 or more items each, hence the minCount, averages to ~2.5x
let generator states groups startDate endDate minCount = 
    let dateGenerator = genConsecutiveDatesBetween startDate endDate
    Seq.init minCount (genItem dateGenerator states groups)
    |> Seq.collect Seq.ofArray

let formatline (uid, groupid, reference, state, errors, occurredat, insertedat) =
    String.Format("{0},{1},{2},{3},{4},{5},{6}", uid, groupid, reference, state, errors, occurredat |> toDbDate, insertedat |> toDbDate)

//run
let groups = genGroups numberOfGroups
let generatedItems = generator states groups occurredAtStart occurredAtEnd numberOfIds
generatedItems

open System.IO
let formatted = generatedItems |> Seq.map formatline
File.WriteAllLines(outputFile, formatted)