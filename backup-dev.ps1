<#
    backup-dev.ps1  -  Volledige back-up van de GoDutchSnelStartWebApp development-omgeving.

    Maakt een tijdstempel-map met:
      repo.zip                : complete werkkopie incl. .git-historie en lokale config
                                (bin/obj/.vs/publish/node_modules eruit)
      GoDutchSnelStartDb.bak  : COPY_ONLY database-back-up (verstoort geen back-upketen)
      db-programmability.sql  : alle stored procedures / functions / views / triggers als tekst
      user-secrets\           : user-secrets van het Web-project (DB-connectie, subscription key)
      MANIFEST.txt            : inhoud + restore-instructies (incl. DPAPI-caveat)

    Gebruik:
      .\backup-dev.ps1                                  # map onder C:\Backups\GoDutchDev\
      .\backup-dev.ps1 -ZipAll                          # daarna alles in 1 .zip
      .\backup-dev.ps1 -SqlUser sa -SqlPassword '...'   # SQL-auth i.p.v. Windows-auth
#>

[CmdletBinding()]
param(
    [string]$RepoPath    = $PSScriptRoot,
    [string]$BackupRoot   = 'C:\Backups\GoDutchDev',
    [string]$SqlServer    = 'localhost',
    [string]$Database     = 'GoDutchSnelStartDb',
    [string]$SqlUser      = '',            # leeg = Windows-auth (-E)
    [string]$SqlPassword  = '',
    [switch]$ZipAll
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoPath)) { $RepoPath = (Get-Location).Path }
$RepoPath = (Resolve-Path $RepoPath).Path
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$dest  = Join-Path $BackupRoot "GoDutchDev-$stamp"
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Write-Host "Back-up doel: $dest`n"

# sqlcmd-auth-argumenten
$sqlAuth = if ([string]::IsNullOrWhiteSpace($SqlUser)) { @('-E') } else { @('-U', $SqlUser, '-P', $SqlPassword) }
$sqlBase = @('-S', $SqlServer) + $sqlAuth + @('-d', $Database, '-C')
function Invoke-Sql([string]$query, [string[]]$extra = @()) {
    $out = & sqlcmd @($sqlBase + @('-b') + $extra + @('-Q', $query))
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd faalde (exit $LASTEXITCODE) voor: $query" }
    return $out
}

# -- 1. Broncode ---------------------------------------------------------------
Write-Host '[1/5] Broncode inpakken (repo.zip)...'
$staging = Join-Path $env:TEMP "gd-src-$stamp"
$null = robocopy $RepoPath $staging /MIR `
    /XD bin obj .vs .vscode node_modules publish TestResults artifacts packages `
    /NFL /NDL /NP /R:1 /W:1
if ($LASTEXITCODE -ge 8) { throw "robocopy faalde (exit $LASTEXITCODE)" }
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath (Join-Path $dest 'repo.zip') -Force
Remove-Item $staging -Recurse -Force
Write-Host ('      ' + [math]::Round((Get-Item (Join-Path $dest 'repo.zip')).Length / 1MB, 1) + ' MB')

# -- 2. Database-back-up -----------------------------------------------------
Write-Host "[2/5] Database '$Database' back-uppen..."
$bakName = "$Database.bak"
# Naar de default backup-map van SQL Server schrijven (permissies), daarna verplaatsen.
$defPath = (Invoke-Sql "SET NOCOUNT ON; SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))" @('-h', '-1', '-W')).Trim()
$sqlBak  = Join-Path $defPath $bakName
Invoke-Sql "BACKUP DATABASE [$Database] TO DISK = N'$sqlBak' WITH COPY_ONLY, INIT, FORMAT, STATS = 25" | Out-Null
Move-Item -LiteralPath $sqlBak -Destination (Join-Path $dest $bakName) -Force
Write-Host ('      ' + [math]::Round((Get-Item (Join-Path $dest $bakName)).Length / 1MB, 1) + ' MB')

# -- 3. Stored procedures / functions / views (leesbaar, ter referentie) -----
Write-Host '[3/5] DB-programmability scripten (db-programmability.sql)...'
$progOut = Join-Path $dest 'db-programmability.sql'
try {
    $listQ = "SET NOCOUNT ON; SELECT QUOTENAME(s.name)+'.'+QUOTENAME(o.name) " +
             "FROM sys.sql_modules m JOIN sys.objects o ON o.object_id=m.object_id " +
             "JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE o.is_ms_shipped=0 ORDER BY o.type,o.name"
    $names = & sqlcmd @($sqlBase + @('-h', '-1', '-W', '-Q', $listQ)) |
             ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -notmatch 'rows affected' }
    if ($LASTEXITCODE -ne 0) { throw "objecten opsommen faalde (exit $LASTEXITCODE)" }

    "-- $Database programmability - $(Get-Date -Format 'yyyy-MM-dd HH:mm') - $($names.Count) objecten" |
        Set-Content -Path $progOut -Encoding UTF8
    foreach ($n in $names) {
        $def = & sqlcmd @($sqlBase + @('-y', '0', '-Q',
                "SET NOCOUNT ON; SELECT definition FROM sys.sql_modules WHERE object_id = OBJECT_ID('$n')")) |
               Where-Object { $_ -notmatch '^\s*definition\s*$' -and $_ -notmatch '^-{2,}\s*$' }
        Add-Content -Path $progOut -Encoding UTF8 -Value ("`r`n/* ===== $n ===== */")
        Add-Content -Path $progOut -Encoding UTF8 -Value (($def -join "`r`n").TrimEnd())
        Add-Content -Path $progOut -Encoding UTF8 -Value 'GO'
    }
    Write-Host "      $($names.Count) objecten"
}
catch {
    Write-Warning "DB-programmability scripten mislukt: $_"
    Write-Warning "Niet erg - de volledige programmability zit in $bakName."
}

# -- 4. User-secrets van het Web-project -----------------------------------
Write-Host '[4/5] User-secrets kopieren...'
$csproj = Join-Path $RepoPath 'GoDutchSnelStartWebApp.Web\GoDutchSnelStartWebApp.Web.csproj'
$secId  = ([regex]'<UserSecretsId>(.+?)</UserSecretsId>').Match((Get-Content $csproj -Raw)).Groups[1].Value
$secDir = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secId"
if ($secId -and (Test-Path $secDir)) {
    Copy-Item $secDir (Join-Path $dest 'user-secrets') -Recurse -Force
    Write-Host "      $secId"
} else {
    Write-Host "      (geen user-secrets gevonden voor id '$secId')"
}

# -- 5. Manifest ---------------------------------------------------------------
Write-Host '[5/5] MANIFEST.txt schrijven...'
$gitHead  = (& git -C $RepoPath rev-parse HEAD 2>$null)
$dirty    = (& git -C $RepoPath status --porcelain 2>$null)
$dirtyNote = if ($dirty) { '  (LET OP: werkkopie had ongecommitte wijzigingen - die zitten wel in repo.zip)' } else { '' }

$manifest = @"
GoDutchSnelStartWebApp - development back-up
Gemaakt : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  op  $env:COMPUTERNAME
Repo    : $RepoPath
Git HEAD: $gitHead$dirtyNote

INHOUD
  repo.zip                Volledige werkkopie incl. .git (complete historie) en lokale
                          config-bestanden. Build-output (bin/obj/.vs/publish/node_modules)
                          is weggelaten.
  $bakName   SQL Server COPY_ONLY back-up van database '$Database' ($SqlServer).
                          Bevat ook alle stored procedures/functions/views/triggers.
  db-programmability.sql   Alle programmability-objecten als leesbare T-SQL (referentie).
  user-secrets\            User-secrets van het Web-project ($secId):
                          DB-connectiestring, SnelStart subscription key, auto-sync-vlaggen.

BEVAT GEHEIMEN - bewaar deze back-up op een beveiligde locatie (SQL sa-wachtwoord in de
connectiestring, e-mailwachtwoord in appsettings.Local.json, subscription key).

RESTORE
  Broncode : repo.zip uitpakken -> 'dotnet restore' -> openen in Visual Studio.
  Database : in SSMS 'Restore Database' vanaf $bakName, of:
             RESTORE DATABASE [$Database] FROM DISK = N'<pad>\$bakName'
               WITH REPLACE, RECOVERY, STATS = 10;
             (evt. MOVE '...' TO '...' als de datafile-paden anders zijn)
  Secrets  : user-secrets\secrets.json terugzetten in
             %APPDATA%\Microsoft\UserSecrets\$secId\

DPAPI-CAVEAT
  De kolommen *Encrypted (ClientKey, ClientSecret, ApiKey, OAuth-tokens, maatwerksleutels)
  zijn versleuteld met Windows DPAPI, scope LocalMachine - gebonden aan DEZE machine.
  Restore op DEZELFDE machine: werkt.
  Restore op een ANDERE machine: die waarden zijn niet te ontsleutelen en moeten opnieuw
  via de portal ingevoerd worden. De rest van de data blijft gewoon bruikbaar.
"@
Set-Content -Path (Join-Path $dest 'MANIFEST.txt') -Value $manifest -Encoding UTF8

# -- Afronden ---------------------------------------------------------------
if ($ZipAll) {
    $zip = "$dest.zip"
    Compress-Archive -Path (Join-Path $dest '*') -DestinationPath $zip -Force
    Remove-Item $dest -Recurse -Force
    Write-Host "`nKlaar: $zip  ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
} else {
    Write-Host "`nKlaar: $dest"
    Get-ChildItem $dest | Select-Object Name, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 2) } } | Format-Table -AutoSize
}
