//headers?
// output:  Uid, GroupId, Reference, State, Errors, OccuredAt, InsertedAt
open System
//config
let numberOfIds = 1
let numberOfGroups = 10
let occuredAtEnd = DateTime.UtcNow
let occuredAtStart = occuredAtEnd.AddYears(-10)
let states = ["Created"; "Requested"; "Received"; "Approved"] //; "Rejected"]

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
    let group = (selectRandomItem groups)
    let consecutiveStates = genRandomStateIncludingPrevious states
    let consecutiveDates = genConsecutiveDates (consecutiveStates |> List.length)
    let errors: string list = []
    (consecutiveStates, consecutiveDates)
    ||> List.zip
    |> List.map (fun (state, occuredAt) -> genUid(), group, i, state, errors, occuredAt, DateTime.UtcNow)

let generator states groups count = 
    Seq.init count (fun i -> genItem states groups i |> Seq.ofList)
    |> Seq.collect id

let groups = (genGroups 10)
generator states groups 2
|> List.ofSeq
|> printf "%A"


