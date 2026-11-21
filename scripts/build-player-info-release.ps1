param(
    [string]$PythonPath='python',
    [string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$setup=Get-Content -Raw -Encoding utf8 -LiteralPath (Join-Path $repo 'unity/Assets/Editor/ProjectSetup.cs')
if($setup -notmatch 'ReleaseVersion="0\.6\.40"' -or $setup -notmatch 'ReleaseCode=88;'){throw 'This recorded build entry is for 0.6.40/88 only; update the entry for a later release'}
$stage=Join-Path $repo 'build/phone-test-r24/stage/unity'
$out=Join-Path $repo 'build/release-0.6.40'
$previousJava=$env:RIICHI_JAVA
$env:RIICHI_JAVA=Join-Path (Split-Path $EditorPath -Parent) 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe'
New-Item -ItemType Directory -Path $out -Force | Out-Null
Push-Location $repo
try {
    & $PythonPath (Join-Path $repo 'scripts/prepare-original-input-stage.py') --batch phone-test-r24
    if($LASTEXITCODE -ne 0){throw 'Stage preparation failed'}
    git apply --directory=build/phone-test-r24/stage scripts/phone-test/CharacterPackService.patch
    if($LASTEXITCODE -ne 0){throw 'Phone-test Saber overlay no longer applies; inspect the source difference'}
    $temporary=Join-Path $stage 'Assets/Editor/Release0640Build.cs'
    if(Test-Path -LiteralPath $temporary){throw 'Release entry already exists'}
    Copy-Item -LiteralPath (Join-Path $repo 'tests/Release0640Build.cs') -Destination $temporary
    $previousOutput=$env:YUNJIAN_RELEASE_REVIEW
    $env:YUNJIAN_RELEASE_REVIEW=$out
    try {
        $arguments='-batchmode -buildTarget Android -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.Release0640Build.Run -logFile "{1}"' -f $stage,(Join-Path $out 'android-0.6.40.log')
        $process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
        $process.WaitForExit()
        if($process.ExitCode -ne 0){throw 'Android build failed; inspect build/release-0.6.40/android-0.6.40.log'}
    } finally {
        $env:YUNJIAN_RELEASE_REVIEW=$previousOutput
        foreach($path in @($temporary,($temporary+'.meta'))){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
    }
    $apk=Join-Path $stage 'Builds/yunjian-riichi-unity-0.6.40.apk'
    & (Join-Path $repo 'scripts/sign-unity-apk.ps1') -ApkPath $apk -EditorPath $EditorPath
    if($LASTEXITCODE -ne 0){throw 'APK signing failed'}
    $final=Join-Path $repo 'dist/yunjian-riichi-unity-0.6.40.apk'
    Copy-Item -LiteralPath $apk -Destination $final
    $checksum=(Get-FileHash -Algorithm SHA256 -LiteralPath $final).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($final)+"`n"
    [IO.File]::WriteAllText((Join-Path $repo 'dist/SHA256-yunjian-riichi-unity-0.6.40.txt'),$checksum,[Text.UTF8Encoding]::new($false))
    & $PythonPath (Join-Path $repo 'scripts/verify-character-audio-apk.py') $final (Join-Path $repo 'dist/unity-0.6.40-audio-verification.json')
    if($LASTEXITCODE -ne 0){throw 'Base and audio APK verification failed'}
    & $PythonPath (Join-Path $repo 'scripts/verify-feedback-apk-r41.py') $final (Join-Path $repo 'dist/unity-0.6.40-feature-verification.json')
    if($LASTEXITCODE -ne 0){throw 'Feature APK verification failed'}
    & $PythonPath (Join-Path $repo 'scripts/verify-profile-apk.py') $final (Join-Path $repo 'dist/unity-0.6.40-profile-verification.json')
    if($LASTEXITCODE -ne 0){throw 'Profile APK verification failed'}
} finally {$env:RIICHI_JAVA=$previousJava;Pop-Location}
