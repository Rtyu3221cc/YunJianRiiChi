param(
 [Parameter(Mandatory=$true)][string]$ApkPath,
 [string]$EditorPath,
 [string]$SdkToolsDirectory = (Join-Path $PSScriptRoot '../tools/sdk/android-15'),
 [string]$KeyStore = (Join-Path $env:USERPROFILE '.android/debug.keystore'),
 [string]$KeyAlias = 'androiddebugkey'
)
$ErrorActionPreference = 'Stop'
$apkSigningTask = (Resolve-Path -LiteralPath $ApkPath).Path
$jarSigningTask = Join-Path $SdkToolsDirectory 'lib/apksigner.jar'
$alignSigningTask = Join-Path $SdkToolsDirectory 'zipalign.exe'
if ($EditorPath) {
 $javaSigningTask = Join-Path (Split-Path $EditorPath -Parent) 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe'
} elseif ($env:JAVA_HOME) {
 $javaSigningTask = Join-Path $env:JAVA_HOME 'bin/java.exe'
} else {
 $javaSigningTask = (Get-Command java.exe -ErrorAction Stop).Source
}
foreach ($requiredSigningTask in @($javaSigningTask,$jarSigningTask,$alignSigningTask,$KeyStore)) {
 if (!(Test-Path -LiteralPath $requiredSigningTask)) { throw "Signing dependency missing: $requiredSigningTask" }
}
# Unity already signs the build. Keep its certificate when adding JAR/v1
# compatibility; a different key would prevent updates of installed copies.
$beforeSigningTask = (& $javaSigningTask -jar $jarSigningTask verify --verbose --print-certs $apkSigningTask) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'The Unity build signature is invalid before compatibility signing.' }
$beforeCertificateTask = [regex]::Match($beforeSigningTask,'Signer #1 certificate SHA-256 digest: ([0-9a-f]+)').Groups[1].Value
if (!$beforeCertificateTask) { throw 'Could not read the original Unity signing certificate.' }
$temporarySigningTask = [IO.Path]::ChangeExtension($apkSigningTask,'v1v2.apk')
$storePasswordSigningTask = $env:RIICHI_KEYSTORE_PASSWORD
$keyPasswordSigningTask = $env:RIICHI_KEY_PASSWORD
try {
 if (!$env:RIICHI_KEYSTORE_PASSWORD) { $env:RIICHI_KEYSTORE_PASSWORD = 'android' }
 if (!$env:RIICHI_KEY_PASSWORD) { $env:RIICHI_KEY_PASSWORD = $env:RIICHI_KEYSTORE_PASSWORD }
 & $javaSigningTask -jar $jarSigningTask sign --ks $KeyStore --ks-key-alias $KeyAlias --ks-pass env:RIICHI_KEYSTORE_PASSWORD --key-pass env:RIICHI_KEY_PASSWORD --v1-signing-enabled true --v2-signing-enabled true --v3-signing-enabled false --v4-signing-enabled false --lib-page-alignment 16384 --out $temporarySigningTask $apkSigningTask
 if ($LASTEXITCODE -ne 0) { throw 'APK v1/v2 signing failed.' }
} finally {
 $env:RIICHI_KEYSTORE_PASSWORD = $storePasswordSigningTask
 $env:RIICHI_KEY_PASSWORD = $keyPasswordSigningTask
}
# Default verification only selects v2 for minSdk >= 24. Force an API 23
# verification range as well, so v1 is actually checked rather than skipped.
$afterSigningTask = (& $javaSigningTask -jar $jarSigningTask verify --verbose --print-certs --min-sdk-version 23 $temporarySigningTask 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0 -or $afterSigningTask -notmatch 'Verified using v1 scheme \(JAR signing\): true' -or $afterSigningTask -notmatch 'Verified using v2 scheme \(APK Signature Scheme v2\): true') { throw 'APK must pass both v1 and v2 signature verification.' }
$afterCertificateTask = [regex]::Match($afterSigningTask,'Signer #1 certificate SHA-256 digest: ([0-9a-f]+)').Groups[1].Value
if ($afterCertificateTask -ne $beforeCertificateTask) { throw 'Signing certificate changed; update compatibility would be lost.' }
# Verify JAR signatures independently, including parsers that ignore v2.
$v1OnlySigningTask = (& $javaSigningTask -jar $jarSigningTask verify --min-sdk-version 23 --max-sdk-version 23 $temporarySigningTask 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Independent v1 signature verification failed.' }
& $alignSigningTask -c -P 16 4 $temporarySigningTask
if ($LASTEXITCODE -ne 0) { throw 'Signing damaged APK/native-library alignment.' }
Move-Item -LiteralPath $temporarySigningTask -Destination $apkSigningTask -Force
Write-Output "PASS: v1 and v2 signatures, independent v1 verification, same certificate $afterCertificateTask, 16 KB alignment."
