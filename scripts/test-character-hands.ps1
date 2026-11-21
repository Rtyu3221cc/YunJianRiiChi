param([string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe',[string]$OutputFolder='character-hands-r37')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $repo 'build/phone-test-r24/stage/unity'
$output=Join-Path $repo ('build/'+$OutputFolder)
New-Item -ItemType Directory -Path $output -Force | Out-Null
if(-not (Test-Path -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/CharacterHands/catalog.json'))){throw 'Import character hands first'}
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/CharacterHands') -Destination (Join-Path $stage 'Assets/Resources/Majsoul') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/CharacterHands.meta') -Destination (Join-Path $stage 'Assets/Resources/Majsoul/CharacterHands.meta')
foreach($file in @('CharacterHands.cs','CharacterHands.cs.meta','CharacterHandVisual.cs','CharacterHandVisual.cs.meta','WardrobeCatalog.cs','SourcePlaybackContracts.cs','TableEffects.cs','TableLook.cs','WardrobeMotionPreview.cs','LobbyController.Characters.cs')){Copy-Item -LiteralPath (Join-Path $repo ('unity/Assets/Scripts/'+$file)) -Destination (Join-Path $stage ('Assets/Scripts/'+$file))}
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/CharacterHand.shader') -Destination (Join-Path $stage 'Assets/Resources/Majsoul/CharacterHand.shader') -Force
Copy-Item -LiteralPath (Join-Path $repo 'tests/CharacterHandReview.cs') -Destination (Join-Path $stage 'Assets/Editor/CharacterHandReview.cs')
foreach($name in @('runtime.failed.txt','runtime-report.json')){if(Test-Path -LiteralPath (Join-Path $output $name)){Remove-Item -LiteralPath (Join-Path $output $name)}}
$prior=$env:YUNJIAN_HAND_REPO
$priorOutput=$env:YUNJIAN_HAND_OUTPUT
try{
    $env:YUNJIAN_HAND_REPO=$repo
    $env:YUNJIAN_HAND_OUTPUT=$output
    $arguments='-batchmode -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.CharacterHandReview.Run -logFile "{1}"' -f $stage,(Join-Path $output 'unity-runtime.log')
    $process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    [IO.File]::WriteAllText((Join-Path $output 'unity-runtime.pid'),$process.Id.ToString())
    $deadline=[DateTime]::UtcNow.AddMinutes(5)
    while(-not $process.WaitForExit(1000)){if([DateTime]::UtcNow -ge $deadline){Stop-Process -Id $process.Id;throw 'Character hand review timed out'}}
    if($process.ExitCode -ne 0){throw ('Character hand review failed; see '+(Join-Path $output 'runtime.failed.txt'))}
    Get-Content -LiteralPath (Join-Path $output 'runtime-report.json') -Raw | ConvertFrom-Json | Select-Object checks,styles,variants,clips,roles,@{n='tableCases';e={$_.cases.Count}},androidDeviceTested
}finally{$env:YUNJIAN_HAND_REPO=$prior;$env:YUNJIAN_HAND_OUTPUT=$priorOutput}
