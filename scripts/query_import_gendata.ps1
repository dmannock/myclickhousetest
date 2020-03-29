$query = "INSRT INTO events FORMAT CSV"
$res = cat ../gendata.csv | winpty docker run -it --rm --link some-clickhouse-server:clickhouse-server yandex/clickhouse-client --host clickhouse-server --database datasets --query $query
Write-Output $res