$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$javaHomeTask = if ($env:JAVA_HOME) { $env:JAVA_HOME } else { 'C:\Program Files\Java\jdk-21.0.12' }
$plugin = 'unity/Assets/Plugins/Android/riichi-host.androidlib'
$assets = 'app/src/main/assets'
$resources = 'unity/Assets/Resources/Majsoul'
foreach ($dir in @("$plugin/libs", "$plugin/assets", "$resources/Tiles", "$resources/Portraits", "$resources/Table", "$resources/Voice", 'build/unity-host/classes','build/unity-host/src/com/yunjian/riichi')) { New-Item -ItemType Directory -Force $dir | Out-Null }
# Source copied on each sync; the Android and Unity hosts share one maintained engine.
Get-ChildItem -LiteralPath $assets -File | Copy-Item -Destination "$plugin/assets" -Force
foreach ($folder in @('tiles','portraits','voice','table')) { if (Test-Path "$assets/$folder") { Copy-Item -LiteralPath "$assets/$folder" -Destination "$plugin/assets" -Recurse -Force } }
Copy-Item "$assets/tiles/*.png" "$resources/Tiles" -Force
Copy-Item "$assets/portraits/*.png" "$resources/Portraits" -Force
if (Test-Path "$assets/table/cloth.png") { Copy-Item "$assets/table/cloth.png" "$resources/Table/cloth.png" -Force }
if (Test-Path "$assets/voice-manifest.json") {
 Copy-Item "$assets/voice-manifest.json" "$resources/voice-manifest.json" -Force
 Get-ChildItem "$assets/voice" -File | Where-Object { $_.Extension -in @('.wav','.mp3') } | Copy-Item -Destination "$resources/Voice" -Force
}
if (Test-Path "$assets/table/score-hub.png") { Copy-Item "$assets/table/score-hub.png" "$resources/Table/score-hub.png" -Force }
$service = [IO.File]::ReadAllText((Join-Path $PWD 'app/src/main/java/com/yunjian/riichi/RoomService.java'))
$service = $service.Replace('new Intent(this,MainActivity.class)', 'getPackageManager().getLaunchIntentForPackage(getPackageName())')
[IO.File]::WriteAllText((Join-Path $PWD 'build/unity-host/src/com/yunjian/riichi/RoomService.java'), $service)
& "$javaHomeTask/bin/javac.exe" -encoding UTF-8 -source 8 -target 8 -classpath tools/sdk/android-35/android.jar -d build/unity-host/classes build/unity-host/src/com/yunjian/riichi/RoomService.java unity/Android/UnityHostBridge.java
if ($LASTEXITCODE -ne 0) { throw 'Unity host Java compile failed' }
& "$javaHomeTask/bin/jar.exe" cf "$plugin/libs/riichi-host.jar" -C build/unity-host/classes .
if ($LASTEXITCODE -ne 0) { throw 'Unity host jar failed' }
Write-Output 'Unity resources and compiled Android phone-host plug-in synced.'
