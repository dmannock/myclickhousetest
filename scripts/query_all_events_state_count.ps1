# NOTE: NOT optimised, ok for small datasets around 20mil otherwise docker container will stop.
# can confirm groupings with: SELECT count(*), Uid, State from datasets.events GROUP BY Uid, State limit 10
# ┌─count()─┬──────────────────────────────────Uid─┬─State─────┐
# │       1 │ e63cdc29-67f4-41da-849b-0e593b52963c │ created   │
# │       1 │ 88569b1c-57bc-4f33-a529-058c95bd8feb │ received  │
# │       1 │ 2fda990e-8e20-42ba-87da-57598502c4ac │ received  │
# │       1 │ f09d5beb-dcca-4fcd-bd7d-48e61c0a4bb5 │ created   │
# │       1 │ 986d1a35-aebd-451a-8abb-32ac5ae8baaf │ received  │
# │       1 │ f24be682-cdb3-4228-b794-be3f9aaadf0a │ created   │
# │       1 │ ede508a7-b916-433a-af03-c3fbd13a5d8d │ requested │
# │       1 │ a80f085c-b8f8-4b82-bbf3-87e255cd02f5 │ created   │
# │       1 │ d3f15c45-c404-40da-9eac-013d4b4eba9d │ received  │
# │       1 │ 905dac96-e4ec-4a43-bf87-e6d58afc26eb │ approved  │
# └─────────┴──────────────────────────────────────┴───────────┘
$query = "SELECT State, Count(State) FROM (
    SELECT State from datasets.events GROUP BY Uid, State
) GROUP BY State;"
# ┌─State─────┬─Count(State)─┐
# │ created   │      4000003 │
# │ requested │      3000049 │
# │ received  │      2000464 │
# │ approved  │       998934 │
# └───────────┴──────────────┘
# slowest time 
# Elapsed: 1.994 sec. Processed 10.00 million rows, 169.99 MB (5.01 million rows/s., 85.24 MB/s.)
$res = docker run -it --rm --link some-clickhouse-server:clickhouse-server yandex/clickhouse-client --host some-clickhouse-server -t --query $query
$res > query_all_events_state_count_datasets.events.txt
Write-Output $res
