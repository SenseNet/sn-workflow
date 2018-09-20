using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IO = System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Workflow.Tests.Implementations;

namespace SenseNet.Workflow.Tests
{
    [TestClass]
    public class WorkflowTests : WorkflowTestBase
    {
        [TestMethod]
        public void WF_TestWF()
        {
            #region <ContentType name='testworkflow' ...
            const string ctd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='testworkflow' parentType='Workflow' handler='SenseNet.Workflow.WorkflowHandlerBase' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>testworkflow</DisplayName>
  <Description>testworkflow</Description>
  <Icon>workflow</Icon>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields/>
</ContentType>
";
            #endregion

            var xamlName = "TestWF.xaml";

            var lines = new List<string>();
            var observer = new ObserverTracer(
                new[] { "Workflow runs" },
                line => { lines.Add(line.Substring(line.LastIndexOf('\t') + 1)); });
            SnTrace.SnTracers.Add(observer);

            try
            {
                using (new SystemAccount())
                {
                    var wfDef = InstallWorkflow("testworkflow.xaml", ctd, xamlName);

                    var wf = StartWorkflow(wfDef, null);
                    WaitFor(wf, DateTime.Now.AddMinutes(1), () => lines.Count >= 3);

                    wf = Node.Load<WorkflowHandlerBase>(wf.Id);
                    if (wf?.WorkflowStatus == WorkflowStatusEnum.Running)
                        InstanceManager.Abort(wf, WorkflowApplicationAbortReason.ManuallyAborted);

                    Assert.IsTrue(lines.Count >= 3, $"Call count does not reach 3 ({lines.Count}).");
                }
            }
            finally
            {
                SnTrace.SnTracers.Remove(observer);
            }

        }

        [TestMethod]
        public void WF_CollectionTestWF()
        {
            #region <ContentType name='collectiontestworkflow' ...
            const string ctd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='collectiontestworkflow' parentType='Workflow' handler='SenseNet.Workflow.WorkflowHandlerBase' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>testworkflow</DisplayName>
  <Description>testworkflow</Description>
  <Icon>workflow</Icon>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields>
    <Field name='RelatedUsers' type='Reference'>
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <AllowedTypes>
          <Type>User</Type>
          <Type>Group</Type>
        </AllowedTypes>
      </Configuration>
    </Field>  </Fields>
</ContentType>
";
            #endregion

            var xamlName = "CollectionTestWF.xaml";

            var lines = new List<string>();
            var observer = new ObserverTracer(
                new[] {"Creating a task for", "Task created for", "Task status:" },
                line => { lines.Add(line.Substring(line.LastIndexOf('\t') + 1)); });
            SnTrace.SnTracers.Add(observer);

            try
            {
                using (new SystemAccount())
                {
                    // Create initial repository structure with the tested workflow prototype.
                    var wfDef = InstallWorkflow("collectiontestworkflow.xaml", ctd, xamlName);

                    // Create a task container.
                    var site = Node.LoadNode("/Root/Sites/Default_Site");
                    var testRoot = new SystemFolder(site) {Name = "WfCollectionTest"};
                    testRoot.Save();
                    var tasks = Content.CreateNew("TaskList", testRoot, "Tasks");
                    tasks.Save();

                    // Create a domain with 3 users.
                    var users = CreateTestUsers();

                    // Start the workflow.
                    var wf = StartWorkflow(wfDef, null, new Dictionary<string, object> { { "RelatedUsers", users } });

                    // Wait for first complete monitoring cycle (persist / wake-up). Timeout: half minute.
                    WaitFor(wf, DateTime.Now.AddSeconds(30), () => lines.Count >= 12);

                    // Modify the Status of tasks.
                    var task = Content.Load("/Root/Sites/Default_Site/WfCollectionTest/Tasks/Task4User1");
                    task["Status"] = "waiting";
                    task.Save();
                    task = Content.Load("/Root/Sites/Default_Site/WfCollectionTest/Tasks/Task4User2");
                    task["Status"] = "deferred";
                    task.Save();
                    task = Content.Load("/Root/Sites/Default_Site/WfCollectionTest/Tasks/Task4User3");
                    task["Status"] = "completed";
                    task.Save();

                    // Wait for workflow detects the change but max a minute.
                    WaitFor(wf, DateTime.Now.AddMinutes(1), () => lines.Last().Contains("Task status: completed"));

                    // Abort workflow if runs.
                    wf = Node.Load<WorkflowHandlerBase>(wf.Id);
                    if(wf?.WorkflowStatus == WorkflowStatusEnum.Running)
                        InstanceManager.Abort(wf, WorkflowApplicationAbortReason.ManuallyAborted);

                    Assert.IsTrue(lines.Last().Contains("Task status: completed"));
                }
            }
            finally
            {
                SnTrace.SnTracers.Remove(observer);
            }
        }

        // This method demonstrates the WfContentCollection usage (see the pseudocode line 28).
        // The test requires a content type for the workflow's related content with two multi-reference fields:
        // RelatedUsers and Tasks. Related users is filled by the test code, item of Tasks are created by the workflow.
        // The test does these tasks:
        //     - Creates a workflow observer for these terms: "Creating a task for", "Task created for", "Task status:".
        //     - Instalss the CTD of the WF related content.
        //     - Installs the workflow prototype (CTD and a WF definition instance).
        //     - Creates the required structure.
        //     - Starts the workflow
        //     - Waits for 12 related trace line.
        //     - Sets the status fields of the tasks created by workflow. New values are pendingm deferred, completed
        //     - Waits for this message: "Task status: completed"
        //     - Checks the trace last line.
        // Here is the list of all newly created paths:
        //      /Root/IMS/Company1
        //      /Root/IMS/Company1/User1
        //      /Root/IMS/Company1/User2
        //      /Root/IMS/Company1/User3
        //      /Root/Sites
        //      /Root/Sites/Default_Site
        //      /Root/Sites/Default_Site/WfContentCollectionTest
        //      /Root/Sites/Default_Site/WfContentCollectionTest/MultiReferenceTestContent
        //      /Root/Sites/Default_Site/WfContentCollectionTest/Tasks
        //      /Root/Sites/Default_Site/workflows
        //      /Root/Sites/Default_Site/workflows/multireferencetestworkflow
        //      /Root/System/Schema/ContentTypes/GenericContent/MultiReferenceTestContentType
        //      /Root/System/Schema/ContentTypes/GenericContent/Workflow/multireferencetestworkflow
        //      /Root/System/Workflows
        //      /Root/System/Workflows/multireferencetestworkflow.xaml
        // The workflows executes this tasks:
        //      - Pins the RelatedContent that is a reference of the workflow state content.
        //      - Creates tasks:
        //        - Enumerates the users in the RelatedUsers of the RelatedContent.
        //          The collection is a WfContentCollection
        //        - Creates a task for each user.
        //        - Loads the node of the new task and collect it in a list of Node.
        //        - Loads the Node of the RelatedContent and sets the list to the "Tasks" reference
        //          property and save the node.
        //      - Monitors the tasks in an infinite loop:
        //        - Enumerates the tasks from the WfCollection stored in the "Tasks" field of the RelatedContent.
        //        - Writes the task status to the trace channel.
        //        - Sleeps for 15 seconds.
        // The workflow"s pseudo code:
        //  1 Sequence: "Main"
        //  2     Variable: WfContent RelatedContent
        //  3     Assign: RelatedContent = StateContent.Reference["RelatedContent"];
        //  4     Sequence: "Create tasks"
        //  5         Variable: List<Node> newTasks = new List<Node>();
        //  6         Variable: Node relatedNode
        //  7         ForEach: WfContent item in RelatedContent.References("RelatedUsers")
        //  8             Variable: String userName
        //  9             Variable: WfContent newWfContent
        // 10             Variable: Node newTask
        // 11             Assign: userName = ContentRepository.Storage.RepositoryPath.GetFileName(item.Path);
        // 12             DebugWrite: "Creating a task for " + userName
        // 13             CreateContent: WfContent newWfContent
        // 14                 ContentTypeName: "Task"
        // 15                 ParentPath: "/Root/Sites/Default_Site/WfContentCollectionTest/Tasks"
        // 16                 ContentDisplayName: "Task for " + userName
        // 17                 FieldValues: new Dictionary<string, object> { {"AssignedTo", item }, {"Status", "waiting" } };
        // 18                 Name: "Task4" + userName
        // 19             InvokeMethod: newTask = Node.LoadNode(newWfContent.Id);
        // 20             AddToCollection: newTask --> newTasks
        // 21             DebugWrite: "Task created " + newTask.Path
        // 22         InvokeMethod: relatedNode = Node.LoadNode(int);
        // 23         InvokeMethod: relatedNode.SetReferences("Tasks", newTasks);
        // 24         InvokeMethod: relatedNode.Save();
        // 25         LoadContent: relatedNode.Path --> RelatedContent
        // 26         DebugWrite: "Referred task count: " + RelatedContent.References("Tasks").Count
        // 27     While: True
        // 28         ForEach: var task in RelatedContent.References("Tasks") // WfContent <-- WfContentCollection
        // 29             DebugWrite: "Task status: " + task["Status"] + ": " + task.Path
        // 30         Delay: 15 sec
        //
        // The workflow's raw output (related SnTrace lines):
        //     Debug: Creating a task for User1 
        //     Debug: Task created /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User1 
        //     Debug: Creating a task for User2 
        //     Debug: Task created /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User2 
        //     Debug: Creating a task for User3 
        //     Debug: Task created /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User3 
        //     Debug: Referred task count: 3 
        //     Debug: Task status: waiting: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User1 
        //     Debug: Task status: waiting: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User2 
        //     Debug: Task status: waiting: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User3 
        // 15-20 sec pause
        //     Debug: Task status: waiting: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User1 
        //     Debug: Task status: waiting: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User2 
        //     Debug: Task status: waiting: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User3 
        // 15-20 sec pause
        //     Debug: Task status: pending: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User1 
        //     Debug: Task status: deferred: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User2 
        //     Debug: Task status: completed: /Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User3 
        [TestMethod]
        public void WF_MultiReferenceTestWF()
        {
            #region <ContentType name='collectiontestworkflow' ...
            const string wfCtd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='multireferencetestworkflow' parentType='Workflow' handler='SenseNet.Workflow.WorkflowHandlerBase' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>testworkflow</DisplayName>
  <Description>testworkflow</Description>
  <Icon>workflow</Icon>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields/>
</ContentType>
";
            #endregion
            #region <ContentType name='MultiReferenceTestContentType' ...
            const string multiReferenceTestContentTypeCtd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='MultiReferenceTestContentType' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields>
    <Field name='RelatedUsers' type='Reference'>
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <AllowedTypes>
          <Type>User</Type>
        </AllowedTypes>
      </Configuration>
    </Field>
    <Field name='Tasks' type='Reference'>
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <AllowedTypes>
          <Type>Task</Type>
        </AllowedTypes>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
";
            #endregion

            const string xamlName = "MultiReferenceTestWF.xaml";

            var lines = new List<string>();
            var observer = new ObserverTracer(
                new[] { "Creating a task for", "Task created for", "Task status:" },
                line => { lines.Add(line.Substring(line.LastIndexOf('\t') + 1)); });
            SnTrace.SnTracers.Add(observer);

            try
            {
                using (new SystemAccount())
                {
                    // Install the content type for the WF related content
                    ContentTypeInstaller.InstallContentType(multiReferenceTestContentTypeCtd);

                    // Create initial repository structure with the tested workflow prototype.
                    var wfDef = InstallWorkflow("multireferencetestworkflow.xaml", wfCtd, xamlName);

                    // Create a task container.
                    var site = Node.LoadNode("/Root/Sites/Default_Site");
                    var testRoot = new SystemFolder(site) { Name = "WfContentCollectionTest" };
                    testRoot.Save();
                    var tasks = Content.CreateNew("TaskList", testRoot, "Tasks");
                    tasks.Save();

                    // Create a domain with 3 users.
                    var users = CreateTestUsers();

                    // Create a workflow related content.
                    var relatedContent = Content.CreateNew("MultiReferenceTestContentType", testRoot, "MultiReferenceTestContent");
                    relatedContent["RelatedUsers"] = users;
                    relatedContent.Save();

                    // Start the workflow.
                    var wf = StartWorkflow(wfDef, relatedContent.ContentHandler);

                    // Wait for first complete monitoring cycle (persist / wake-up). Timeout: half minute.
                    WaitFor(wf, DateTime.Now.AddSeconds(30), () => lines.Count >= 12);

                    // Modify the Status of tasks.
                    var task = Content.Load("/Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User1");
                    task["Status"] = "pending";
                    task.Save();
                    task = Content.Load("/Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User2");
                    task["Status"] = "deferred";
                    task.Save();
                    task = Content.Load("/Root/Sites/Default_Site/WfContentCollectionTest/Tasks/Task4User3");
                    task["Status"] = "completed";
                    task.Save();

                    // Wait for workflow detects the change but max a minute.
                    WaitFor(wf, DateTime.Now.AddMinutes(1), () => lines.Last().Contains("Task status: completed"));

                    // Abort workflow if runs.
                    wf = Node.Load<WorkflowHandlerBase>(wf.Id);
                    if (wf?.WorkflowStatus == WorkflowStatusEnum.Running)
                        InstanceManager.Abort(wf, WorkflowApplicationAbortReason.ManuallyAborted);

                    Assert.IsTrue(lines.Last().Contains("Task status: completed"));
                }
            }
            finally
            {
                SnTrace.SnTracers.Remove(observer);
            }

        }

        /* =============================================================================== Common tools */

        private void WaitFor(WorkflowHandlerBase workflow, DateTime timeLimit, Func<bool> condition)
        {
            SnTrace.Test.Write("WAIT START");
            while (DateTime.Now < timeLimit && !condition())
            {
                Thread.Sleep(1000);
                workflow = Node.Load<WorkflowHandlerBase>(workflow.Id);
                if (workflow.WorkflowStatus != WorkflowStatusEnum.Running)
                    break;
            }
            SnTrace.Test.Write("WAIT END");
        }
        private void EnsureDefaultSiteStructure()
        {
            var sites = Node.LoadNode("/Root/Sites");
            if (sites == null)
            {
                sites = new GenericContent(Repository.Root, "Sites") { Name = "Sites" };
                sites.Save();
            }

            var site = Node.LoadNode("/Root/Sites/Default_Site");
            if (site == null)
            {
                site = new Site(sites) { Name = "Default_Site" };
                site.Save();
            }

            var workflows = Node.LoadNode("/Root/Sites/Default_Site/workflows");
            if (workflows == null)
            {
                workflows = new SystemFolder(site) { Name = "workflows" };
                workflows.Save();
            }
        }

        private IEnumerable<Node> CreateTestUsers()
        {
            // Create a domain with 3 users.
            var domain = Node.Load<Domain>(RepositoryStructure.ImsFolderPath + "/Company1");
            if (domain == null)
            {
                domain = new Domain(Repository.ImsFolder) { Name = "Company1" };
                domain.Save();
            }
            var users = new List<Node>();
            for (var i = 1; i < 4; i++)
            {
                var user = Node.Load<User>(domain.Path + $"/User{i}");
                if (user == null)
                {
                    user = new User(domain) { Name = $"User{i}" };
                    user.Save();
                }
                users.Add(user);
            }
            return users;
        }

        private WorkflowDefinitionHandler InstallWorkflow(string name, string ctd, string xamlName)
        {
            var xamlPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, $@"..\..\Workflows\{xamlName}"));

            EnsureDefaultSiteStructure();

            if (ContentType.GetByName(name) == null)
                ContentTypeInstaller.InstallContentType(ctd);

            var wfPah = "/Root/System/Workflows/" + name;
            var wfd = Node.Load<WorkflowDefinitionHandler>(wfPah);
            if (wfd == null)
            {
                string xaml;
                using (var reader = new IO.StreamReader(xamlPath))
                    xaml = reader.ReadToEnd();

                var parent = Node.LoadNode("/Root/System/Workflows");
                wfd = new WorkflowDefinitionHandler(parent, "WorkflowDefinition") {Name = name};
                wfd.DeleteInstanceAfterFinished = WorkflowDeletionStrategy.AlwaysKeep;
                wfd.Binary.SetStream(RepositoryTools.GetStreamFromString(xaml));
                wfd.Save();
            }
            return wfd;
        }

        private WorkflowHandlerBase StartWorkflow(WorkflowDefinitionHandler wfDef, Node relatedContent, Dictionary<string, object> fields = null)
        {
            var parent = Node.LoadNode("/Root/Sites/Default_Site/Workflows");
            var name = wfDef.Name.Replace(".xaml", "");

            var content = Content.CreateNew(name, parent, name);
            if (fields != null)
                foreach (var item in fields)
                    content[item.Key] = item.Value;

            var wfHandler = (WorkflowHandlerBase)content.ContentHandler;
            wfHandler.AllowManualStart = true;
            if(relatedContent != null)
                wfHandler.RelatedContent = relatedContent;
            content.Save();

            InstanceManager.Start(wfHandler);

            return wfHandler;
        }
    }
}
