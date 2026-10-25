$ErrorActionPreference='Stop'
$taskRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput=Join-Path $taskRepo 'build/interaction-070'
New-Item -ItemType Directory -Force $taskOutput | Out-Null
if(Get-NetTCPConnection -State Listen -LocalPort 8765 -ErrorAction SilentlyContinue){throw 'Port 8765 is occupied; leaving that service unchanged.'}
$taskNode=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
$env:RIICHI_HOST_SECRET='interaction-070';$env:RIICHI_PORT='8765';$env:RIICHI_STATE_FILE='build/interaction-070/state-'+[Guid]::NewGuid().ToString('N')+'.json'
$taskPreviewHost=Join-Path $taskOutput 'preview-host.cjs'
[IO.File]::WriteAllText($taskPreviewHost,"Math.random=()=>0.1;require('../../desktop.cjs');",[Text.UTF8Encoding]::new($false))
$taskServer=Start-Process -FilePath $taskNode -ArgumentList $taskPreviewHost -WorkingDirectory $taskRepo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskOutput 'host.log') -RedirectStandardError (Join-Path $taskOutput 'host-errors.log')
try{
    Start-Sleep -Milliseconds 700
    if($taskServer.HasExited){throw 'Preview host failed to start'}
    $taskUnity=Start-Process -FilePath 'D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-projectPath',($taskRepo+'/unity'),'-executeMethod','YunJian.Riichi.Editor.Interaction070Review.Run','-logFile',($taskOutput+'/runtime.log')) -WindowStyle Hidden -PassThru
    $taskUnity.WaitForExit();if($taskUnity.ExitCode -ne 0){throw 'Interaction preview failed; inspect runtime.log'}
    if(!(Select-String -LiteralPath (Join-Path $taskOutput 'runtime.log') -SimpleMatch 'INTERACTION 070 PREVIEW PASS ' -Quiet)){throw 'Preview completion marker absent'}
    Write-Output 'INTERACTION 070 PREVIEW PASS'
}finally{if(!$taskServer.HasExited){Stop-Process -Id $taskServer.Id -Force};$taskServer.WaitForExit()}
