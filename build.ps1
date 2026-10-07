$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$bt = Join-Path $PSScriptRoot 'tools\sdk\android-15'
$platform = 'tools\sdk\android-35\android.jar'
$javaHomeTask = if ($env:JAVA_HOME) { $env:JAVA_HOME } else { 'C:\Program Files\Java\jdk-21.0.12' }
if (!(Test-Path $platform)) { throw 'Missing SDK: see tools/download.cjs and README.md' }
function Check-Build { if ($LASTEXITCODE -ne 0) { throw "Build command failed: $LASTEXITCODE" } }
$buildRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'build'))
foreach ($name in @('classes','dex')) {
 $cleanPath = [IO.Path]::GetFullPath((Join-Path $buildRoot $name))
 if (!$cleanPath.StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid build cleanup path' }
 if (Test-Path -LiteralPath $cleanPath) { Remove-Item -LiteralPath $cleanPath -Recurse -Force }
}
New-Item -ItemType Directory -Force build,build/classes,build/dex,dist | Out-Null
node scripts/bundle.cjs
Check-Build
& "$bt\aapt2.exe" compile --dir app/src/main/res -o build/resources.zip
Check-Build
& "$bt\aapt2.exe" link -o build/base.apk --manifest app/src/main/AndroidManifest.xml -I $platform build/resources.zip
Check-Build
$sources = Get-ChildItem -LiteralPath app/src/main/java/com/yunjian/riichi -Filter '*.java' | ForEach-Object FullName
& "$javaHomeTask\bin\javac.exe" -encoding UTF-8 -source 8 -target 8 -classpath $platform -d build/classes $sources
Check-Build
& "$javaHomeTask\bin\jar.exe" cf build/classes.jar -C build/classes .
Check-Build
& "$javaHomeTask\bin\java.exe" -cp "$bt\lib\d8.jar" com.android.tools.r8.D8 --min-api 26 --lib $platform --output build/dex build/classes.jar
Check-Build
Copy-Item build/base.apk build/unsigned.apk -Force
& "$javaHomeTask\bin\jar.exe" uf build/unsigned.apk -C build/dex classes.dex -C app/src/main assets
Check-Build
# Store WAV clips uncompressed so Android can open and seek asset file descriptors.
& "$javaHomeTask\bin\jar.exe" uf0 build/unsigned.apk -C app/src/main assets/voice
Check-Build
# jar preserves forward slashes for nested assets on Windows; validate before signing.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$assetRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'app/src/main/assets'))
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $buildRoot 'unsigned.apk'))
try {
 foreach ($assetFile in Get-ChildItem -LiteralPath $assetRoot -Recurse -File) {
  $entryName = 'assets/' + [IO.Path]::GetRelativePath($assetRoot, $assetFile.FullName).Replace('\','/')
  if (!$archive.GetEntry($entryName)) { throw "Missing APK asset: $entryName" }
 }
} finally { $archive.Dispose() }
& "$bt\zipalign.exe" -f -p 4 build/unsigned.apk build/aligned.apk
Check-Build
if (!(Test-Path build/local-signing.jks)) {
 & "$javaHomeTask\bin\keytool.exe" -genkeypair -keystore build/local-signing.jks -storepass local-riichi -keypass local-riichi -alias local -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=LAN Riichi Local Build' -noprompt
 Check-Build
}
& "$javaHomeTask\bin\java.exe" -jar "$bt\lib\apksigner.jar" sign --ks build/local-signing.jks --ks-pass pass:local-riichi --key-pass pass:local-riichi --out dist/yunjian-riichi-0.4.0.apk build/aligned.apk
Check-Build
& "$javaHomeTask\bin\java.exe" -jar "$bt\lib\apksigner.jar" verify --verbose dist/yunjian-riichi-0.4.0.apk
Check-Build
Get-FileHash -Algorithm SHA256 dist/yunjian-riichi-0.4.0.apk | Format-List

