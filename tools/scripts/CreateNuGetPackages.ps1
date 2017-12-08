$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installPackageFolder = "$srcPath\nuget\content\Admin\tools"
$installPackagePath = "$installPackageFolder\install-workflow.zip"
$scriptsSourcePath = "$srcPath\Workflow\Data\Scripts"
$scriptsTargetPath = "$srcPath\nuget\snadmin\install-workflow\scripts"

# delete existing packages
Remove-Item $PSScriptRoot\*.nupkg

if (!(Test-Path $installPackageFolder))
{
	New-Item $installPackageFolder -Force -ItemType Directory
}

New-Item $scriptsTargetPath -ItemType directory -Force

Copy-Item $scriptsSourcePath\SqlWorkflowInstanceStoreSchema.sql $scriptsTargetPath -Force
Copy-Item $scriptsSourcePath\SqlWorkflowInstanceStoreLogic.sql $scriptsTargetPath -Force

Compress-Archive -Path "$srcPath\nuget\snadmin\install-workflow\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath

nuget pack $srcPath\Workflow\Workflow.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
nuget pack $srcPath\Workflow\Workflow.Install.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
nuget pack $srcPath\Workflow.Portlets\Workflow.Portlets.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
