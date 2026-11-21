param([string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$monoRoot=Join-Path (Split-Path $EditorPath -Parent) 'Data\MonoBleedingEdge'
$mono=Join-Path $monoRoot 'bin\mono.exe'
$compiler=Join-Path $monoRoot 'lib\mono\4.5\csc.exe'
$output=Join-Path $root 'build\character-voice-r34\trigger-tests.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $output -Parent) | Out-Null
& $mono $compiler /nologo /langversion:latest "/out:$output" (Join-Path $root 'unity\Assets\Scripts\RoomProtocol.cs') (Join-Path $root 'unity\Assets\Scripts\CharacterVoiceTriggers.cs') (Join-Path $root 'tests\character-voice-triggers.cs')
if($LASTEXITCODE -ne 0){throw 'Character voice trigger compilation failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Character voice trigger checks failed'}
