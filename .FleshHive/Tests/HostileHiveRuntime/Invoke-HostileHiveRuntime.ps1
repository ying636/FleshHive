[CmdletBinding()]
param([int]$TimeoutSeconds = 600, [string]$Language = 'English')

$ErrorActionPreference = 'Stop'
$taskRepo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$taskSourceGame = (Resolve-Path -LiteralPath (Join-Path $taskRepo '../..')).Path
& dotnet build (Join-Path $taskRepo '.FleshHive/FleshHive/FleshHive.csproj') --no-restore -v quiet
if ($LASTEXITCODE -ne 0) { throw 'FleshHive build failed.' }
& dotnet build (Join-Path $PSScriptRoot 'HostileHiveRuntime.csproj') -c Release -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Verification assembly build failed.' }
$taskRunRoot = Join-Path $env:TEMP ('FH-HostileHive-' + [guid]::NewGuid().ToString('N'))
$taskGameDirectory = Join-Path $taskRunRoot 'Game'
$taskModsDirectory = Join-Path $taskGameDirectory 'Mods'
$null = New-Item -ItemType Directory -Path $taskModsDirectory
$null = New-Item -ItemType Directory -Path (Join-Path $taskRunRoot 'Config')
foreach ($taskFileName in @('RimWorldWin64.exe','UnityPlayer.dll','UnityCrashHandler64.exe','steam_appid.txt')) {
    Copy-Item -LiteralPath (Join-Path $taskSourceGame $taskFileName) -Destination (Join-Path $taskGameDirectory $taskFileName)
}
foreach ($taskDirectoryName in @('Data','MonoBleedingEdge','RimWorldWin64_Data')) {
    $null = New-Item -ItemType Junction -Path (Join-Path $taskGameDirectory $taskDirectoryName) -Target (Join-Path $taskSourceGame $taskDirectoryName)
}
foreach ($taskModDirectory in Get-ChildItem -LiteralPath (Join-Path $taskSourceGame 'Mods') -Directory) {
    $taskDestination = Join-Path $taskModsDirectory $taskModDirectory.Name
    if ($taskModDirectory.FullName -eq $taskRepo) {
        $null = New-Item -ItemType Directory -Path $taskDestination
        foreach ($taskContent in @('About','1.6','Languages','Compatibility','Patches','LoadFolders.xml')) {
            Copy-Item -LiteralPath (Join-Path $taskRepo $taskContent) -Destination (Join-Path $taskDestination $taskContent) -Recurse
        }
        $null = New-Item -ItemType Junction -Path (Join-Path $taskDestination 'Textures') -Target (Join-Path $taskRepo 'Textures')
        $taskBridge = Join-Path $taskDestination '1.6/Assemblies/net48/ZZ.FH.HostileHiveTests.dll'
    } else {
        $null = New-Item -ItemType Junction -Path $taskDestination -Target $taskModDirectory.FullName
    }
}
if (-not $taskBridge.StartsWith($taskRunRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Test assembly target escaped isolated game.' }
@'
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData>
  <activeMods>
    <li>brrainz.harmony</li>
    <li>ludeon.rimworld</li>
    <li>ludeon.rimworld.royalty</li>
    <li>ludeon.rimworld.ideology</li>
    <li>ludeon.rimworld.biotech</li>
    <li>ludeon.rimworld.anomaly</li>
    <li>ludeon.rimworld.odyssey</li>
    <li>hailuan.hivecreatureframework</li>
    <li>hailuan.fleshhive</li>
  </activeMods>
</ModsConfigData>
'@ | Set-Content -LiteralPath (Join-Path $taskRunRoot 'Config/ModsConfig.xml') -Encoding utf8
@'
<?xml version="1.0" encoding="utf-8"?>
<PrefsData>
  <runInBackground>true</runInBackground>
  <fullscreen>false</fullscreen>
  <screenWidth>1280</screenWidth>
  <screenHeight>720</screenHeight>
  <langFolderName>English</langFolderName>
  <resetModsConfigOnCrash>false</resetModsConfigOnCrash>
</PrefsData>
'@ | Set-Content -LiteralPath (Join-Path $taskRunRoot 'Config/Prefs.xml') -Encoding utf8
[xml]$taskPrefs = Get-Content -LiteralPath (Join-Path $taskRunRoot 'Config/Prefs.xml') -Encoding UTF8 -Raw
$taskPrefs.PrefsData.langFolderName = $Language
$taskPrefs.Save((Join-Path $taskRunRoot 'Config/Prefs.xml'))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin/Release/net48/FH.HostileHiveTests.dll') -Destination $taskBridge
$taskProcess = $null
try {
    $taskProcess = Start-Process -FilePath (Join-Path $taskGameDirectory 'RimWorldWin64.exe') -ArgumentList @('-quicktest', ('-savedatafolder=' + $taskRunRoot), '-logFile', (Join-Path $taskRunRoot 'Player.log')) -WindowStyle Hidden -PassThru
    Write-Output "Verification PID=$($taskProcess.Id) Directory=$taskRunRoot"
    $taskDeadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    $taskPassed = Join-Path $taskRunRoot 'FH-HostileHiveTests/passed.txt'
    $taskFailure = Join-Path $taskRunRoot 'FH-HostileHiveTests/failure.txt'
    while (-not (Test-Path -LiteralPath $taskPassed)) {
        if (Test-Path -LiteralPath $taskFailure) { throw (Get-Content -LiteralPath $taskFailure -Encoding utf8 -Raw) }
        $taskProcess.Refresh()
        if ($taskProcess.HasExited) { throw "Verification process exited. Inspect $taskRunRoot/Player.log" }
        if ([datetime]::UtcNow -gt $taskDeadline) { throw "Verification timed out. Inspect $taskRunRoot/Player.log" }
        Start-Sleep -Seconds 1
    }
    Get-Content -LiteralPath $taskPassed -Encoding utf8
} finally {
    $taskResults = Join-Path $PSScriptRoot 'Results'
    $null = New-Item -ItemType Directory -Path $taskResults -Force
    foreach ($taskOldResult in Get-ChildItem -LiteralPath $taskResults -File) {
        Remove-Item -LiteralPath $taskOldResult.FullName
    }
    if (Test-Path -LiteralPath (Join-Path $taskRunRoot 'FH-HostileHiveTests')) {
        Copy-Item -Path (Join-Path $taskRunRoot 'FH-HostileHiveTests/*') -Destination $taskResults -Force
    }
    if (Test-Path -LiteralPath (Join-Path $taskRunRoot 'Player.log')) {
        Copy-Item -LiteralPath (Join-Path $taskRunRoot 'Player.log') -Destination (Join-Path $taskResults 'Player.log')
    }
    [ordered]@{
        passed = Test-Path -LiteralPath $taskPassed
        checks = if (Test-Path -LiteralPath $taskPassed) { @(Get-Content -LiteralPath $taskPassed -Encoding UTF8).Count } else { 0 }
        assemblySHA256 = (Get-FileHash -LiteralPath (Join-Path $taskRepo '1.6/Assemblies/net48/FleshHive.dll') -Algorithm SHA256).Hash
        completed = [datetimeoffset]::Now.ToString('O')
        testDirectory = $taskRunRoot
        isolatedGame = $true
        language = $Language
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskResults 'manifest.json') -Encoding utf8
    if ($taskProcess -and -not $taskProcess.HasExited) {
        $taskIdentity = Get-CimInstance Win32_Process -Filter "ProcessId=$($taskProcess.Id)"
        if ($taskIdentity.CommandLine -notlike "*$taskRunRoot*") { throw 'Verification process identity mismatch.' }
        Stop-Process -Id $taskProcess.Id
    }
    if (Test-Path -LiteralPath $taskBridge) { Remove-Item -LiteralPath $taskBridge }
}
