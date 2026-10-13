param([Parameter(Mandatory=$true)][string]$EditorPath)
$ErrorActionPreference='Stop'
$rootHostTask=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$javaBinHostTask=Join-Path (Split-Path $EditorPath -Parent) 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
$androidJarHostTask=Join-Path $rootHostTask 'tools/sdk/android-35/android.jar'
if(!(Test-Path -LiteralPath $androidJarHostTask)){$androidJarHostTask=Join-Path (Split-Path $EditorPath -Parent) 'Data/PlaybackEngines/AndroidPlayer/SDK/platforms/android-36/android.jar'}
$compileHostTask=Join-Path $rootHostTask ('build/unity-host/compile-'+[Guid]::NewGuid().ToString('N'))
$sourceHostTask=Join-Path $compileHostTask 'src/com/yunjian/riichi/RoomService.java'
$classesHostTask=Join-Path $compileHostTask 'classes'
New-Item -ItemType Directory -Force -Path (Split-Path $sourceHostTask -Parent),$classesHostTask | Out-Null
$serviceHostTask=[IO.File]::ReadAllText((Join-Path $rootHostTask 'app/src/main/java/com/yunjian/riichi/RoomService.java')).Replace('new Intent(this,MainActivity.class)','getPackageManager().getLaunchIntentForPackage(getPackageName())')
[IO.File]::WriteAllText($sourceHostTask,$serviceHostTask,[Text.UTF8Encoding]::new($false))
& (Join-Path $javaBinHostTask 'javac.exe') -encoding UTF-8 -source 8 -target 8 -classpath $androidJarHostTask -d $classesHostTask $sourceHostTask (Join-Path $rootHostTask 'unity/Android/UnityHostBridge.java')
if($LASTEXITCODE -ne 0){throw 'Unity Android host Java compilation failed'}
& (Join-Path $javaBinHostTask 'jar.exe') cf (Join-Path $rootHostTask 'unity/Assets/Plugins/Android/riichi-host.androidlib/libs/riichi-host.jar') -C $classesHostTask .
if($LASTEXITCODE -ne 0){throw 'Unity Android host JAR packaging failed'}
Write-Output 'PASS: Unity phone-host JAR rebuilt from current Java sources.'
