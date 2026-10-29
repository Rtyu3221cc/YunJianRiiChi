$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput=Join-Path $taskRoot 'build/original-input-r18/pure-tests'
New-Item -ItemType Directory -Force $taskOutput | Out-Null
$taskSource=Join-Path $taskRoot 'unity/Assets/Scripts/ClientHandOrder.cs'
$taskTests=Join-Path $taskRoot 'tests/ClientHandOrderContract.cs'
$taskProject='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup><ItemGroup><Compile Include="'+$taskSource+'"/><Compile Include="'+$taskTests+'"/></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $taskOutput 'HandInput.csproj'),$taskProject,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $taskOutput 'NuGet.Config'),'<configuration><packageSources><clear /></packageSources></configuration>',[Text.UTF8Encoding]::new($false))
$env:DOTNET_CLI_HOME=Join-Path $taskRoot 'build/original-input-r18/dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
& 'D:/Unity/Lib/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe' run --project (Join-Path $taskOutput 'HandInput.csproj') -c Release
if($LASTEXITCODE -ne 0){throw 'Original hand order contracts failed'}
