param([string]$Case='',[string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe',[string]$Output='')
$ErrorActionPreference='Stop';$repo=Split-Path $PSScriptRoot -Parent;$stage=Join-Path $repo 'build/phone-test-r24/stage/unity';$output=if($Output){$Output}else{Join-Path $repo 'build/yakuman-alignment-r40'}
$env:YUNJIAN_FILM_OUTPUT=($output -replace '\\','/')+'/capture'
$directory=Get-Content -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-windows.json') -Raw | ConvertFrom-Json
$packKey=($directory.items | Where-Object id -eq 'mjp_rosered').pack;$pack=$directory.packs | Where-Object key -eq $packKey
$source=$null
foreach($file in (& rg --files (Join-Path $repo 'build') -g '*.bundle')){if([IO.Path]::GetFileName($file) -notin @($pack.file,$pack.sha256+'.bundle')){continue};if((Get-Item -LiteralPath $file).Length -eq $pack.bytes -and (Get-FileHash -LiteralPath $file).Hash.ToLowerInvariant() -eq $pack.sha256){$source=$file;break}}
if(-not $source){throw 'Missing verified reference rose-red tile pack'}
$folder=Join-Path $repo ('build/feedback-1114-r39/wardrobes/'+$packKey);New-Item -ItemType Directory -Path $folder -Force | Out-Null;$target=Join-Path $folder ($pack.sha256+'.bundle');if(-not (Test-Path -LiteralPath $target)){New-Item -ItemType HardLink -Path $target -Target $source | Out-Null};[IO.File]::WriteAllText($target+'.ok',$pack.sha256)
Copy-Item -LiteralPath (Join-Path $repo 'tests/YakumanFilmCapture.cs') -Destination (Join-Path $stage 'Assets/Editor/YakumanFilmCapture.cs') -Force
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Scripts/YakumanCinematic.cs') -Destination (Join-Path $stage 'Assets/Scripts/YakumanCinematic.cs') -Force
Copy-Item -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/Characters/framing.json') -Destination (Join-Path $stage 'Assets/Resources/Majsoul/Characters/framing.json') -Force
Get-ChildItem -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/NativeReview/YakumanR39') -Filter '*-motion.json' | Copy-Item -Destination (Join-Path $stage 'Assets/Resources/Majsoul/NativeReview/YakumanR39') -Force
$env:YUNJIAN_1114_REPO=$repo;$env:YUNJIAN_FILM_CASE=$Case
New-Item -ItemType Directory -Path (Join-Path $output 'capture') -Force | Out-Null
foreach($name in @('failed.txt','complete.txt')){Remove-Item -LiteralPath (Join-Path $output ('capture/'+$name)) -ErrorAction SilentlyContinue}
$args='-batchmode -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.YakumanFilmCapture.Run -logFile "{1}"' -f $stage,(Join-Path $output 'capture.log')
$p=Start-Process -FilePath $EditorPath -ArgumentList $args -WindowStyle Hidden -PassThru;$p.Id | Set-Content (Join-Path $output 'capture.pid')
while(-not $p.WaitForExit(1000)){}
if($p.ExitCode -ne 0){throw 'Film capture failed; inspect capture/failed.txt'}
Get-Content -LiteralPath (Join-Path $output 'capture/complete.txt')
