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

namespace SenseNet.Workflow.Tests
{
    [TestClass]
    public class WorkflowTests : WorkflowTestBase
    {
        [TestMethod]
        public void WF_TestWF()
        {
            #region <Activity x:Class='SenseNet.Workflow.Definitions.TestWF' ...

            string xaml = @"<Activity mc:Ignorable='sap sap2010 sads' x:Class='SenseNet.Workflow.Definitions.TestWF'
 xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
 xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
 xmlns:mva='clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities'
 xmlns:sads='http://schemas.microsoft.com/netfx/2010/xaml/activities/debugger'
 xmlns:sap='http://schemas.microsoft.com/netfx/2009/xaml/activities/presentation'
 xmlns:sap2010='http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation'
 xmlns:scg='clr-namespace:System.Collections.Generic;assembly=mscorlib'
 xmlns:sco='clr-namespace:System.Collections.ObjectModel;assembly=mscorlib'
 xmlns:sd='clr-namespace:System.Diagnostics;assembly=System'
 xmlns:sw='clr-namespace:SenseNet.Workflow;assembly=SenseNet.Workflow'
 xmlns:swa='clr-namespace:SenseNet.Workflow.Activities;assembly=SenseNet.Workflow'
 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <x:Members>
    <x:Property Name='StateContent' Type='InArgument(sw:WfContent)' />
  </x:Members>
  <mva:VisualBasic.Settings>
    <x:Null />
  </mva:VisualBasic.Settings>
  <TextExpression.NamespacesForImplementation>
    <sco:Collection x:TypeArguments='x:String'>
      <x:String>System.Activities</x:String>
      <x:String>System.Activities.Statements</x:String>
      <x:String>System.Activities.Expressions</x:String>
      <x:String>System.Activities.Validation</x:String>
      <x:String>System.Activities.XamlIntegration</x:String>
      <x:String>Microsoft.VisualBasic</x:String>
      <x:String>Microsoft.VisualBasic.Activities</x:String>
      <x:String>System</x:String>
      <x:String>System.Activities.Debugger</x:String>
      <x:String>System.Activities.Debugger.Symbol</x:String>
      <x:String>System.Collections.Generic</x:String>
      <x:String>System.Diagnostics</x:String>
      <x:String>System.Data</x:String>
      <x:String>System.Linq</x:String>
      <x:String>System.Text</x:String>
      <x:String>SenseNet.Workflow</x:String>
      <x:String>System.Windows.Markup</x:String>
      <x:String>SenseNet.Diagnostics</x:String>
    </sco:Collection>
  </TextExpression.NamespacesForImplementation>
  <TextExpression.ReferencesForImplementation>
    <sco:Collection x:TypeArguments='AssemblyReference'>
      <AssemblyReference>System.Activities</AssemblyReference>
      <AssemblyReference>System</AssemblyReference>
      <AssemblyReference>mscorlib</AssemblyReference>
      <AssemblyReference>System.Xml</AssemblyReference>
      <AssemblyReference>System.Core</AssemblyReference>
      <AssemblyReference>System.ServiceModel</AssemblyReference>
      <AssemblyReference>System.Data</AssemblyReference>
      <AssemblyReference>SenseNet.Workflow</AssemblyReference>
      <AssemblyReference>PresentationFramework</AssemblyReference>
      <AssemblyReference>WindowsBase</AssemblyReference>
      <AssemblyReference>PresentationCore</AssemblyReference>
      <AssemblyReference>System.Xaml</AssemblyReference>
      <AssemblyReference>SenseNet.Portal</AssemblyReference>
      <AssemblyReference>SenseNet.Storage</AssemblyReference>
    </sco:Collection>
  </TextExpression.ReferencesForImplementation>
  <Sequence>
    <DoWhile Condition='True'>
      <Sequence>
        <swa:DebugWrite Message='Workflow runs.' sap2010:WorkflowViewState.IdRef='DebugWrite_1' />
        <InvokeMethod MethodName='WriteLine' TargetType='sd:Trace'>
          <InArgument x:TypeArguments='x:String'>TestWF runs.</InArgument>
          <sap2010:WorkflowViewState.IdRef>InvokeMethod_1</sap2010:WorkflowViewState.IdRef>
        </InvokeMethod>
        <Delay Duration='[TimeSpan.FromMinutes(1)]' sap2010:WorkflowViewState.IdRef='Delay_1' />
        <sap2010:WorkflowViewState.IdRef>Sequence_1</sap2010:WorkflowViewState.IdRef>
      </Sequence>
      <sap2010:WorkflowViewState.IdRef>DoWhile_1</sap2010:WorkflowViewState.IdRef>
    </DoWhile>
    <sap2010:WorkflowViewState.IdRef>Sequence_2</sap2010:WorkflowViewState.IdRef>
    <sads:DebugSymbol.Symbol>d1dEOlxkZXZcdGZzXFNlbnNlTmV0XFJlbGVhc2VzXGVudGVycHJpc2VcdjYuNVxzZXJ2aWNlcGFja1xTb3VyY2VcU2Vuc2VOZXRcV0ZcVGVzdFdGLnhhbWwKPANLDgIBAT0FSA8CAQI9GD0eAgEKPgdGEgIBAz8JP2MCAQhACUMYAgEGRAlEYQIBBD8hPzECAQlBMkE+AgEHRBlENAIBBQ==</sads:DebugSymbol.Symbol>
  </Sequence>
  <sap2010:WorkflowViewState.IdRef>SenseNet.Workflow.Definitions.TestWF_1</sap2010:WorkflowViewState.IdRef>
  <sap2010:WorkflowViewState.ViewStateManager>
    <sap2010:ViewStateManager>
      <sap2010:ViewStateData Id='DebugWrite_1' sap:VirtualizedContainerService.HintSize='218,22' />
      <sap2010:ViewStateData Id='InvokeMethod_1' sap:VirtualizedContainerService.HintSize='218,128' />
      <sap2010:ViewStateData Id='Delay_1' sap:VirtualizedContainerService.HintSize='218,22' />
      <sap2010:ViewStateData Id='Sequence_1' sap:VirtualizedContainerService.HintSize='240,376'>
        <sap:WorkflowViewStateService.ViewState>
          <scg:Dictionary x:TypeArguments='x:String, x:Object'>
            <x:Boolean x:Key='IsExpanded'>True</x:Boolean>
          </scg:Dictionary>
        </sap:WorkflowViewStateService.ViewState>
      </sap2010:ViewStateData>
      <sap2010:ViewStateData Id='DoWhile_1' sap:VirtualizedContainerService.HintSize='464,538' />
      <sap2010:ViewStateData Id='Sequence_2' sap:VirtualizedContainerService.HintSize='486,662'>
        <sap:WorkflowViewStateService.ViewState>
          <scg:Dictionary x:TypeArguments='x:String, x:Object'>
            <x:Boolean x:Key='IsExpanded'>True</x:Boolean>
          </scg:Dictionary>
        </sap:WorkflowViewStateService.ViewState>
      </sap2010:ViewStateData>
      <sap2010:ViewStateData Id='SenseNet.Workflow.Definitions.TestWF_1' sap:VirtualizedContainerService.HintSize='526,742' />
    </sap2010:ViewStateManager>
  </sap2010:WorkflowViewState.ViewStateManager>
</Activity>
";

            #endregion

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

            using (new SystemAccount())
            {
                var wfDef = EnsureWorkflow("testworkflow.xaml", ctd, xaml);

                var wf = StartWorkflow(wfDef);

                SnTrace.Test.Write("WAIT START");
                Thread.Sleep(2 * 60 * 1000);
                SnTrace.Test.Write("WAIT END");

                wf = Node.Load<WorkflowHandlerBase>(wf.Id);
                InstanceManager.Abort(wf, WorkflowApplicationAbortReason.ManuallyAborted);

                Assert.Inconclusive();
            }
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
        private WorkflowDefinitionHandler EnsureWorkflow(string name, string ctd, string xaml)
        {
            EnsureDefaultSiteStructure();

            if (ContentType.GetByName(name) == null)
            {
                ContentTypeInstaller.InstallContentType(ctd);
            }

            var wfPah = "/Root/System/Workflows/" + name;
            var wfd = Node.Load<WorkflowDefinitionHandler>(wfPah);
            if (wfd == null)
            {
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
