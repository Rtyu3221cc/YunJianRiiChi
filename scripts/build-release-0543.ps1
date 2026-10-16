# Re-record the final UI, build/sign Android, and assemble verified artifacts.
$ErrorActionPreference='Stop'
$releaseRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $releaseRoot
$releasePython=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$releaseEditor='D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe'
if(Get-NetTCPConnection -State Listen -LocalPort 8765 -ErrorAction SilentlyContinue){throw 'Preview port occupied'}
$env:RIICHI_AUTO_PREVIEW_OUTPUT='../build/ai-settlement-preview/render-0.5.43'
Remove-Item -LiteralPath Env:RIICHI_AUTO_PREVIEW_SCENARIOS -ErrorAction SilentlyContinue
$previewHost=Start-Process -FilePath (Get-Command node).Source -ArgumentList 'scripts/ai-settlement-preview.cjs' -WorkingDirectory $releaseRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput 'build/preview-host-0543.log' -RedirectStandardError 'build/preview-host-errors-0543.log'
try{
 Write-Output 'Recording four final Unity presentation workflows'
 $previewEditor=Start-Process -FilePath $releaseEditor -ArgumentList @('-batchmode','-projectPath',($releaseRoot+'/unity'),'-executeMethod','YunJian.Riichi.Editor.AutoSettlementPreview.Run','-logFile',($releaseRoot+'/build/preview-0543.log')) -WindowStyle Hidden -PassThru -Wait
 if($previewEditor.ExitCode -ne 0 -or !(Select-String -LiteralPath 'build/preview-0543.log' -SimpleMatch 'AUTO SETTLEMENT PREVIEW COMPLETE' -Quiet)){throw 'Unity flow recording failed'}
}finally{if(!$previewHost.HasExited){Stop-Process -Id $previewHost.Id -Force}}
$env:RIICHI_REVIEW_RELEASE='0.5.43'
$videoEncoder=Start-Process -FilePath $releasePython -ArgumentList 'scripts/package-ai-settlement-preview.py' -WorkingDirectory $releaseRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput 'build/video-package-0543.log' -RedirectStandardError 'build/video-package-errors-0543.log'
Write-Output 'Four recordings complete; encoding videos and building Android APK'
& (Join-Path $PSScriptRoot 'verify-unity.ps1') -Mode Android -EditorPath $releaseEditor
& $releasePython scripts/verify-unity-apk.py > build/apk-verify-0543.log
if($LASTEXITCODE -ne 0){throw 'Final APK validation failed'}
$videoEncoder.WaitForExit()
if($videoEncoder.ExitCode -ne 0){throw 'Video encoding/verification failed'}
& $releasePython scripts/finalize-release-0543.py > build/finalize-0543.log
if($LASTEXITCODE -ne 0){throw 'Release evidence generation failed'}
& $releasePython scripts/package-release-0543.py > build/package-0543.log
if($LASTEXITCODE -ne 0){throw 'Release package verification failed'}
Write-Output 'UNITY 0.5.43 SIGNED APK AND VERIFIED RELEASE PACKAGES COMPLETE'
