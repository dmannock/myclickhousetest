$query = "SELECT count(*) from datasets.events WHERE GroupId='cbb0dff4-8934-4cad-af8b-5dc74aa5a8f4'"
$res = docker run -it --rm --link some-clickhouse-server:clickhouse-server yandex/clickhouse-client --host some-clickhouse-server -t --query $query
$res > query_matching_group_count_datasets.events.txt
Write-Output $res
