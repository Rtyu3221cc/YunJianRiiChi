$ErrorActionPreference='Stop'
$releaseRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $releaseRoot
$releaseEditor='D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe'
$releasePython=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
New-Item -ItemType Directory -Force -Path 'build/release-0545' | Out-Null
& (Join-Path $PSScriptRoot 'verify-unity.ps1') -Mode Validate -EditorPath $releaseEditor
node scripts/unity-fixtures.cjs
if($LASTEXITCODE -ne 0){throw 'Fixture generation failed'}
$settlement=Start-Process -FilePath $releaseEditor -ArgumentList @('-batchmode','-projectPath',($releaseRoot+'/unity'),'-executeMethod','YunJian.Riichi.Editor.WinSettlementValidation.Run','-logFile',($releaseRoot+'/build/release-0545/settlement.log')) -WindowStyle Hidden -PassThru -Wait
if($settlement.ExitCode -ne 0 -or !(Select-String -LiteralPath 'build/release-0545/settlement.log' -SimpleMatch 'WIN SETTLEMENT VALIDATION PASS' -Quiet)){throw 'Settlement regression failed'}
Copy-Item -LiteralPath 'build/win-settlement-0543/passed.txt' -Destination 'build/release-0545/settlement-passed.txt' -Force
& (Join-Path $PSScriptRoot 'run-unity-regression-0542.ps1')
foreach($stage in @('table','lobby','room')){Copy-Item -LiteralPath ('build/release-0542/'+$stage+'-passed.txt') -Destination ('build/release-0545/'+$stage+'-passed.txt') -Force}
& (Join-Path $PSScriptRoot 'verify-unity.ps1') -Mode Android -EditorPath $releaseEditor
& $releasePython scripts/verify-unity-apk.py *> build/release-0545/apk-verify.log
if($LASTEXITCODE -ne 0){throw 'APK verification failed'}
Write-Output 'UNITY 0.5.45 TESTS AND APK COMPLETE'
