open System
let numberOfIds = 1_000_000
let numberOfGroups = 100
let outputFile = "gendata.csv"
let occurredAtEnd = DateTime.UtcNow
let occurredAtStart = occurredAtEnd.AddYears(-10)

// Note: stored as enum in db - normalise to lowercase
// single state flow handled - not worrying about state flow splits e.g. approved OR rejected
let states = [|"created"; "requested"; "received"; "approved"|] //; "rejected"|]

let rnd = Random()
let toDbDate (dt: DateTime) = dt.ToString(Globalization.CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)

let genGroups n = Array.init n (fun _ -> Guid.NewGuid())

let selectRandomItem gs = Array.item (rnd.Next(gs |> Array.length)) gs

let genRandomStateIncludingPrevious states = 
    states |> Array.take (Array.length states |> rnd.Next |> (+) 1)
    
// good enough
let genConsecutiveDatesBetween (min: DateTime) (max: DateTime) num =
    let safeNum = num |> abs 
    let split = (max - min).TotalDays / float safeNum
    let genNoise() = split * Math.Max(rnd.NextDouble(), 0.3)
    let nextDate = (+) 1 >> float >> (*) (genNoise()) >> min.AddDays
    Array.init safeNum nextDate

let genItem dateGenerator states groups i =
    let uid = Guid.NewGuid()
    let group = selectRandomItem groups
    let consecutiveStates = genRandomStateIncludingPrevious states
    let consecutiveDates = dateGenerator (Array.length consecutiveStates)
    Array.map2 (fun state occurredAt -> struct (uid, group, i, state, occurredAt, DateTime.UtcNow)) consecutiveStates consecutiveDates

// generates 1 or more items each, hence the minCount, averages to ~2.5x
let generator states groups startDate endDate minCount = 
    let dateGenerator = genConsecutiveDatesBetween startDate endDate
    Seq.init minCount (genItem dateGenerator states groups)
    |> Seq.collect Seq.ofArray

let formatline struct (uid, groupid, reference, state, occurredat, insertedat) =
    // Errors is currently unused, use empty array
    String.Format("{0},{1},{2},{3},[],{4},{5}", uid, groupid, reference, state, occurredat |> toDbDate, insertedat |> toDbDate)

//run
let groups = genGroups numberOfGroups
let generatedItems = generator states groups occurredAtStart occurredAtEnd numberOfIds

open System.IO
let formatted = generatedItems |> Seq.map formatline
File.WriteAllLines(outputFile, formatted)