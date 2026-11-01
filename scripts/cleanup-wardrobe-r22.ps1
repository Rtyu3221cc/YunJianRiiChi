$ErrorActionPreference='Stop'
$repoTask=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$batchTask=Join-Path $repoTask 'build/wardrobe-r22'
$publishedTask=Join-Path $batchTask 'apk-published.json'
if (!(Test-Path -LiteralPath $publishedTask)) { throw 'Finish and verify R22 publication before deleting its build checkout.' }
$beforeTask=Get-PSDrive C,D | ForEach-Object { @{name=$_.Name;freeBytes=$_.Free} }
$targetsTask=@(Join-Path $batchTask 'stage'; Join-Path $batchTask 'clients')
$removedTask=@()
foreach ($targetTask in $targetsTask) {
 if (!(Test-Path -LiteralPath $targetTask)) { continue }
 $resolvedTask=(Resolve-Path -LiteralPath $targetTask).Path
 $expectedTask=[IO.Path]::GetFullPath($batchTask).TrimEnd('\')+'\'
 if (!$resolvedTask.StartsWith($expectedTask,[StringComparison]::OrdinalIgnoreCase)) { throw "Cleanup path escapes R22: $resolvedTask" }
 if ((Get-Item -LiteralPath $resolvedTask).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Cleanup root is a reparse point' }
 $linksTask=Get-ChildItem -LiteralPath $resolvedTask -Force -Recurse | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }
 if ($linksTask) { throw 'A reparse point needs explicit review before recursive cleanup' }
 Remove-Item -LiteralPath $resolvedTask -Recurse -Force
 $removedTask+=$resolvedTask
}
foreach ($platformTask in @('android','windows')) {
 $catalogTask=Get-Content -LiteralPath (Join-Path $repoTask "unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-$platformTask.json") -Raw | ConvertFrom-Json
 $keepTask=@{};foreach ($packTask in $catalogTask.packs) { if (!$packTask.builtin) { $keepTask[$packTask.file]=$true } }
 $folderTask=(Resolve-Path -LiteralPath (Join-Path $batchTask "packs/$platformTask")).Path
 if (!$folderTask.StartsWith([IO.Path]::GetFullPath($batchTask).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Pack folder is outside R22' }
 foreach ($fileTask in Get-ChildItem -LiteralPath $folderTask -File -Filter '*.bundle') {
  if (!$keepTask.ContainsKey($fileTask.Name)) { Remove-Item -LiteralPath $fileTask.FullName -Force; $removedTask+=$fileTask.FullName }
 }
}
$afterTask=Get-PSDrive C,D | ForEach-Object { @{name=$_.Name;freeBytes=$_.Free} }
@{before=$beforeTask;after=$afterTask;removed=$removedTask;preserved=@('original APK','reference bundles and decoded local evidence','signing material','dist releases and previews','published fingerprint bundles','maintained unity source')} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $batchTask 'cleanup.json') -Encoding utf8
Write-Output 'R22 verified temporary files cleaned; maintained source and published resource bundles retained.'
Get-PSDrive C,D | Select-Object Name,@{Name='FreeGiB';Expression={[math]::Round($_.Free/1GB,2)}}
