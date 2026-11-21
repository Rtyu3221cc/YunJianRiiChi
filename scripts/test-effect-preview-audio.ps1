param(
    [string]$EditorPath='D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe',
    [string]$StagePath='build/phone-test-r24/stage/unity',
    [string]$ReviewRoot='build/effect-playback-r35'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
function Absolute([string]$value){if([IO.Path]::IsPathRooted($value)){return [IO.Path]::GetFullPath($value)};return [IO.Path]::GetFullPath((Join-Path $repo $value))}
$stage=Absolute $StagePath
$output=Absolute $ReviewRoot
if(-not (Test-Path -LiteralPath (Join-Path $stage 'Assets/Scenes/Table.unity'))){throw 'The cached Unity stage is required'}
New-Item -ItemType Directory -Force -Path $output | Out-Null
$directory=Get-Content -LiteralPath (Join-Path $repo 'unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-windows.json') -Raw | ConvertFrom-Json
$packs=@{};foreach($pack in $directory.packs){$packs[$pack.key]=$pack}
$wanted=[Collections.Generic.HashSet[string]]::new()
function IncludePack([string]$key){if($wanted.Add($key)){foreach($dep in $packs[$key].dependencies){IncludePack $dep}}}
foreach($id in @('ron_chilin','effect_liqi_chilin','riichi-fire')){IncludePack (($directory.items | Where-Object id -eq $id).pack)}
Push-Location $repo
try{$files=@(& rg --files build -g '*.bundle')}finally{Pop-Location}
$index=@{};foreach($name in $files){$base=[IO.Path]::GetFileName($name);$index[$base]=@($index[$base])+@((Join-Path $repo $name))}
foreach($key in $wanted){
    $pack=$packs[$key];if($pack.builtin){continue}
    $source=$null
    foreach($file in @($index[$pack.file])+@($index[$pack.sha256+'.bundle'])){
        if($file -and (Get-Item -LiteralPath $file).Length -eq $pack.bytes -and (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -eq $pack.sha256){$source=$file;break}
    }
    if(-not $source){throw "Missing verified Windows test pack: $key"}
    $folder=Join-Path $output ('wardrobes/'+$key);New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $target=Join-Path $folder ($pack.sha256+'.bundle')
    if(-not (Test-Path -LiteralPath $target)){
        if([IO.Path]::GetPathRoot($source) -eq [IO.Path]::GetPathRoot($target)){New-Item -ItemType HardLink -Path $target -Target $source | Out-Null}else{Copy-Item -LiteralPath $source -Destination $target}
    }
    if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pack.sha256){throw "Invalid cached test pack: $key"}
    [IO.File]::WriteAllText($target+'.ok',$pack.sha256)
}
Copy-Item -LiteralPath (Join-Path $repo 'tests/EffectPreviewAudioReview.cs') -Destination (Join-Path $stage 'Assets/Editor/EffectPreviewAudioReview.cs')
foreach($file in @('RiichiClient.DressPreview.cs','LobbyController.DressTable.cs')){
    Copy-Item -LiteralPath (Join-Path $repo ('unity/Assets/Scripts/'+$file)) -Destination (Join-Path $stage ('Assets/Scripts/'+$file))
}
$priorRoot=$env:YUNJIAN_EFFECT_PREVIEW_ROOT;$priorMode=$env:YUNJIAN_EFFECT_PREVIEW_MODE
try{
    $env:YUNJIAN_EFFECT_PREVIEW_ROOT=$output;$env:YUNJIAN_EFFECT_PREVIEW_MODE='after'
    $arguments='-batchmode -projectPath "{0}" -executeMethod YunJian.Riichi.Editor.EffectPreviewAudioReview.Run -logFile "{1}"' -f $stage,(Join-Path $output 'preview-regression.log')
    $process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $deadline=[DateTime]::UtcNow.AddMinutes(3)
    while(-not $process.WaitForExit(1000)){if([DateTime]::UtcNow -ge $deadline){Stop-Process -Id $process.Id;throw 'Preview audio regression timed out'}}
    if($process.ExitCode -ne 0){throw ('Preview audio regression failed; see '+(Join-Path $output 'after.failed.txt'))}
    $report=Get-Content -LiteralPath (Join-Path $output 'after.json') -Raw | ConvertFrom-Json
    if($report.cases.Count -ne 3 -or @($report.cases | Where-Object { -not $_.cueObserved -or $_.peak -le 0 -or -not $_.stoppedOnExit }).Count){throw 'Incomplete preview audio report'}
    $report.cases | Select-Object id,peak,cycles,stoppedOnExit
}finally{$env:YUNJIAN_EFFECT_PREVIEW_ROOT=$priorRoot;$env:YUNJIAN_EFFECT_PREVIEW_MODE=$priorMode}
