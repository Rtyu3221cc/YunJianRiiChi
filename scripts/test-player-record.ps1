param(
    [string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe',
    [string]$StagePath='build/phone-test-r24/stage/unity',
    [string]$ReviewRoot='build/player-info-r1/review',
    [string]$RoleCache='build/character-voice-r34/roles'
)
# Rank rules, local record and player-info page in the cached stage project.
# Copies only this feature's files; the stage's CharacterPackService override is untouched.
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
function Absolute([string]$value){if([IO.Path]::IsPathRooted($value)){return [IO.Path]::GetFullPath($value)};return [IO.Path]::GetFullPath((Join-Path $repo $value))}
$stage=Absolute $StagePath;$output=Absolute $ReviewRoot;$roles=Absolute $RoleCache
if(-not (Test-Path -LiteralPath (Join-Path $stage 'Assets/Scenes/Table.unity'))){throw 'Cached Unity stage required'}
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach($name in @('report.json','failed.txt')){if(Test-Path -LiteralPath (Join-Path $output $name)){Remove-Item -LiteralPath (Join-Path $output $name)}}
$files=@('RankRules.cs','PlayerRecord.cs','PlayerTitles.cs','LobbyController.PlayerInfo.cs','LobbyController.cs','MatchIntroduction.cs','ResultPresentation.cs','RiichiClient.cs','RiichiClient.Session.cs','RoomProtocol.cs')
foreach($file in $files){Copy-Item -LiteralPath (Join-Path $repo ('unity/Assets/Scripts/'+$file)) -Destination (Join-Path $stage ('Assets/Scripts/'+$file));$meta=Join-Path $repo ('unity/Assets/Scripts/'+$file+'.meta');if(Test-Path -LiteralPath $meta){Copy-Item -LiteralPath $meta -Destination (Join-Path $stage ('Assets/Scripts/'+$file+'.meta'))}}
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/Profile') -Destination (Join-Path $stage 'Assets/Resources/Majsoul') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/Profile.meta') -Destination (Join-Path $stage 'Assets/Resources/Majsoul/Profile.meta')
Copy-Item -LiteralPath (Join-Path $repo 'tests/PlayerRecordReview.cs') -Destination (Join-Path $stage 'Assets/Editor/PlayerRecordReview.cs')
$priorOut=$env:YUNJIAN_PLAYERINFO_OUT;$priorRoles=$env:YUNJIAN_PLAYERINFO_ROLES
try{
    $env:YUNJIAN_PLAYERINFO_OUT=$output;$env:YUNJIAN_PLAYERINFO_ROLES=if(Test-Path -LiteralPath $roles){$roles}else{''}
    $arguments='-batchmode -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.PlayerRecordReview.Run -logFile "{1}"' -f $stage,(Join-Path $output 'unity.log')
    $process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $deadline=[DateTime]::UtcNow.AddMinutes(6)
    while(-not $process.WaitForExit(1000)){if([DateTime]::UtcNow -ge $deadline){Stop-Process -Id $process.Id;throw 'Player info review timed out'}}
    if($process.ExitCode -ne 0){throw ('Player info review failed; see '+(Join-Path $output 'failed.txt'))}
    $report=Get-Content -LiteralPath (Join-Path $output 'report.json') -Raw | ConvertFrom-Json
    $report | Select-Object checks,@{n='screenshots';e={$_.screenshots.Count}},androidDeviceTested
}finally{$env:YUNJIAN_PLAYERINFO_OUT=$priorOut;$env:YUNJIAN_PLAYERINFO_ROLES=$priorRoles}
