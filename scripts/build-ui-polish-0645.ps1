param([string]$PythonPath='C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $repo 'build/phone-test-r24/stage/unity'
$out=Join-Path $repo 'build/release-0.6.45'
[IO.Directory]::CreateDirectory($out)|Out-Null
function Copy-Current([string]$source,[string]$destination){
 if(Test-Path -LiteralPath $destination){if((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $destination).Hash){return};Remove-Item -LiteralPath $destination}
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))|Out-Null
 Copy-Item -LiteralPath $source -Destination $destination
}
$saber=Join-Path $stage 'Assets/Scripts/CharacterPackService.cs'
if((Get-FileHash -LiteralPath $saber).Hash.ToLowerInvariant() -ne 'd9932fd079cbc77c07b14308956b6d3fa34484f7b0b9addbc28086e9e97454e0'){throw 'Unexpected Saber build overlay; preserve and inspect it'}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo 'unity/Assets/Scripts') -File){if($file.Name -in @('CharacterPackService.cs','CharacterPackService.cs.meta')){continue};Copy-Current $file.FullName (Join-Path $stage ('Assets/Scripts/'+$file.Name))}
$assets=Join-Path $repo 'unity/Assets/Resources/Majsoul'
foreach($folder in @('Profile','CharacterDirectory','WardrobeDirectory','Settlement/RankCalc','NativeReview/NovR10','RoomCustomization','CharacterHands')){
 $source=Join-Path $assets $folder
 foreach($file in Get-ChildItem -LiteralPath $source -File -Recurse){Copy-Current $file.FullName (Join-Path $stage ('Assets/Resources/Majsoul/'+$folder+'/'+$file.FullName.Substring($source.Length+1)))}
 if(Test-Path -LiteralPath ($source+'.meta')){Copy-Current ($source+'.meta') (Join-Path $stage ('Assets/Resources/Majsoul/'+$folder+'.meta'))}
}
foreach($relative in @('Lobby/Sprites/Create/room_create_computer_text.png','Lobby/Sprites/Create/room_create_computer_text.png.meta','Characters/framing.json','Characters/voice-directory.json','Characters/voice-lines.json','Characters/native-fonts.json','Characters/native-fonts.json.meta','Characters/Fonts/character-name-brush.ttf','Characters/Fonts/character-name-brush.ttf.meta','Lobby/Fonts/brush.ttf','Lobby/Fonts/brush.ttf.meta','CharacterHand.shader','CharacterHand.shader.meta','TableLook/Sprites/notitle.png','TableLook/Sprites/notitle.png.meta','Wardrobe/catalog.json','Wardrobe/StaticDecoration.shader','Wardrobe/StaticDecoration.shader.meta','Actions/playback-contracts.json','Settings/music.json')){Copy-Current (Join-Path $assets $relative) (Join-Path $stage ('Assets/Resources/Majsoul/'+$relative))}
foreach($relative in @('Assets/Editor/ProjectSetup.cs','Assets/Editor/LobbyAssetImport.cs','Assets/Editor/CharacterHandImport.cs','ProjectSettings/ProjectSettings.asset','Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js','Assets/Plugins/Android/riichi-host.androidlib/assets/ai.js','Assets/Plugins/Android/riichi-host.androidlib/assets/client.js')){Copy-Current (Join-Path $repo ('unity/'+$relative)) (Join-Path $stage $relative)}
Copy-Current (Join-Path $repo 'tests/Release0645Build.cs') (Join-Path $stage 'Assets/Editor/Release0645Build.cs')
$previousJava=$env:RIICHI_JAVA
$env:RIICHI_JAVA='D:\Unity\Lib\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe'
$env:YUNJIAN_RELEASE_REVIEW=$out
try {
 $arguments='-batchmode -buildTarget Android -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.Release0645Build.Run -logFile "{1}"' -f $stage,(Join-Path $out 'android-0.6.45.log')
 $process=Start-Process -FilePath 'D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.Id | Set-Content -LiteralPath (Join-Path $out 'unity.pid')
 $process.WaitForExit()
 if($process.ExitCode -ne 0){throw '0.6.45 Android build failed; inspect android-0.6.45.log'}
 $apk=Join-Path $stage 'Builds/yunjian-riichi-unity-0.6.45.apk'
 & (Join-Path $repo 'scripts/sign-unity-apk.ps1') -ApkPath $apk -EditorPath 'D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe'
 if($LASTEXITCODE -ne 0){throw 'Signing failed'}
 $final=Join-Path $repo 'dist/yunjian-riichi-unity-0.6.45.apk';Copy-Item -LiteralPath $apk -Destination $final
 [IO.File]::WriteAllText((Join-Path $repo 'dist/SHA256-yunjian-riichi-unity-0.6.45.txt'),(Get-FileHash -LiteralPath $final).Hash.ToLowerInvariant()+'  yunjian-riichi-unity-0.6.45.apk'+"`n",[Text.UTF8Encoding]::new($false))
 foreach($check in @(@('verify-character-audio-apk.py','audio'),@('verify-feedback-apk-r41.py','feature'),@('verify-profile-apk.py','profile'),@('verify-rank-content-apk.py','rank-content'),@('verify-room-feedback-apk.py','room-feedback'),@('verify-bot-room-apk.py','bot-room'),@('verify-ui-polish-apk.py','ui-polish'))){
  & $PythonPath -X utf8 (Join-Path $repo ('scripts/'+$check[0])) $final (Join-Path $repo ('dist/unity-0.6.45-'+$check[1]+'-verification.json'))
  if($LASTEXITCODE -ne 0){throw ('APK verification failed: '+$check[1])}
 }
} finally {$env:RIICHI_JAVA=$previousJava}
