New-Item ..\..\src\nuget\snadmin\install-workflow\scripts -ItemType directory -Force

Copy-Item ..\..\src\Workflow\Data\Scripts\SqlWorkflowInstanceStoreSchema.sql ..\..\src\nuget\snadmin\install-workflow\scripts -Force
Copy-Item ..\..\src\Workflow\Data\Scripts\SqlWorkflowInstanceStoreLogic.sql ..\..\src\nuget\snadmin\install-workflow\scripts -Force

Compress-Archive -Path "..\..\src\nuget\snadmin\install-workflow\*" -Force -CompressionLevel Optimal -DestinationPath "..\..\src\nuget\content\Admin\tools\install-workflow.zip"
nuget pack ..\..\src\Workflow\Workflow.nuspec -properties Configuration=Release
nuget pack ..\..\src\Workflow\Workflow.Install.nuspec -properties Configuration=Release
