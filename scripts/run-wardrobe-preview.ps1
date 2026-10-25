param([switch]$ParticleStudy)
$ErrorActionPreference='Stop'
$taskRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput=Join-Path $taskRepo $(if($ParticleStudy){'build/wardrobe-preview-r2-study'}else{'build/wardrobe-preview-r3'})
New-Item -ItemType Directory -Force $taskOutput | Out-Null
if(Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.Path -like '*\Editor\Unity.exe' }){throw 'An existing Unity editor is open. Leave it unchanged.'}
$taskMethod=if($ParticleStudy){'YunJian.Riichi.Editor.WardrobePreview.RunParticleStudy'}else{'YunJian.Riichi.Editor.WardrobePreview.Run'}
$taskUnity=Start-Process -FilePath 'D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-projectPath',($taskRepo+'/unity'),'-executeMethod',$taskMethod,'-logFile',($taskOutput+'/runtime.log')) -WindowStyle Hidden -PassThru
$taskUnity.WaitForExit()
if($taskUnity.ExitCode -ne 0){throw 'Wardrobe rendering stopped; inspect runtime.log'}
if(!(Select-String -LiteralPath (Join-Path $taskOutput 'runtime.log') -SimpleMatch 'WARDROBE PREVIEW CAPTURE COMPLETE ' -Quiet)){throw 'Wardrobe capture did not finish'}
Write-Output 'Wardrobe visual capture and table demonstration complete. See capture.json and gameplay-checks.txt.'
