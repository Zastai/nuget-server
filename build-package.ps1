#Requires -Version 6.2
#Requires -Modules @{ ModuleName = 'Microsoft.PowerShell.Archive'; ModuleVersion = '1.2.3' }

[CmdletBinding()]
param (
  [string] $Configuration = 'Release',
  [switch] $WithBinLog = $false
)

$opts = @( '--nologo' )
if ($WithBinLog) {
  $opts += '-bl'
}

$ErrorActionPreference = 'Stop'

# The top-level folder used as target for the publish step (before being zipped).
$PublishFolder = 'publish'

# The zip file created after the publish step.
# FIXME: Can we easily get any version info included in this name?
$ZipFile = 'Zastai.NuGet.Server.zip'

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
dotnet build $opts --no-restore "-c:$Configuration" '-p:ContinuousIntegrationBuild=true' '-p:Deterministic=true'
Complete-BuildStep 'build' 'SOLUTION BUILD'

Write-Host 'Running tests...'
dotnet test $opts --no-build "-c:$Configuration"
Complete-BuildStep 'test' 'UNIT TESTS'

Write-Host 'Publishing...'
dotnet publish $opts --no-build "-c:$Configuration" "-o:$PublishFolder"
Complete-BuildStep 'pack' 'PUBLISH'

Write-Host 'Creating Zip File...'
Push-Location $PublishFolder
try {
  Compress-Archive -Force -Path * -DestinationPath "..\$ZipFile"
}
finally {
  Pop-Location
}
Remove-Item -Recurse -Force $PublishFolder
