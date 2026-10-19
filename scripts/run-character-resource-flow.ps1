$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskNode=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
$taskEditor='D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe'
$taskOutput=Join-Path $taskRoot 'build/character-resources-v1'
if(Get-NetTCPConnection -State Listen -LocalPort 8765 -ErrorAction SilentlyContinue){throw 'Port 8765 is already used; will not stop another room service'}
$env:RIICHI_HOST_SECRET='character-pack-review'
$env:RIICHI_PORT='8765'
$env:RIICHI_STATE_FILE='build/character-resources-v1/flow-state-'+[Guid]::NewGuid().ToString('N')+'.json'
$taskServer=Start-Process -FilePath $taskNode -ArgumentList 'desktop.cjs' -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskOutput 'flow-host.log') -RedirectStandardError (Join-Path $taskOutput 'flow-host-errors.log')
try {
    Start-Sleep -Milliseconds 700
    if($taskServer.HasExited){throw 'Dedicated resource test host failed to start'}
    $taskUnity=Start-Process -FilePath $taskEditor -ArgumentList @('-batchmode','-projectPath',($taskRoot+'/unity'),'-executeMethod','YunJian.Riichi.Editor.CharacterPackExperiment.Run','-logFile',($taskOutput+'/flow.log')) -WindowStyle Hidden -PassThru -Wait
    if($taskUnity.ExitCode -ne 0){throw 'Resource flow validation failed'}
    if(!(Select-String -LiteralPath (Join-Path $taskOutput 'flow.log') -SimpleMatch 'CHARACTER PACK EXPERIMENT PASS 37' -Quiet)){throw 'Resource flow pass marker absent'}
    Write-Output 'CHARACTER RESOURCE FLOW PASS 37'
} finally {
    if(!$taskServer.HasExited){Stop-Process -Id $taskServer.Id -Force}
    $taskServer.WaitForExit()
}
exit 0
