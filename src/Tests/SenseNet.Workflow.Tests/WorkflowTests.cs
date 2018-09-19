using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IO = System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

                    var wf = StartWorkflow(wfDef);
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

                    // Create a domain wit 3 users.
                    var domain = new Domain(Repository.ImsFolder) {Name = "Company1"};
                    domain.Save();
                    var users = new List<Node>();
                    for (int i = 1; i < 4; i++)
                    {
                        var user = new User(domain) { Name = $"User{i}" };
                        user.Save();
                        users.Add(user);
                    }

                    // Start the workflow.
                    var wf = StartWorkflow(wfDef, new Dictionary<string, object> { { "RelatedUsers", users } });

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

        private WorkflowHandlerBase StartWorkflow(WorkflowDefinitionHandler wfDef, Dictionary<string, object> fields = null)
        {
            var parent = Node.LoadNode("/Root/Sites/Default_Site/Workflows");
            var name = wfDef.Name.Replace(".xaml", "");

            var content = Content.CreateNew(name, parent, name);
            if (fields != null)
                foreach (var item in fields)
                    content[item.Key] = item.Value;

            var wfHandler = (WorkflowHandlerBase)content.ContentHandler;
            wfHandler.AllowManualStart = true;
            content.Save();

            InstanceManager.Start(wfHandler);

            return wfHandler;
        }
    }
}
