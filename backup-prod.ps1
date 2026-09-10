<#
    backup-prod.ps1  -  Volledige back-up van de GoDutchSnelStartWebApp PRODUCTIE-omgeving
                        op deze server, inclusief de IIS-configuratie en de gedeployde sites.

    Maakt een tijdstempel-map met:
      GoDutchSnelStartDb.bak   : volledige COPY_ONLY database-back-up
      db-programmability.sql   : alle stored procedures / functions / views / triggers als tekst
      iis\                     : appcmd-config-back-up (applicationHost/administration/redirection),
                                 losse kopie van applicationHost.config, sites/apppools als XML,
                                 en sites-summary.txt (bindings + SSL-thumbprints, leesbaar)
      sites\<site>.zip         : volledige fysieke map per IIS-site (incl. appsettings.Production.json,
                                 web.config, gecompileerde DLLs); 'logs' wordt overgeslagen
      certs\<thumbprint>.pfx   : alleen met -CertPassword; de aan 443 gebonden certificaten
      repo.bundle              : alleen met -IncludeSource; 'git bundle --all' van de broncode
      MANIFEST.txt             : inhoud + restore-instructies + DPAPI-caveat

    MOET ALS ADMINISTRATOR DRAAIEN (leest C:\inetpub, applicationHost.config, exporteert certs).

    Gebruik:
      .\backup-prod.ps1
      .\backup-prod.ps1 -ZipAll
      .\backup-prod.ps1 -CertPassword (Read-Host -AsSecureString) -IncludeSource -ZipAll
      .\backup-prod.ps1 -Sites padans-api,padans-portal
      .\backup-prod.ps1 -SqlUser sa -SqlPassword '...'      # SQL-auth i.p.v. Windows-auth
#>

[CmdletBinding()]
param(
    [string]  $BackupRoot   = 'C:\Backups\GoDutchProd',
    [string[]]$Sites,                                    # leeg = alle sites met 'padans' in de naam
    [string]  $SqlServer    = 'localhost',
    [string]  $Database     = 'GoDutchSnelStartDb',
    [string]  $SqlUser      = '',                        # leeg = Windows-auth (-E)
    [string]  $SqlPassword  = '',
    [securestring]$CertPassword,                         # nodig om de SSL-certificaten (.pfx) mee te nemen
    [string]  $RepoPath     = 'C:\__Claude\GoDutchSnelStartWebApp',
    [switch]  $IncludeSource,
    [switch]  $ZipAll
)

$ErrorActionPreference = 'Stop'

# -- Elevatie-check ---------------------------------------------------------------
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
          ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw 'Dit script moet als Administrator draaien (IIS-config en C:\inetpub lezen). Open PowerShell "Als administrator".'
}

Import-Module WebAdministration -ErrorAction Stop

$stamp   = Get-Date -Format 'yyyyMMdd-HHmmss'
$dest    = Join-Path $BackupRoot "GoDutchProd-$stamp"
$appcmd  = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $dest 'iis')   -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $dest 'sites') -Force | Out-Null
Write-Host "Back-up doel: $dest`n"

# sqlcmd-basis (met DB-context; Windows-auth staat anders default op master)
$sqlAuth = if ([string]::IsNullOrWhiteSpace($SqlUser)) { @('-E') } else { @('-U', $SqlUser, '-P', $SqlPassword) }
$sqlBase = @('-S', $SqlServer) + $sqlAuth + @('-d', $Database, '-C')
function Invoke-Sql([string]$query, [string[]]$extra = @()) {
    $out = & sqlcmd @($sqlBase + @('-b') + $extra + @('-Q', $query))
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd faalde (exit $LASTEXITCODE) voor: $query" }
    return $out
}

# certificateHash op een IIS-binding kan een byte[] of al een hex-string zijn
function Get-BindingThumb($hash) {
    if ($null -eq $hash) { return $null }
    if ($hash -is [byte[]]) { return (($hash | ForEach-Object { $_.ToString('X2') }) -join '') }
    return ("$hash").Replace(' ', '').Trim()
}

# -- 1. Database-back-up --------------------------------------------------------
Write-Host "[1/5] Database '$Database' back-uppen..."
$bakName = "$Database.bak"
$defPath = (Invoke-Sql "SET NOCOUNT ON; SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))" @('-h', '-1', '-W')).Trim()
$sqlBak  = Join-Path $defPath $bakName
Invoke-Sql "BACKUP DATABASE [$Database] TO DISK = N'$sqlBak' WITH COPY_ONLY, INIT, FORMAT, STATS = 25" | Out-Null
Move-Item -LiteralPath $sqlBak -Destination (Join-Path $dest $bakName) -Force
Write-Host ('      ' + [math]::Round((Get-Item (Join-Path $dest $bakName)).Length / 1MB, 1) + ' MB')

# -- 2. DB-programmability (leesbaar, ter referentie) -------------------------
Write-Host '[2/5] DB-programmability scripten (db-programmability.sql)...'
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
    Write-Warning "DB-programmability scripten mislukt: $_  (zit wel volledig in $bakName)"
}

# -- 3. IIS-configuratie -----------------------------------------------------
Write-Host '[3/5] IIS-configuratie back-uppen...'
$iisDir = Join-Path $dest 'iis'

# 3a. appcmd config-back-up (applicationHost / administration / redirection / schema)
$acBackupName = "GoDutchProd-$stamp"
& $appcmd add backup $acBackupName | Out-Null
$acSrc = Join-Path $env:windir "system32\inetsrv\backup\$acBackupName"
if (Test-Path $acSrc) {
    Copy-Item $acSrc (Join-Path $iisDir 'config-backup') -Recurse -Force
    & $appcmd delete backup $acBackupName | Out-Null    # bronset opruimen; kopie zit in de back-up
}

# 3b. losse kopie van de kernbestanden (handig om te diffen)
Copy-Item (Join-Path $env:windir 'system32\inetsrv\config\applicationHost.config') $iisDir -Force
Copy-Item (Join-Path $env:windir 'system32\inetsrv\config\administration.config')   $iisDir -Force -ErrorAction SilentlyContinue

# 3c. sites + apppools als portable XML
& $appcmd list site    /config /xml | Out-File (Join-Path $iisDir 'sites.xml')    -Encoding utf8
& $appcmd list apppool  /config /xml | Out-File (Join-Path $iisDir 'apppools.xml') -Encoding utf8

# 3d. leesbare samenvatting incl. SSL-thumbprints
$summary = New-Object System.Text.StringBuilder
[void]$summary.AppendLine("IIS-samenvatting  -  $(Get-Date -Format 'yyyy-MM-dd HH:mm')  -  $env:COMPUTERNAME`r`n")
foreach ($s in (Get-Website)) {
    [void]$summary.AppendLine("SITE  $($s.Name)   state=$($s.State)   pool=$($s.applicationPool)")
    [void]$summary.AppendLine("  physicalPath : $($s.PhysicalPath)")
    foreach ($b in $s.Bindings.Collection) {
        $line = "  binding      : $($b.protocol)  $($b.bindingInformation)"
        if ($b.protocol -eq 'https' -and $b.certificateHash) {
            $thumb = Get-BindingThumb $b.certificateHash
            $line += "   cert=$thumb  store=$($b.certificateStoreName)"
        }
        [void]$summary.AppendLine($line)
    }
    [void]$summary.AppendLine('')
}
foreach ($p in (Get-ChildItem IIS:\AppPools)) {
    [void]$summary.AppendLine("APPPOOL  $($p.Name)   runtime=$($p.managedRuntimeVersion)   pipeline=$($p.managedPipelineMode)   identity=$($p.processModel.identityType)   state=$($p.state)")
}
$summary.ToString() | Set-Content (Join-Path $iisDir 'sites-summary.txt') -Encoding UTF8

# -- 4. Gedeployde site-mappen -----------------------------------------------
Write-Host '[4/5] Site-mappen inpakken...'
$allSites = Get-Website
if ($Sites) {
    $targetSites = $allSites | Where-Object { $Sites -contains $_.Name }
} else {
    $targetSites = $allSites | Where-Object { $_.Name -like '*padans*' }
}
foreach ($s in $targetSites) {
    $path = [Environment]::ExpandEnvironmentVariables($s.PhysicalPath)
    if (-not (Test-Path $path)) { Write-Warning "  $($s.Name): map niet gevonden ($path)"; continue }
    $staging = Join-Path $env:TEMP "gd-site-$($s.Name)-$stamp"
    $null = robocopy $path $staging /MIR /XD logs /NFL /NDL /NP /R:1 /W:1
    if ($LASTEXITCODE -ge 8) { Write-Warning "  robocopy $($s.Name) faalde (exit $LASTEXITCODE)"; Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue; continue }
    $zip = Join-Path $dest "sites\$($s.Name).zip"
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -Force
    Remove-Item $staging -Recurse -Force
    Write-Host ("      $($s.Name)  " + [math]::Round((Get-Item $zip).Length / 1MB, 1) + ' MB')
}

# -- 5. Certificaten / TLS-setup (optioneel) + broncode (optioneel) + manifest --
if ($CertPassword) {
    Write-Host '[5/5] TLS-setup back-uppen + MANIFEST...'
    $certDir = Join-Path $dest 'certs'
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null

    # 5a. Probeer elk gebonden certificaat als .pfx te exporteren (lukt alleen als
    #     de private key exportable is gemarkeerd).
    $done = @{}
    foreach ($s in (Get-Website)) {
        foreach ($b in $s.Bindings.Collection) {
            if ($b.protocol -eq 'https' -and $b.certificateHash) {
                $thumb = Get-BindingThumb $b.certificateHash
                if (-not $thumb -or $done.ContainsKey($thumb)) { continue }
                $done[$thumb] = $true
                $store = if ($b.certificateStoreName) { $b.certificateStoreName } else { 'My' }
                try {
                    Export-PfxCertificate -Cert "Cert:\LocalMachine\$store\$thumb" `
                        -FilePath (Join-Path $certDir "$thumb.pfx") -Password $CertPassword -Force | Out-Null
                    Write-Host "      cert $thumb geexporteerd (.pfx)"
                } catch {
                    Write-Host "      cert $thumb : private key niet-exportabel - .pfx overgeslagen ($($_.Exception.Message))"
                    # publieke cert (zonder key) wel meenemen, is nuttig voor keten/thumbprint
                    try {
                        Export-Certificate -Cert "Cert:\LocalMachine\$store\$thumb" `
                            -FilePath (Join-Path $certDir "$thumb.cer") -Type CERT -Force | Out-Null
                    } catch { }
                }
            }
        }
    }

    # 5b. win-acme (Let's Encrypt) state - de echte back-up van de TLS-setup als de
    #     keys niet-exportabel zijn: hiermee kan de cert opnieuw uitgegeven/gebonden worden.
    $winAcme = 'C:\ProgramData\win-acme'
    if (Test-Path $winAcme) {
        $staging = Join-Path $env:TEMP "gd-winacme-$stamp"
        $null = robocopy $winAcme $staging /MIR /NFL /NDL /NP /R:1 /W:1
        Compress-Archive -Path (Join-Path $staging '*') -DestinationPath (Join-Path $certDir 'win-acme-state.zip') -Force
        Remove-Item $staging -Recurse -Force
        Write-Host ('      win-acme-state.zip  ' + [math]::Round((Get-Item (Join-Path $certDir 'win-acme-state.zip')).Length / 1MB, 1) + ' MB')
    }
}
else {
    Write-Host '[5/5] MANIFEST... (geen -CertPassword: TLS-setup NIET meegenomen)'
}

if ($IncludeSource -and (Test-Path (Join-Path $RepoPath '.git'))) {
    & git -C $RepoPath bundle create (Join-Path $dest 'repo.bundle') --all 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Host ('      repo.bundle  ' + [math]::Round((Get-Item (Join-Path $dest 'repo.bundle')).Length / 1MB, 1) + ' MB') }
}

$manifest = @"
GoDutchSnelStartWebApp - PRODUCTIE back-up
Gemaakt : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  op  $env:COMPUTERNAME
Database: $Database  op  $SqlServer

INHOUD
  $bakName   Volledige COPY_ONLY back-up van de productiedatabase.
                          Bevat ook alle stored procedures/functions/views/triggers.
  db-programmability.sql   Die programmability-objecten als leesbare T-SQL (referentie).
  iis\config-backup\       'appcmd'-configuratieset (applicationHost/administration/redirection/schema).
  iis\applicationHost.config   Losse kopie ter inzage/diff.
  iis\sites.xml / apppools.xml  Sites en app pools als portable XML.
  iis\sites-summary.txt    Leesbare lijst: fysieke paden, bindings, SSL-thumbprints, app pools.
  sites\<site>.zip         Volledige fysieke map per site (incl. appsettings.Production.json,
                          web.config en gecompileerde DLLs). 'logs' is weggelaten.
  certs\<thumb>.pfx        SSL-certificaat MET private key (alleen als de key exportabel is).
  certs\<thumb>.cer        Publiek certificaat zonder key (fallback als .pfx niet kan).
  certs\win-acme-state.zip C:\ProgramData\win-acme: Let's Encrypt renewal-config + ACME-account.
                          Hiermee is de cert opnieuw uit te geven/te binden.
  repo.bundle             Git bundle van de broncode (alleen met -IncludeSource).

BEVAT GEHEIMEN - beveiligd bewaren (DB-connectiestrings met sa-wachtwoord en de SnelStart
subscription key staan in de appsettings.Production.json binnen sites\*.zip; .pfx-bestanden
zijn met wachtwoord versleuteld).

RESTORE (op DEZELFDE server)
  Database :
    RESTORE DATABASE [$Database] FROM DISK = N'<pad>\$bakName' WITH REPLACE, RECOVERY, STATS = 10;
  Site-bestanden :
    Stop de app pool  ->  leeg de fysieke map  ->  pak sites\<site>.zip erin uit  ->  start de app pool.
    (Fysieke paden staan in iis\sites-summary.txt.)
  IIS-configuratie :
    Optie A (alles): kopieer iis\config-backup\GoDutchProd-<stamp> terug naar
      %windir%\system32\inetsrv\backup\<naam>\  en draai:  appcmd restore backup "<naam>"
    Optie B (gericht): pas bindings/pools handmatig aan a.d.h.v. iis\sites-summary.txt / sites.xml.
  Certificaten (Let's Encrypt via win-acme) :
    Als certs\<thumb>.pfx bestaat:
      Import-PfxCertificate -FilePath certs\<thumb>.pfx -CertStoreLocation Cert:\LocalMachine\WebHosting -Password <pwd>
      Daarna de https-binding opnieuw aan het thumbprint koppelen (zie sites-summary.txt).
    Als alleen certs\win-acme-state.zip bestaat (private key was niet-exportabel):
      Pak win-acme-state.zip uit naar C:\ProgramData\win-acme, installeer win-acme,
      en draai:  wacs.exe --renew --force   (geeft de cert opnieuw uit en herbindt de sites).
    De scheduled task "win-acme renew (...)" vernieuwt de cert daarna weer automatisch.

DPAPI-CAVEAT
  De kolommen *Encrypted in de database (maatwerksleutels, client secrets, API keys, OAuth-tokens)
  zijn versleuteld met Windows DPAPI scope LocalMachine - gebonden aan DEZE server.
  Restore op dezelfde server: werkt.
  Restore op een ANDERE server: die waarden zijn onleesbaar en moeten opnieuw via de portal
  ingevoerd worden. De overige data blijft bruikbaar.
"@
Set-Content -Path (Join-Path $dest 'MANIFEST.txt') -Value $manifest -Encoding UTF8

# -- Afronden ----------------------------------------------------------------
if ($ZipAll) {
    $zip = "$dest.zip"
    Compress-Archive -Path (Join-Path $dest '*') -DestinationPath $zip -Force
    Remove-Item $dest -Recurse -Force
    Write-Host "`nKlaar: $zip  ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
} else {
    Write-Host "`nKlaar: $dest"
    Get-ChildItem $dest -Recurse -File | Select-Object @{n = 'Pad'; e = { $_.FullName.Substring($dest.Length + 1) } }, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 2) } } | Format-Table -AutoSize
}
