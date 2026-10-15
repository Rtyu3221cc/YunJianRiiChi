param([string]$EditorPath='D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$lobbyRootTask=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lobbyLogTask=Join-Path $lobbyRootTask 'build/lobby-reference-1013/play.log'
$env:RIICHI_HOST_SECRET='lobby-validation-1013'
$lobbyArgsTask=@('-batchmode','-projectPath',('"'+(Join-Path $lobbyRootTask 'unity')+'"'),'-executeMethod','YunJian.Riichi.Editor.LobbyPlayValidation.Run','-logFile',('"'+$lobbyLogTask+'"'))
$lobbyProcessTask=Start-Process -FilePath $EditorPath -ArgumentList $lobbyArgsTask -WorkingDirectory (Join-Path $lobbyRootTask 'unity') -WindowStyle Hidden -PassThru -Wait
if($lobbyProcessTask.ExitCode -ne 0){throw "Lobby Play validation failed. See $lobbyLogTask"}
if(!(Select-String -LiteralPath $lobbyLogTask -SimpleMatch 'RIICHI LOBBY 1013 PASS' -Quiet)){throw 'No lobby validation success marker'}
Get-Content -LiteralPath (Join-Path $lobbyRootTask 'build/lobby-reference-1013/rendered/passed.txt')
