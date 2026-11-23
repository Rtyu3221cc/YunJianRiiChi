$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $repo 'build\phone-test-r24\stage\unity'
$here=Join-Path $repo 'build\rank-stick-r10\review'
[IO.Directory]::CreateDirectory($here) | Out-Null
$files=@([pscustomobject]@{path='unity/Assets/Scripts/ResourceRoomCoordinator.cs'},[pscustomobject]@{path='unity/Assets/Scripts/RoomProtocol.cs'},[pscustomobject]@{path='unity/Assets/Scripts/PlaylistAudio.cs'},[pscustomobject]@{path='unity/Assets/Scripts/PlayerTitles.cs'},[pscustomobject]@{path='unity/Assets/Scripts/PlayerRecord.cs'},[pscustomobject]@{path='unity/Assets/Scripts/LobbyController.cs'},[pscustomobject]@{path='unity/Assets/Scripts/LobbyController.PlayerInfo.cs'},[pscustomobject]@{path='unity/Assets/Scripts/TableEffects.cs'},[pscustomobject]@{path='unity/Assets/Scripts/YakumanCinematic.cs'},[pscustomobject]@{path='unity/Assets/Editor/ProjectSetup.cs'},[pscustomobject]@{path='unity/Assets/Scripts/MatchIntroduction.cs'},[pscustomobject]@{path='unity/Assets/Scripts/RiichiClient.cs'})
$files+=@('ResultPresentation.cs','SettlementAudio.cs','WardrobeModelVisual.cs','TableRenderer.cs') | ForEach-Object {[pscustomobject]@{path=('unity/Assets/Scripts/'+$_)}}
$backup=@{}
foreach($file in $files | Where-Object {$_.path.StartsWith('unity/')} ){$path=Join-Path $stage $file.path.Substring(6);$backup[$path]=[IO.File]::ReadAllBytes($path)}
$settings=Join-Path $stage 'ProjectSettings\ProjectSettings.asset';$backup[$settings]=[IO.File]::ReadAllBytes($settings)
$profile=Join-Path $stage 'Assets\Resources\Majsoul' ;@(Get-ChildItem -LiteralPath (Join-Path $profile 'Profile') -Recurse -File; Get-ChildItem -LiteralPath (Join-Path $profile 'Settlement/RankCalc') -Recurse -File -ErrorAction SilentlyContinue) | ForEach-Object {$backup[$_.FullName]=[IO.File]::ReadAllBytes($_.FullName)}
$catalog=Get-Content -Raw -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-windows.json') | ConvertFrom-Json
$keys=[Collections.Generic.HashSet[string]]::new()
function Add-ReviewPack([string]$key){if($keys.Add($key)){foreach($dep in ($catalog.packs | Where-Object key -eq $key).dependencies){Add-ReviewPack $dep}}}
foreach($pack in $catalog.items | Where-Object category -eq 'stick'){Add-ReviewPack $pack.pack}
$extraPaths=[Collections.Generic.HashSet[string]]::new()
foreach($asset in $catalog.addresses | Where-Object {$keys.Contains($_.pack)}){
 $null=$extraPaths.Add($asset.source);$null=$extraPaths.Add($asset.source+'.meta');$parent=[IO.Path]::GetDirectoryName($asset.source)
 while($parent -and $parent -ne 'Assets'){$meta=$parent+'.meta';if(Test-Path -LiteralPath (Join-Path $repo ('unity/'+$meta))){$null=$extraPaths.Add($meta)};$parent=[IO.Path]::GetDirectoryName($parent)}
}
$extra=@($extraPaths)
$extraRoot=Join-Path $stage 'Assets\WardrobePacks'
if(Test-Path -LiteralPath $extraRoot){Get-ChildItem -LiteralPath $extraRoot -Recurse -File | ForEach-Object {$backup[$_.FullName]=[IO.File]::ReadAllBytes($_.FullName)}}
foreach($relative in $extra){$destination=Join-Path $stage $relative;if(Test-Path -LiteralPath $destination){$backup[$destination]=[IO.File]::ReadAllBytes($destination)}}
$recorder=Join-Path $stage 'Assets\Editor\RankStickR10Review.cs'
if(Test-Path -LiteralPath $recorder){throw 'Temporary recorder already exists'}
try {
 foreach($relative in $extra){$destination=Join-Path $stage $relative;[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null;if(Test-Path -LiteralPath $destination){Remove-Item -LiteralPath $destination};Copy-Item -LiteralPath (Join-Path $repo ('unity/'+$relative)) -Destination $destination}
 foreach($file in $files | Where-Object {$_.path.StartsWith('unity/')}){Copy-Item -LiteralPath (Join-Path $repo $file.path) -Destination (Join-Path $stage $file.path.Substring(6)) -Force}
 # Source copies are independent of the build cache's hardlinked assets.
 $sourceProfile=Join-Path $repo 'unity/Assets/Resources/Majsoul'
 @(Get-ChildItem -LiteralPath (Join-Path $sourceProfile 'Profile') -Recurse -File; Get-ChildItem -LiteralPath (Join-Path $sourceProfile 'Settlement/RankCalc') -Recurse -File; Get-Item -LiteralPath (Join-Path $sourceProfile 'Settlement/RankCalc.meta')) | ForEach-Object {
  $destination=Join-Path $profile $_.FullName.Substring($sourceProfile.Length+1)
  if(Test-Path -LiteralPath $destination){if((Get-FileHash -LiteralPath $_.FullName).Hash -eq (Get-FileHash -LiteralPath $destination).Hash){return};Remove-Item -LiteralPath $destination}
  [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
  Copy-Item -LiteralPath $_.FullName -Destination $destination
 }
 Copy-Item -LiteralPath (Join-Path $repo 'tests/RankStickR10Review.cs') -Destination $recorder -Force
 $env:YUNJIAN_PLAYERINFO_OUT=$here
 $env:YUNJIAN_R10_OUT=$here
 $env:YUNJIAN_R10_REPO=$repo
 $env:YUNJIAN_R10_PHASE="after"
 $env:YUNJIAN_PLAYERINFO_REPO=$repo
 $env:YUNJIAN_PLAYERINFO_ROLES=Join-Path $repo 'build\character-voice-r34\roles'
 $args='-batchmode -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.RankStickR10Review.Run -logFile "{1}"' -f $stage,(Join-Path $here 'unity-r10.log')
 $process=Start-Process -FilePath 'D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
 $process.Id | Set-Content -LiteralPath (Join-Path $here 'unity-r10.pid')
 if(-not $process.WaitForExit(600000)){Stop-Process -Id $process.Id;throw 'Seven fix review timed out'}
 if($process.ExitCode -ne 0){throw 'Seven fix review failed; inspect unity-r10.log'}
 foreach($name in @('title-art-source.json','achievement-source.json')){Copy-Item -LiteralPath (Join-Path $repo ('docs/reference/seven-fixes-r8/'+$name)) -Destination (Join-Path $here $name)}
 Get-Content -LiteralPath (Join-Path $here 'report.json') -TotalCount 4
} finally {
 foreach($path in $backup.Keys){[IO.File]::WriteAllBytes($path,$backup[$path])}
 $profileRoot=[IO.Path]::GetFullPath($profile)+[IO.Path]::DirectorySeparatorChar
 @(Get-ChildItem -LiteralPath (Join-Path $profile 'Profile') -Recurse -File; Get-ChildItem -LiteralPath (Join-Path $profile 'Settlement/RankCalc') -Recurse -File) | Where-Object {-not $backup.ContainsKey($_.FullName)} | ForEach-Object {if(-not $_.FullName.StartsWith($profileRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected temporary asset path'};Remove-Item -LiteralPath $_.FullName}
 foreach($path in @($recorder,($recorder+'.meta'))){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
 $extraPrefix=[IO.Path]::GetFullPath($extraRoot)+[IO.Path]::DirectorySeparatorChar
 Get-ChildItem -LiteralPath $extraRoot -Recurse -File | Where-Object {-not $backup.ContainsKey($_.FullName)} | ForEach-Object {if(-not $_.FullName.StartsWith($extraPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected fixture path'};Remove-Item -LiteralPath $_.FullName}
 $folderMeta=$extraRoot+'.meta';if((Test-Path -LiteralPath $folderMeta)-and -not $backup.ContainsKey($folderMeta)){Remove-Item -LiteralPath $folderMeta}
}
