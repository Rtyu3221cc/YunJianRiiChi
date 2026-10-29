param([string]$PlanPath=(Join-Path $PSScriptRoot '../build/cleanup-20261028/plan.json'))
$ErrorActionPreference='Stop'
$taskPlan=Get-Content -LiteralPath $PlanPath -Raw -Encoding UTF8 | ConvertFrom-Json
$taskRoot=[IO.Path]::GetFullPath('D:\MyWorkSpace\YunJianRiichi_Codex').TrimEnd('\')
if($taskPlan.project -ne $taskRoot){throw 'Unexpected cleanup project'}
$taskEditor=Get-Process -Name Unity -ErrorAction SilentlyContinue | Where-Object {$_.Path -like '*\Editor\Unity.exe'}
if($taskEditor){throw 'Close the Unity Editor before clearing its project caches'}
$taskAllowed=@(
 'D:\MyWorkSpace\YunJianRiichi_Codex\unity\Library',
 'D:\MyWorkSpace\YunJianRiichi_Codex\unity\Builds',
 'D:\MyWorkSpace\YunJianRiichi_Codex\unity\Temp',
 'D:\MyWorkSpace\YunJianRiichi_Codex\unity\Obj',
 'D:\MyWorkSpace\YunJianRiichi\unity\Library',
 'D:\MyWorkSpace\YunJianRiichi\unity\Temp',
 'C:\Users\Rtyu3221\.gradle\caches\9.3.1',
 'C:\Users\Rtyu3221\AppData\Local\Temp\codex-riichi-build-82a62bbc1604'
)
$taskProtected=@($taskPlan.protected | ForEach-Object {[IO.Path]::GetFullPath($_.path)})
$taskValidated=@()
foreach($taskEntry in $taskPlan.directories){
 $taskTarget=[IO.Path]::GetFullPath($taskEntry.path).TrimEnd('\')
 if($taskAllowed -notcontains $taskTarget){throw ('Directory not allowlisted: '+$taskTarget)}
 foreach($taskKeep in $taskProtected){if($taskKeep.StartsWith($taskTarget+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'A protected artifact is inside a deletion target'}}
 if(Test-Path -LiteralPath $taskTarget){$taskItem=Get-Item -LiteralPath $taskTarget -Force;if($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Refusing reparse root '+$taskTarget)}}
 $taskValidated+=$taskTarget
}
$taskFiles=@()
foreach($taskEntry in $taskPlan.files){
 $taskTarget=[IO.Path]::GetFullPath($taskEntry.path)
 if(!$taskTarget.StartsWith($taskRoot+'\build\',[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskTarget) -ne '.apk' -or $taskTarget -match 'Majsoul|雀魂'){throw ('Invalid temporary APK path '+$taskTarget)}
 if($taskProtected -contains $taskTarget){throw 'Protected APK selected for deletion'}
 $taskFiles+=$taskTarget
}
$taskResults=@()
foreach($taskTarget in $taskValidated){
 if(!(Test-Path -LiteralPath $taskTarget)){continue}
 Write-Output ('Removing validated generated directory: '+$taskTarget)
 $taskErrors=@();Remove-Item -LiteralPath $taskTarget -Recurse -Force -ErrorAction SilentlyContinue -ErrorVariable taskErrors
 $taskResults+=@{path=$taskTarget;removed=!(Test-Path -LiteralPath $taskTarget);errors=@($taskErrors | Select-Object -First 5 | ForEach-Object {$_.ToString()})}
}
foreach($taskTarget in $taskFiles){
 if(!(Test-Path -LiteralPath $taskTarget)){continue}
 $taskErrors=@();Remove-Item -LiteralPath $taskTarget -Force -ErrorAction SilentlyContinue -ErrorVariable taskErrors
 $taskResults+=@{path=$taskTarget;removed=!(Test-Path -LiteralPath $taskTarget);errors=@($taskErrors | Select-Object -First 3 | ForEach-Object {$_.ToString()})}
}
$taskReport=Join-Path (Split-Path $PlanPath -Parent) 'deletion-results.json'
$taskResults | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $taskReport -Encoding UTF8
Write-Output ('Removed '+@($taskResults | Where-Object {$_.removed}).Count+' of '+$taskResults.Count+' generated targets; retained sources and distribution artifacts.')
