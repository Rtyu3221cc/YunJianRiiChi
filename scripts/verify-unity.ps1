param([string]$EditorPath, [ValidateSet('Validate','Play','Android')][string]$Mode = 'Validate')
$ErrorActionPreference = 'Stop'
$projectTask = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../unity'))
$originalProjectTask = $projectTask
if ($Mode -eq 'Android') {
 # Sync the maintained offline engine before copying/staging or invoking Unity.
 # This prevents a successful native build from silently packaging stale room rules.
 $hostSourceTask = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../app/src/main/assets'))
 $hostTargetTask = Join-Path $projectTask 'Assets/Plugins/Android/riichi-host.androidlib/assets'
 foreach ($hostFileTask in (Get-ChildItem -LiteralPath $hostSourceTask -File -Recurse)) {
  # Windows PowerShell 5.1 has no [IO.Path]::GetRelativePath.
  $hostRelativeTask = $hostFileTask.FullName.Substring($hostSourceTask.TrimEnd('\','/').Length + 1)
  $hostDestinationTask = Join-Path $hostTargetTask $hostRelativeTask
  New-Item -ItemType Directory -Force (Split-Path $hostDestinationTask -Parent) | Out-Null
  Copy-Item -LiteralPath $hostFileTask.FullName -Destination $hostDestinationTask -Force
 }
}
if (!$EditorPath) {
 $candidateTask = Get-ChildItem -LiteralPath 'C:/Program Files/Unity/Hub/Editor' -Filter 'Unity.exe' -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
 # Unity Hub's per-version install location; this machine keeps editors on D:.
 if (!$candidateTask) { $candidateTask = Get-Item -LiteralPath 'D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe' -ErrorAction SilentlyContinue }
 if ($candidateTask) { $EditorPath = $candidateTask.FullName }
 if (!$EditorPath) {
  $hubPathTask = Join-Path $env:APPDATA 'UnityHub/secondaryInstallPath.json'
  if (Test-Path -LiteralPath $hubPathTask) {
   $installPathTask = Get-Content -LiteralPath $hubPathTask -Raw | ConvertFrom-Json
   $candidateTask = Get-ChildItem -LiteralPath $installPathTask -Filter 'Unity.exe' -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
   if ($candidateTask) { $EditorPath = $candidateTask.FullName }
  }
 }
}
if (!$EditorPath -or !(Test-Path -LiteralPath $EditorPath)) { throw 'Unity Editor is not installed yet. Pass -EditorPath after installing it.' }
if($Mode -eq 'Android'){ & (Join-Path $PSScriptRoot 'compile-unity-host.ps1') -EditorPath $EditorPath }
if ($Mode -eq 'Android' -and $projectTask -match '[^\x00-\x7F]') {
 # Android/IL2CPP rejects Unicode project paths. Keep the maintained project in place.
 $pathHashTask = (-join ([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($projectTask)) | ForEach-Object { $_.ToString('x2') })).Substring(0,12)
 $projectTask = Join-Path ([IO.Path]::GetTempPath()) "codex-riichi-build-$pathHashTask"
 if ($projectTask -match '[^\x00-\x7F]') { throw 'TEMP also contains non-ASCII characters; set TEMP to an ASCII directory before Android build.' }
 New-Item -ItemType Directory -Force $projectTask | Out-Null
 foreach ($folderTask in @('Assets','Packages','ProjectSettings')) {
  & robocopy (Join-Path $originalProjectTask $folderTask) (Join-Path $projectTask $folderTask) /E /NFL /NDL /NJH /NJS /NP | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "Failed staging Unity $folderTask" }
 }
 Write-Output "Android build staged at $projectTask"
}
$logTask = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../build/unity-$($Mode.ToLower()).log"))
New-Item -ItemType Directory -Force (Split-Path $logTask -Parent) | Out-Null
# Unity.exe is a Windows GUI application; explicitly wait for its actual exit code.
$methodTask = switch ($Mode) { 'Play' { 'YunJian.Riichi.Editor.PlayValidation.Run' }; 'Android' { 'YunJian.Riichi.Editor.ProjectSetup.BuildAndroid' }; default { 'YunJian.Riichi.Editor.ProjectValidation.Validate' } }
$argumentsTask = @('-batchmode','-projectPath',('"' + $projectTask + '"'),'-executeMethod',$methodTask,'-logFile',('"' + $logTask + '"'))
if ($Mode -ne 'Play') { $argumentsTask += @('-nographics','-quit') }
if ($Mode -eq 'Android') { $argumentsTask += @('-buildTarget','Android') }
$unityTask = Start-Process -FilePath $EditorPath -ArgumentList $argumentsTask -WorkingDirectory $projectTask -WindowStyle Hidden -PassThru -Wait
if ($unityTask.ExitCode -ne 0) { throw "Unity validation failed (exit $($unityTask.ExitCode)). See $logTask" }
if ($Mode -ne 'Android') {
 $markerTask = if ($Mode -eq 'Play') { 'RIICHI UNITY PLAY PASS' } else { 'RIICHI UNITY VALIDATION PASS' }
 if (!(Select-String -LiteralPath $logTask -SimpleMatch $markerTask -Quiet)) { throw 'Unity exited without the validation success marker.' }
} elseif (!(Test-Path -LiteralPath (Join-Path $projectTask 'Builds/yunjian-riichi-unity-0.6.6.apk'))) { throw 'Unity did not produce the Android APK.' }
if ($Mode -eq 'Android' -and $projectTask -ne $originalProjectTask) {
 New-Item -ItemType Directory -Force (Join-Path $originalProjectTask 'Builds') | Out-Null
 Copy-Item -LiteralPath (Join-Path $projectTask 'Builds/yunjian-riichi-unity-0.6.6.apk') -Destination (Join-Path $originalProjectTask 'Builds/yunjian-riichi-unity-0.6.6.apk') -Force
}
if ($Mode -eq 'Android') {
 & (Join-Path $PSScriptRoot 'sign-unity-apk.ps1') -ApkPath (Join-Path $originalProjectTask 'Builds/yunjian-riichi-unity-0.6.6.apk') -EditorPath $EditorPath
}
Get-Content -LiteralPath $logTask -Tail 20
