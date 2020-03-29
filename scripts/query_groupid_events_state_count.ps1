$query = "SELECT State, Count(State) FROM (
    SELECT State from datasets.events WHERE GroupId = 'cbb0dff4-8934-4cad-af8b-5dc74aa5a8f4' GROUP BY Uid, State
) GROUP BY State;"
# ┌─State─────┬─Count(State)─┐
# │ created   │        29790 │
# │ requested │        22316 │
# │ received  │        14821 │
# │ approved  │         7416 │
# └───────────┴──────────────┘
# slowest time 
# Elapsed: 0.092 sec. Processed 6.07 million rows, 199.87 MB (65.89 million rows/s., 2.17 GB/s.)
$res = docker run -it --rm --link some-clickhouse-server:clickhouse-server yandex/clickhouse-client --host some-clickhouse-server -t --query $query
$res > query_groupid_events_state_count_datasets.events.txt
Write-Output $res
