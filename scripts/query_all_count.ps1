$query = "SELECT count(*) from datasets.events"
$res = docker run -it --rm --link some-clickhouse-server:clickhouse-server yandex/clickhouse-client --host some-clickhouse-server -t --query $query
$res > query_all_count_datasets.events.txt
Write-Output $res
