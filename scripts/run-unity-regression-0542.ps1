param([string]$StageNames='table,lobby,room')
$ErrorActionPreference='Stop'
$releaseRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $releaseRoot
$releaseNode=(Get-Command node).Source
$releaseEditor='D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe'
$releaseOutput=Join-Path $releaseRoot 'build/release-0542'
New-Item -ItemType Directory -Force -Path $releaseOutput | Out-Null
& $releaseNode scripts/unity-fixtures.cjs
if($LASTEXITCODE -ne 0){throw 'Public fixture generation failed'}
foreach($variable in Get-ChildItem Env:){if($variable.Name -match '^RIICHI_(REVIEW_|FIDELITY_|LOOK_|NORTH_ONLY|NATIVE_PREVIEW|OPPONENT_HAND_PREVIEW)'){Remove-Item -LiteralPath ('Env:'+ $variable.Name)}}
$stages=@(
 @{Name='table';Host='unity-local-validation';Method='YunJian.Riichi.Editor.PlayValidation.Run';Marker='RIICHI UNITY PLAY PASS';Report='build/unity-smoke/passed.txt'},
 @{Name='lobby';Host='lobby-validation-1013';Method='YunJian.Riichi.Editor.LobbyPlayValidation.Run';Marker='RIICHI LOBBY 1013 PASS';Report='build/lobby-reference-1013/rendered/passed.txt'},
 @{Name='room';Host='lobby-release-0542';Method='YunJian.Riichi.Editor.LobbyReleaseValidation.Run';Marker='LOBBY RELEASE ONLY PASS';Report='build/lobby-release-0.5.41/passed.txt'}
)
foreach($stage in ($stages | Where-Object {$_.Name -in $StageNames.Split(',')})){
 if(Get-NetTCPConnection -State Listen -LocalPort 8765 -ErrorAction SilentlyContinue){throw 'Port 8765 is already in use; refusing to touch another room service'}
 $env:RIICHI_HOST_SECRET=$stage.Host;$env:RIICHI_PORT='8765';$env:RIICHI_STATE_FILE='build/release-0542/state-'+$stage.Name+'-'+[Guid]::NewGuid().ToString('N')+'.json'
 $server=Start-Process -FilePath $releaseNode -ArgumentList 'desktop.cjs' -WorkingDirectory $releaseRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $releaseOutput ($stage.Name+'-host.log')) -RedirectStandardError (Join-Path $releaseOutput ($stage.Name+'-host-errors.log'))
 try{
  Start-Sleep -Milliseconds 700
  if($server.HasExited){throw 'Dedicated room service did not start'}
  $log=Join-Path $releaseOutput ($stage.Name+'.log')
  Write-Output ('Starting Unity '+$stage.Name+' regression')
  $arguments=@('-batchmode','-projectPath',($releaseRoot+'/unity'),'-executeMethod',$stage.Method,'-logFile',$log)
  $editor=Start-Process -FilePath $releaseEditor -ArgumentList $arguments -WorkingDirectory ($releaseRoot+'/unity') -WindowStyle Hidden -PassThru -Wait
  if($editor.ExitCode -ne 0){throw ('Unity '+$stage.Name+' failed; see '+$log)}
  if(!(Select-String -LiteralPath $log -SimpleMatch $stage.Marker -Quiet)){throw ('Unity '+$stage.Name+' exited without pass marker')}
  Copy-Item -LiteralPath (Join-Path $releaseRoot $stage.Report) -Destination (Join-Path $releaseOutput ($stage.Name+'-passed.txt')) -Force
  Get-Content -LiteralPath (Join-Path $releaseOutput ($stage.Name+'-passed.txt'))
  Write-Output ('Completed Unity '+$stage.Name+' regression')
 }finally{if(!$server.HasExited){Stop-Process -Id $server.Id -Force};$server.WaitForExit()}
}
Write-Output 'SELECTED UNITY REGRESSION STAGES PASSED'
