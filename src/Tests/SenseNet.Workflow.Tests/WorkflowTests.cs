using System;
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
            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
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

            var called = 0;
            var observer = new ObserverTracer(new[] { "Workflow runs" }, () => { called++; });
            SnTrace.SnTracers.Add(observer);

            try
            {
                using (new SystemAccount())
                {
                    var wfDef = EnsureWorkflow("testworkflow.xaml", ctd, xamlName);
                    var wf = StartWorkflow(wfDef);
                    WaitForUpperLimit(ref called, 3, DateTime.Now.AddMinutes(1));
                    
                    wf = Node.Load<WorkflowHandlerBase>(wf.Id);
                    InstanceManager.Abort(wf, WorkflowApplicationAbortReason.ManuallyAborted);

                    Assert.IsTrue(called >= 3, $"Call count does not reach 3 ({called}).");
                }
            }
            finally
            {
                SnTrace.SnTracers.Remove(observer);
            }

        }

        private void WaitForUpperLimit(ref int value, int limit, DateTime timeLimit)
        {
            SnTrace.Test.Write("WAIT START");
            while (DateTime.Now < timeLimit && value < limit)
                Thread.Sleep(1000);
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
        private WorkflowDefinitionHandler EnsureWorkflow(string name, string ctd, string xamlName)
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
                wfd.Binary.SetStream(RepositoryTools.GetStreamFromString(xaml));
                wfd.Save();
            }
            return wfd;
        }
        private WorkflowHandlerBase StartWorkflow(WorkflowDefinitionHandler wfDef)
        {
            var parent = Node.LoadNode("/Root/Sites/Default_Site/Workflows");
            var name = wfDef.Name.Replace(".xaml", "");
            var content = Content.CreateNew(name, parent, name);
            var wfHandler = (WorkflowHandlerBase)content.ContentHandler;
            wfHandler.AllowManualStart = true;
            content.Save();
            InstanceManager.Start(wfHandler);
            return wfHandler;
        }

    }
}
