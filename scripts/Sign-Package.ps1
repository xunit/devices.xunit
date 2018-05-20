$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if([string]::IsNullOrEmpty($env:SignClientSecret)){
	Write-Host "Client Secret not found, not signing packages"
	return;
}

& nuget install SignClient -Version 0.9.1 -SolutionDir "$currentDirectory\..\" -Verbosity quiet -ExcludeVersion
# Setup Variables we need to pass into the sign client tool

$appSettings = "$currentDirectory\appsettings.json"
$filter = "$currentDirectory\filter.txt"

$appPath = "$currentDirectory\..\packages\SignClient\tools\netcoreapp2.0\SignClient.dll"

$nupgks = ls $Env:ArtifactDirectory\*.nupkg | Select -ExpandProperty FullName
$vsixs = ls $Env:ArtifactDirectory\*.vsix | Select -ExpandProperty FullName


foreach ($nupkg in $nupgks){
	Write-Host "Submitting $nupkg for signing"

	dotnet $appPath 'sign' -c $appSettings -i $nupkg -f $filter -r $env:SignClientUser -s $env:SignClientSecret -n 'xUnit.net' -d 'xUnit.net' -u 'https://github.com/xunit/devices.xunit' 

	Write-Host "Finished signing $nupkg"
}

foreach ($vsix in $vsixs){
	Write-Host "Submitting $vsix for signing"

	dotnet $appPath 'sign' -c $appSettings -i $vsix -f $filter -r $env:SignClientUser -s $env:SignClientSecret -n 'xUnit.net' -d 'xUnit.net' -u 'https://github.com/xunit/devices.xunit' 

	Write-Host "Finished signing $nupkg"
}

Write-Host "Sign-package complete"