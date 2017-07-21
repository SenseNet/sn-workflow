New-Item ..\..\src\nuget\snadmin\install-workflow\scripts -ItemType directory -Force

Copy-Item ..\..\src\Workflow\Data\Scripts\SqlWorkflowInstanceStoreSchema.sql ..\..\src\nuget\snadmin\install-workflow\scripts -Force
Copy-Item ..\..\src\Workflow\Data\Scripts\SqlWorkflowInstanceStoreLogic.sql ..\..\src\nuget\snadmin\install-workflow\scripts -Force

Compress-Archive -Path "..\..\src\nuget\snadmin\install-workflow\*" -Force -CompressionLevel Optimal -DestinationPath "..\..\src\nuget\content\Admin\tools\install-workflow.zip"
nuget pack ..\..\src\Workflow\Workflow.nuspec -properties Configuration=Release
nuget pack ..\..\src\Workflow\Workflow.Install.nuspec -properties Configuration=Release
nuget pack ..\..\src\Workflow.Portlets\Workflow.Portlets.nuspec -properties Configuration=Release

# nuget.exe push -Source "SenseNet" -ApiKey VSTS .\SenseNet.Workflow.7.0.0-beta0.nupkg
# nuget.exe push -Source "SenseNet" -ApiKey VSTS .\SenseNet.Workflow.Install.7.0.0-beta0.nupkg
# nuget.exe push -Source "SenseNet" -ApiKey VSTS .\SenseNet.Workflow.Portlets.7.0.0-beta0.nupkg
