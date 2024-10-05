#Requires -Version 5.1

[CmdletBinding()]
param (
  [string] $Configuration = 'Release',
  [switch] $ContinuousIntegration,
  [switch] $WithBinLog
)

$ErrorActionPreference = 'Stop'

function Complete-BuildStep {
  param (
    [string] $BinLogKeyword,
    [string] $FailureTerm
  )
  if (Test-Path msbuild.binlog) {
    if (Test-Path msbuild.$BinLogKeyword.binlog) {
      Remove-Item -Force msbuild.$BinLogKeyword.binlog
    }
    Rename-Item msbuild.binlog msbuild.$BinLogKeyword.binlog
  }
  Write-Host ''
  if ($LASTEXITCODE -ne 0) {
    Write-Error "$FailureTerm FAILED"
  }
}

$opts = @( '--nologo' )
if ($WithBinLog) {
  $opts += '-bl'
}

$props = @()
if ($ContinuousIntegration) {
  $props += '-p:ContinuousIntegrationBuild=true'
  $props += '-p:Deterministic=true'
}

# The top-level folder used as target for the publish step (before being zipped).
if ($ContinuousIntegration) {
  $PublishFolder = "gh-build-${Configuration}"
}
else {
  $PublishFolder = 'publish'
}

# The zip file created after the publish step.
# FIXME: Can we easily get any version info included in this name?
$ZipFile = 'Zastai.NuGet.Server.zip'

Write-Host 'Cleaning up existing build output...'
Remove-Item -Recurse -Force */bin, */obj
if (Test-Path $PublishFolder) {
  Remove-Item -Recurse -Force $PublishFolder
}
Write-Host ''

Write-Host "Running package restore..."
dotnet restore $opts
Complete-BuildStep 'restore' 'PACKAGE RESTORE'

Write-Host "Building the solution (Configuration: $Configuration)..."
dotnet build $opts --no-restore "-c:$Configuration" $props
Complete-BuildStep 'build' 'SOLUTION BUILD'

Write-Host 'Running tests...'
dotnet test $opts --no-build "-c:$Configuration"
Complete-BuildStep 'test' 'UNIT TESTS'

Write-Host 'Publishing...'
dotnet publish $opts PackageImport       --no-build "-c:$Configuration" "-o:$PublishFolder/importer"
dotnet publish $opts Zastai.NuGet.Server --no-build "-c:$Configuration" "-o:$PublishFolder/server"
Complete-BuildStep 'publish' 'PUBLISH'

if (-not $ContinuousIntegration) {
  Write-Host 'Creating Zip File...'
  Compress-Archive -Force -Path $PublishFolder/* -DestinationPath $ZipFile
  Remove-Item -Recurse -Force $PublishFolder
}
