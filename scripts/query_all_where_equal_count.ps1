$query = "SELECT count(*) from datasets.ontime WHERE OriginState='AK' GROUP BY OriginState"
$res = docker run -it --rm --link some-clickhouse-server:clickhouse-server yandex/clickhouse-client --host some-clickhouse-server -t --query $query
$res > query_all__where_equal_count_datasets.ontime.txt
Write-Output $res
