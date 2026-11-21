param(
    [string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe',
    [string]$StagePath='build/phone-test-r24/stage/unity',
    [string]$ReviewRoot='build/character-subtitles-r36',
    [string]$RoleCache='build/character-voice-r34/roles'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
function Absolute([string]$value){if([IO.Path]::IsPathRooted($value)){return [IO.Path]::GetFullPath($value)};return [IO.Path]::GetFullPath((Join-Path $repo $value))}
$stage=Absolute $StagePath;$output=Absolute $ReviewRoot;$roles=Absolute $RoleCache
if(-not (Test-Path -LiteralPath (Join-Path $stage 'Assets/Scenes/Table.unity'))){throw 'Cached Unity stage required'}
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach($name in @('report.json','failed.txt')){if(Test-Path -LiteralPath (Join-Path $output $name)){Remove-Item -LiteralPath (Join-Path $output $name)}}
$directory=Get-Content -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/CharacterDirectory/packs-windows.json') -Raw | ConvertFrom-Json
$pack=$directory.packs | Where-Object key -eq 'weilai'
$bundle=Join-Path $roles ('weilai/'+$pack.sha256+'.bundle')
if(-not (Test-Path -LiteralPath $bundle) -or (Get-Item -LiteralPath $bundle).Length -ne $pack.bytes -or (Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pack.sha256){throw 'Verified Weilai Windows test bundle required'}
foreach($file in @('CharacterVoiceCaption.cs','CharacterVoiceCaption.cs.meta','CharacterVoiceCatalog.cs','CharacterVoices.cs','LobbyController.Subtitles.cs','LobbyController.Subtitles.cs.meta','LobbyController.cs','LobbyController.Dorm.cs')){Copy-Item -LiteralPath (Join-Path $repo ('unity/Assets/Scripts/'+$file)) -Destination (Join-Path $stage ('Assets/Scripts/'+$file))}
Copy-Item -LiteralPath (Join-Path $repo 'tests/CharacterSubtitleReview.cs') -Destination (Join-Path $stage 'Assets/Editor/CharacterSubtitleReview.cs')
$priorRoot=$env:YUNJIAN_SUBTITLE_OUT;$priorRoles=$env:YUNJIAN_SUBTITLE_ROLES
try{
    $env:YUNJIAN_SUBTITLE_OUT=$output;$env:YUNJIAN_SUBTITLE_ROLES=$roles
    $arguments='-batchmode -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.CharacterSubtitleReview.Run -logFile "{1}"' -f $stage,(Join-Path $output 'unity-subtitles.log')
    $process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    [IO.File]::WriteAllText((Join-Path $output 'unity-subtitles.pid'),$process.Id.ToString())
    $deadline=[DateTime]::UtcNow.AddMinutes(4)
    while(-not $process.WaitForExit(1000)){if([DateTime]::UtcNow -ge $deadline){Stop-Process -Id $process.Id;throw 'Subtitle review timed out'}}
    if($process.ExitCode -ne 0){throw ('Subtitle review failed; see '+(Join-Path $output 'failed.txt'))}
    $report=Get-Content -LiteralPath (Join-Path $output 'report.json') -Raw | ConvertFrom-Json
    if($report.screenshots.Count -ne 6 -or @($report.screenshots | Where-Object {-not $_.sourcePlaying -or -not $_.text}).Count){throw 'Incomplete subtitle capture'}
    $report | Select-Object checks,catalogLines,@{n='screenshots';e={$_.screenshots.Count}},androidDeviceTested
}finally{$env:YUNJIAN_SUBTITLE_OUT=$priorRoot;$env:YUNJIAN_SUBTITLE_ROLES=$priorRoles}
