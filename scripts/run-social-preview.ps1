$ErrorActionPreference='Stop'
$taskRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput=Join-Path $taskRepo 'build/social-preview-1022'
New-Item -ItemType Directory -Force $taskOutput | Out-Null
if(Get-NetTCPConnection -State Listen -LocalPort 8765 -ErrorAction SilentlyContinue){throw 'Port 8765 is occupied; the preview will not stop another room service.'}
$taskNode=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
$env:RIICHI_HOST_SECRET='social-preview-1022';$env:RIICHI_PORT='8765';$env:RIICHI_STATE_FILE='build/social-preview-1022/state-'+[Guid]::NewGuid().ToString('N')+'.json'
$taskServer=Start-Process -FilePath $taskNode -ArgumentList 'desktop.cjs' -WorkingDirectory $taskRepo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskOutput 'host.log') -RedirectStandardError (Join-Path $taskOutput 'host-errors.log')
try{
    Start-Sleep -Milliseconds 700
    if($taskServer.HasExited){throw 'Preview room host did not start'}
    $taskUnity=Start-Process -FilePath 'D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-projectPath',($taskRepo+'/unity'),'-executeMethod','YunJian.Riichi.Editor.SocialPreferencesReview.Run','-logFile',($taskOutput+'/runtime.log')) -WindowStyle Hidden -PassThru
    $taskUnity.WaitForExit();if($taskUnity.ExitCode -ne 0){throw 'Social preview failed; inspect runtime.log'}
    if(!(Select-String -LiteralPath (Join-Path $taskOutput 'runtime.log') -SimpleMatch 'SOCIAL PREFERENCES PREVIEW PASS ' -Quiet)){throw 'Preview completion marker absent'}
    Write-Output 'SOCIAL PREVIEW PASS'
}finally{if(!$taskServer.HasExited){Stop-Process -Id $taskServer.Id -Force};$taskServer.WaitForExit()}
