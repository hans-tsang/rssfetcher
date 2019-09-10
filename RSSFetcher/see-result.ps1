<#
    Read the result with right click on see-result.ps1
#>

# I have absolutely no idea why database need to be resolved with absolute path
# $dbPath = 'C:\Users\jason.liu\Documents\rssfetcher\RSSFetcher\bin\Debug\rss.db';
$dbPath = Join-Path $PSScriptRoot "rss.db"
Add-Type -Path ".\System.Data.SQLite.dll"
$con = New-Object -TypeName System.Data.SQLite.SQLiteConnection
$con.ConnectionString = "Data Source=$dbPath;Version=3;"
$con.Open()

$sql = $con.CreateCommand()
$sql.CommandText = "SELECT * FROM JOBS;"

$adapter = New-Object -TypeName System.Data.SQLite.SQLiteDataAdapter $sql

$data = New-Object System.Data.DataSet
[void]$adapter.Fill($data)

$rowCount = $data.tables[0].Rows.Count
(1..$rowCount) | foreach { $data.tables[0].Rows[$_] } | out-gridview -Title 'Jobs'

$data.Dispose()
Read-Host "Press any key to close..."