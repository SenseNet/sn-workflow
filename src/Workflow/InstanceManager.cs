using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.Activities.DurableInstancing;
using System.Xml.Linq;
using System.Runtime.DurableInstancing;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository;
using System.Activities.XamlIntegration;
using System.Activities.Expressions;
using SenseNet.Configuration;

namespace SenseNet.Workflow
{
    public enum WorkflowApplicationCreationPurpose { StartNew, Resume, Poll, Abort };
    public enum WorkflowApplicationAbortReason { ManuallyAborted, StateContentDeleted, RelatedContentChanged, RelatedContentDeleted };

    public static class InstanceManager
    {
        private const string STATECONTENT = "StateContent";
        private const double MINPOLLINTERVAL = 2000.0;

        public static void StartWorkflowSystem()
        {
            SnTrace.Workflow.Write("Start Workflow System");
        }

        // =========================================================================================================== Polling

        private static readonly System.Timers.Timer _pollTimer;

        static InstanceManager()
        {
            var pollInterval = SenseNet.Configuration.Workflow.TimerInterval * 60.0 * 1000.0;

            if (pollInterval >= MINPOLLINTERVAL)
            {
                _pollTimer = new System.Timers.Timer(pollInterval);
                _pollTimer.Elapsed += PollTimerElapsed;
                _pollTimer.Disposed += PollTimerDisposed;
                _pollTimer.Enabled = true;

                SnLog.WriteInformation("Starting polling timer. Interval in minutes: " + SenseNet.Configuration.Workflow.TimerInterval);
            }
            else
            {
                SnLog.WriteWarning(string.Format("Polling timer was not started because the configured interval ({0}) is less than acceptable minimum ({1}). Interval in minutes: ",
                    SenseNet.Configuration.Workflow.TimerInterval, MINPOLLINTERVAL));
            }
        }
        private static void PollTimerDisposed(object sender, EventArgs e)
        {
            _pollTimer.Elapsed -= PollTimerElapsed;
            _pollTimer.Disposed -= PollTimerDisposed;
        }
        private static void PollTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            using (var op = SnTrace.Workflow.StartOperation("Polling"))
            {
                var msg = new StringBuilder();
                int counter = 0;
                _pollTimer.Enabled = false;
                try
                {
                    ManageOrphanedInstances();
                    ManageOrphanedLockOwners();

                    foreach (var item in GetPollingInstances())
                    {
                        try
                        {
                            ExecuteDelays(item);
                        }
                        catch (Exception ex)
                        {
                            if (msg.Length == 0)
                                msg.AppendLine("Workflow polling errors:");
                            msg.AppendFormat("{0}.: {1} was thrown during processing {2}. Message: {3}{4}", ++counter, ex.GetType().FullName, item.Path, ex.Message, Environment.NewLine);

                            SnTrace.Workflow.WriteError("WF: POLLING ERROR {0}. {1} was thrown during processing {2}. {3}", counter, ex.GetType().FullName, item.Path, e);
                        }
                    }
                    if (msg.Length > 0)
                        throw new ApplicationException(msg.ToString());
                }
                finally
                {
                    _pollTimer.Enabled = true;
                }

                op.Successful = true;
            }
        }
        public static IEnumerable<WorkflowHandlerBase> GetPollingInstances()
        {
            try
            {
                if (!RepositoryInstance.ContentQueryIsAllowed)
                    return new WorkflowHandlerBase[0];
                var result = SenseNet.Search.ContentQuery.Query(SafeQueries.GetPollingInstances, null, WorkflowStatusEnum.Running);
                var instances = new Dictionary<string, WorkflowHandlerBase>();
                foreach (WorkflowHandlerBase item in result.Nodes)
                {
                    var key = string.Format("{0}-{1}", item.WorkflowTypeName, item.WorkflowDefinitionVersion);
                    if (!instances.ContainsKey(key))
                        instances.Add(key, item);
                }
                SnTrace.Workflow.Write("WF: Trying execute active workflows. ResultCount:{0}, PollingItems:{1}", result.Count, instances.Count);

                return instances.Values.ToArray();
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("GetPollingInstances: {0}", e);
                throw;
            }
        }
        private static void _Poll()
        {
            try
            {
                foreach (var item in GetPollingInstances())
                    ExecuteDelays(item);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("_Poll: {0}", e);
                throw;
            }
        }

        // =========================================================================================================== Building

        private static WorkflowDataClassesDataContext GetDataContext()
        {
            try
            {
                return new WorkflowDataClassesDataContext(ConnectionStrings.ConnectionString);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("GetDataContext: {0}", e);
                throw;
            }
        }

        private static WorkflowApplication CreateWorkflowApplication(WorkflowHandlerBase workflowMasterInstance, WorkflowApplicationCreationPurpose purpose, IDictionary<string, object> parameters)
        {
            try
            {
                string version;
                WorkflowApplication wfApp = null;
                var workflow = workflowMasterInstance.CreateWorkflowInstance(out version);
                CompileExpressions(workflow);
                switch (purpose)
                {
                    case WorkflowApplicationCreationPurpose.StartNew:
                        Dictionary<string, object> arguments = workflowMasterInstance.CreateParameters();
                        arguments.Add(STATECONTENT, new WfContent(workflowMasterInstance));
                        if (parameters != null)
                            foreach (var item in parameters)
                                arguments.Add(item.Key, item.Value);
                        wfApp = new WorkflowApplication(workflow, arguments);
                        workflowMasterInstance.WorkflowDefinitionVersion = version;
                        workflowMasterInstance.WorkflowInstanceGuid = wfApp.Id.ToString();
                        break;
                    default:
                        wfApp = new WorkflowApplication(workflow);
                        break;
                }

                SnTrace.Workflow.Write("WF: CreateWorkflowApplication: NodeId:{0}, instanceId:{1}, Purpose:{2}",
                    workflowMasterInstance.Id, workflowMasterInstance.WorkflowInstanceGuid, purpose);

                InstanceHandle ownerHandle;
                var store = CreateInstanceStore(workflowMasterInstance, out ownerHandle);
                Dictionary<XName, object> wfScope = new Dictionary<XName, object> { { GetWorkflowHostTypePropertyName(), GetWorkflowHostTypeName(workflowMasterInstance) } };

                wfApp.InstanceStore = store;
                wfApp.AddInitialInstanceValues(wfScope);

                wfApp.PersistableIdle =      a => { SnTrace.Workflow.Write("WF: PersistableIdle. WfAppId:{0}",      wfApp.Id); DestroyInstanceOwner(wfApp, ownerHandle); return PersistableIdleAction.Unload; };
                wfApp.Unloaded =             b => { SnTrace.Workflow.Write("WF: Unloaded. WfAppId:{0}",             wfApp.Id); DestroyInstanceOwner(wfApp, ownerHandle); };
                wfApp.Completed =            c => { SnTrace.Workflow.Write("WF: Completed. WfAppId:{0}",            wfApp.Id); OnWorkflowCompleted(c); DestroyInstanceOwner(wfApp, ownerHandle); };
                wfApp.Aborted =              d => { SnTrace.Workflow.Write("WF: Aborted. WfAppId:{0}",              wfApp.Id); OnWorkflowAborted(d); DestroyInstanceOwner(wfApp, ownerHandle); };
                wfApp.OnUnhandledException = e => { SnTrace.Workflow.Write("WF: OnUnhandledException. WfAppId:{0}", wfApp.Id); return HandleError(e); };

                wfApp.Extensions.Add(new ContentWorkflowExtension
                {
                    WorkflowApp = wfApp,
                    WorkflowInstancePath = purpose == WorkflowApplicationCreationPurpose.Poll
                        ? null
                        : workflowMasterInstance.Path
                });
                return wfApp;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("CreateWorkflowApplication: {0}", e);
                throw;
            }
        }
        private static SqlWorkflowInstanceStore CreateInstanceStore(WorkflowHandlerBase workflowInstance, out InstanceHandle ownerHandle)
        {
            try
            {
                var store = new SqlWorkflowInstanceStore(ConnectionStrings.ConnectionString);
                ownerHandle = store.CreateInstanceHandle();

                var wfHostTypeName = GetWorkflowHostTypeName(workflowInstance);
                var WorkflowHostTypePropertyName = GetWorkflowHostTypePropertyName();

                var ownerCommand = new CreateWorkflowOwnerCommand() { InstanceOwnerMetadata = { { WorkflowHostTypePropertyName, new InstanceValue(wfHostTypeName) } } };
                var owner = store.Execute(ownerHandle, ownerCommand, TimeSpan.FromSeconds(30)).InstanceOwner;
                ownerHandle.Free();
                store.DefaultInstanceOwner = owner;
                return store;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("CreateInstanceStore: {0}", e);
                throw;
            }
        }
        private static void DestroyInstanceOwner(WorkflowApplication wfApp, InstanceHandle instanceHandle)
        {
            try
            {
                if (instanceHandle.IsValid)
                {
                    var deleteOwnerCmd = new DeleteWorkflowOwnerCommand();
                    wfApp.InstanceStore.Execute(instanceHandle, deleteOwnerCmd, TimeSpan.FromSeconds(30));
                    wfApp.InstanceStore.DefaultInstanceOwner = null;
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("DestroyInstanceOwner: {0}", e);
                throw;
            }
        }

        private static XName GetWorkflowHostTypePropertyName()
        {
            return XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("WorkflowHostType");
        }
        private static XName GetWorkflowHostTypeName(WorkflowHandlerBase workflowInstance)
        {
            return XName.Get(workflowInstance.WorkflowHostType, "http://www.sensenet.com/2010/workflow");
        }

        private static void CompileExpressions(Activity activity)
        {
            // activityName is the Namespace.Type of the activity that contains the
            // C# expressions.
            var da = activity as DynamicActivity;

            string activityName =
                (da != null && !string.IsNullOrEmpty(da.Name))
                ? da.Name :
                activity.GetType().ToString();

            // Split activityName into Namespace and Type.Append _CompiledExpressionRoot to the type name
            // to represent the new type that represents the compiled expressions.
            // Take everything after the last . for the type name.
            string activityType = activityName.Split('.').Last() + "_CompiledExpressionRoot";
            // Take everything before the last . for the namespace.
            string activityNamespace = string.Join(".", activityName.Split('.').Reverse().Skip(1).Reverse());

            // Create a TextExpressionCompilerSettings.
            TextExpressionCompilerSettings settings = new TextExpressionCompilerSettings
            {
                Activity = activity,
                Language = "C#",
                ActivityName = activityType,
                ActivityNamespace = activityNamespace,
                RootNamespace = null,
                GenerateAsPartialClass = false,
                AlwaysGenerateSource = true,
                ForImplementation = false
            };

            // Compile the C# expression.
            TextExpressionCompilerResults results = new TextExpressionCompiler(settings).Compile();

            // Any compilation errors are contained in the CompilerMessages.
            if (results.HasErrors)
            {
                SnTrace.Workflow.WriteError("Activity compilation error.");
                throw new Exception("Compilation failed.");
            }

            // Create an instance of the new compiled expression type.
            ICompiledExpressionRoot compiledExpressionRoot =
                Activator.CreateInstance(results.ResultType,
                    new object[] { activity }) as ICompiledExpressionRoot;

            // Attach it to the activity.
            CompiledExpressionInvoker.SetCompiledExpressionRoot(
                activity, compiledExpressionRoot);
        }

        // =========================================================================================================== Operations

        public static Guid Start(WorkflowHandlerBase workflowInstance)
        {
            try
            {
                var wfApp = CreateWorkflowApplication(workflowInstance, WorkflowApplicationCreationPurpose.StartNew, null);
                var id = wfApp.Id;
                workflowInstance.WorkflowStatus = WorkflowStatusEnum.Running;
                workflowInstance.DisableObserver(typeof(WorkflowNotificationObserver));
                using (new SystemAccount())
                    workflowInstance.Save();
                wfApp.Run();
                return id;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("Start: {0}", e);
                throw;
            }
        }

        public static void Abort(WorkflowHandlerBase workflowInstance, WorkflowApplicationAbortReason reason)
        {
            try
            {
                // check permissions
                if (reason == WorkflowApplicationAbortReason.ManuallyAborted && !workflowInstance.Security.HasPermission(PermissionType.Save))
                {
                    SnTrace.Workflow.Write("InstanceManager cannot abort the instance: {0}, because the user doesn't have the sufficient permissions (Save).", workflowInstance.Path);
                    throw new SenseNetSecurityException(workflowInstance.Path, PermissionType.Save, AccessProvider.Current.GetCurrentUser());
                }

                // abort the workflow
                var name = workflowInstance == null ? "[null]" : workflowInstance.Name;
                try
                {
                    var wfApp = CreateWorkflowApplication(workflowInstance, WorkflowApplicationCreationPurpose.Abort, null);

                    wfApp.Load(Guid.Parse(workflowInstance.WorkflowInstanceGuid));
                    wfApp.Cancel();
                }
                catch (Exception e)
                {
                    SnTrace.Workflow.WriteError("CANNOT ABORT: {0}", name);
                    SnLog.WriteWarning(string.Concat("InstanceManager cannot abort the instance: ", workflowInstance.Path, ". Exception message: ", e.Message));
                }

                // write back workflow state
                WriteBackAbortMessage(workflowInstance, reason);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("Abort: {0}", e);
                throw;
            }
        }

        public static void ExecuteDelays(WorkflowHandlerBase workflowMasterInstance)
        {
            try
            {
                var wfApps = LoadRunnableInstances(workflowMasterInstance);

                var loadedInstanceIds = wfApps.Select(w => w.Id).ToArray();
                SnTrace.Workflow.Write("Loaded instances for executing delays: ({0}): {1}", loadedInstanceIds.Length, loadedInstanceIds);

                foreach (var wfApp in wfApps)
                {
                    if (ValidWakedUpWorkflow(wfApp))
                        wfApp.Run();
                    else
                        wfApp.Cancel();
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("ExecuteDelays: {0}", e);
                throw;
            }
        }

        private static IEnumerable<WorkflowApplication> LoadRunnableInstances(WorkflowHandlerBase workflowMasterInstance)
        {
            try
            {
                var wfApps = new List<WorkflowApplication>();
                while (true)
                {
                    try
                    {
                        var wfApp = CreateWorkflowApplication(workflowMasterInstance, WorkflowApplicationCreationPurpose.Poll, null);
                        wfApp.LoadRunnableInstance();
                        wfApps.Add(wfApp);
                    }
                    catch (InstanceNotReadyException)
                    {
                        break;
                    }
                }
                return wfApps;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("LoadRunnableInstances: {0}", e);
                throw;
            }
        }

        public static void FireNotification(WorkflowNotification notification, WorkflowNotificationEventArgs eventArgs)
        {
            try
            {
                var wfInstance = Node.Load<WorkflowHandlerBase>(notification.WorkflowNodePath);
                var wfApp = CreateWorkflowApplication(wfInstance, WorkflowApplicationCreationPurpose.Resume, null);
                wfApp.Load(notification.WorkflowInstanceId);

                var nodeId = notification.NodeId;
                var instanceId = notification.WorkflowInstanceId;
                var bookmarkName = notification.BookmarkName;
                if (ValidWakedUpWorkflow(wfApp))
                {
                    if (wfApp.GetBookmarks().Any(x => x.BookmarkName == bookmarkName))
                    {
                        SnTrace.Workflow.Write("WF: FireNotification: Resume bookmark: NodeId: {0}, instance: {1}, bookmark: {2}", nodeId, instanceId, bookmarkName);
                        wfApp.ResumeBookmark(bookmarkName, eventArgs);
                    }
                    else
                    {
                        SnTrace.Workflow.Write("WF: FireNotification: Skip bookmark: NodeId: {0}, instance: {1}, bookmark: {2}", nodeId, instanceId, bookmarkName);
                        wfApp.Unload();
                    }
                }
                else
                {
                    SnTrace.Workflow.Write("WF: FireNotification: Cancel: NodeId: {0}, instance: {1}", nodeId, instanceId);
                    wfApp.Cancel();
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("FireNotification: {0}", e);
                throw;
            }
        }
        public static void NotifyContentChanged(WorkflowNotificationEventArgs eventArgs)
        {
            try
            {
                WorkflowNotification[] notifications = null;
                using (var dbContext = GetDataContext())
                {
                    notifications = dbContext.WorkflowNotifications.Where(notification =>
                        notification.NodeId == eventArgs.NodeId).ToArray();
                }

                if (SnTrace.Workflow.Enabled)
                    SnTrace.Workflow.Write("WF: NotifyContentChanged: Loaded notifications for Content#{0}: count:{1}, items:[{2}]", 
                        eventArgs.NodeId,
                        notifications.Length,
                        notifications.Select(n => n.WorkflowInstanceId).ToArray());

                foreach (var notification in notifications)
                    InstanceManager.FireNotification(notification, eventArgs);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("NotifyContentChanged: {0}", e);
                throw;
            }
        }
        /// <summary>
        /// Notifies the workflow engine about multiple content changes.
        /// </summary>
        /// <param name="contentIds">List of content ids that changed.</param>
        /// <param name="notificationType">Freetext notification type (e.g. child created)</param>
        /// <param name="info">Optional change-specific information.</param>
        public static void NotifyMultipleContentChanged(int[] contentIds, string notificationType, object info = null)
        {
            if (contentIds == null || contentIds.Length == 0)
                return;

            try
            {
                WorkflowNotification[] notifications;

                // load all notifications using a single db query
                using (var dbContext = GetDataContext())
                {
                    notifications = dbContext.WorkflowNotifications.Where(wfn => contentIds.Contains(wfn.NodeId)).ToArray();
                }

                foreach (var notification in notifications)
                    FireNotification(notification, new WorkflowNotificationEventArgs(notification.NodeId, notificationType, info));
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("NotifyMultipleContentChanged: {0}", e);
                throw;
            }
        }
        public static int RegisterWait(int nodeID, Guid wfInstanceId, string bookMarkName, string wfContentPath)
        {
            try
            {
                SnTrace.Workflow.Write("RegisterWait:{0}", wfInstanceId);
                using (var dbContext = GetDataContext())
                {
                    var notification = new WorkflowNotification()
                    {
                        BookmarkName = bookMarkName,
                        NodeId = nodeID,
                        WorkflowInstanceId = wfInstanceId,
                        WorkflowNodePath = wfContentPath
                    };
                    dbContext.WorkflowNotifications.InsertOnSubmit(notification);
                    dbContext.SubmitChanges();
                    return notification.NotificationId;
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("RegisterWait: {0}", e);
                throw;
            }
        }
        public static void ReleaseWait(int notificationId)
        {
            try
            {
                SnTrace.Workflow.Write("ReleaseWait: {0}", notificationId);
                using (var dbContext = GetDataContext())
                {
                    var ent = dbContext.WorkflowNotifications.SingleOrDefault(wn => wn.NotificationId == notificationId);
                    dbContext.WorkflowNotifications.DeleteOnSubmit(ent);
                    dbContext.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("ReleaseWait: {0}", e);
                throw;
            }
        }

        // =========================================================================================================== Events

        private static void OnWorkflowAborted(WorkflowApplicationAbortedEventArgs args)
        {
            try
            {
                DeleteNotifications(args.InstanceId);
                WriteBackTheState(WorkflowStatusEnum.Aborted, args.InstanceId);

                // also write back abort message, if it is not yet given
                var stateContent = GetStateContent(args.InstanceId);
                if (stateContent == null)
                    return;

                WriteBackAbortMessage(stateContent, DumpException(args.Reason));
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("OnWorkflowAborted: {0}", e);
                throw;
            }
        }
        private static void OnWorkflowCompleted(WorkflowApplicationCompletedEventArgs args)
        {
            try
            {
                DeleteNotifications(args.InstanceId);

                // This event handler is called not only when the last activity is completed successfully,
                // but from Abort also (!), because we call the Cancel method there (instead of Abort) 
                // to avoid interrupting the workflow forcefully.
                WriteBackTheState(args.CompletionState == ActivityInstanceState.Canceled || args.CompletionState == ActivityInstanceState.Faulted
                    ? WorkflowStatusEnum.Aborted
                    : WorkflowStatusEnum.Completed, args.InstanceId);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("OnWorkflowCompleted: {0}", e);
                throw;
            }
        }
        private static void DeleteNotifications(Guid instanceId)
        {
            try
            {
                using (var dbContext = GetDataContext())
                {
                    var notifications = dbContext.WorkflowNotifications.Where(notification =>
                        notification.WorkflowInstanceId == instanceId);

                    dbContext.WorkflowNotifications.DeleteAllOnSubmit(notifications);
                    dbContext.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("DeleteNotifications: {0}", e);
                throw;
            }
        }

        private static UnhandledExceptionAction HandleError(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                SnLog.WriteException(args.UnhandledException);

                WorkflowHandlerBase stateContent = GetStateContent(args);
                if (stateContent == null)
                    SnLog.WriteWarning("The workflow InstanceManager cannot write back the aborting/terminating reason into the workflow state content.");
                else
                    WriteBackAbortMessage(stateContent, DumpException(args));
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("WF: OnUnhandledException: {0}", e);
                SnLog.WriteException(e);
            }
            return UnhandledExceptionAction.Abort;
        }

        // =========================================================================================================== Tools

        private static bool ValidWakedUpWorkflow(WorkflowApplication wfApp)
        {
            try
            {
                var stateContent = GetStateContent(wfApp.Id);
                if (stateContent == null)
                {
                    WriteBackAbortMessage(null, WorkflowApplicationAbortReason.StateContentDeleted);
                    return false;
                }

                if (!stateContent.ContentWorkflow)
                    return true;

                if (stateContent.RelatedContent == null)
                {
                    WriteBackAbortMessage(stateContent, WorkflowApplicationAbortReason.RelatedContentDeleted);
                    return false;
                }
                
                // If the workflow should not be aborted when the related content has changed, 
                // than we do not care if the timestamp is different on the workflow content.
                if (stateContent.AbortOnRelatedContentChange && stateContent.RelatedContentTimestamp != stateContent.RelatedContent.NodeTimestamp)
                {
                    WriteBackAbortMessage(stateContent, WorkflowApplicationAbortReason.RelatedContentChanged);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("ValidWakedUpWorkflow: {0}", e);
                throw;
            }
        }

        private const string ABORTEDBYUSERMESSAGE = "Aborted manually by the following user: ";
        private static string GetAbortMessage(WorkflowApplicationAbortReason reason, WorkflowHandlerBase workflow)
        {
            try
            {
                switch (reason)
                {
                    case WorkflowApplicationAbortReason.ManuallyAborted:
                        return string.Concat(ABORTEDBYUSERMESSAGE, AccessProvider.Current.GetCurrentUser().Username);
                    case WorkflowApplicationAbortReason.StateContentDeleted:
                        return "Workflow deleted" + (workflow == null ? "." : (": " + workflow.Path));
                    case WorkflowApplicationAbortReason.RelatedContentChanged:
                        return "Aborted because the related content was changed.";
                    case WorkflowApplicationAbortReason.RelatedContentDeleted:
                        return "Aborted because the related content was moved or deleted.";
                    default:
                        return reason.ToString();
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("GetAbortMessage: {0}", e);
                throw;
            }
        }
        private static void WriteBackAbortMessage(WorkflowHandlerBase stateContent, WorkflowApplicationAbortReason reason)
        {
            try
            {
                var abortMessage = GetAbortMessage(reason, stateContent);
                if (reason == WorkflowApplicationAbortReason.StateContentDeleted)
                {
                    var msg = "Workflow aborted. Reason: " + abortMessage + ".";
                    if (stateContent != null)
                        msg += " InstanceId: " + stateContent.WorkflowInstanceGuid;
                    SnTrace.Workflow.Write("WF: {0}", msg);
                    SnLog.WriteInformation(msg);
                }
                else
                    WriteBackAbortMessage(stateContent, abortMessage);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("WriteBackAbortMessage#1: {0}", e);
                throw;
            }
        }
        private static void WriteBackAbortMessage(WorkflowHandlerBase stateContent, string abortMessage)
        {
            try
            {
                var state = stateContent.WorkflowStatus;
                if (state == WorkflowStatusEnum.Completed)
                    return;

                // if a system message has already been persisted to the workflow content, don't overwrite it
                if (!string.IsNullOrEmpty(stateContent.SystemMessages))
                    return;

                var times = 3;
                while (true)
                {
                    try
                    {
                        stateContent.SystemMessages = abortMessage;
                        stateContent.DisableObserver(typeof(WorkflowNotificationObserver));
                        SnTrace.Workflow.Write("WF: Workflow aborted. Reason:{0}. InstanceId:{1}", abortMessage, stateContent.WorkflowInstanceGuid);
                        using (new SystemAccount())
                            stateContent.Save(SenseNet.ContentRepository.SavingMode.KeepVersion);
                        break;
                    }
                    catch (NodeIsOutOfDateException ne)
                    {
                        if (--times == 0)
                            throw new NodeIsOutOfDateException("Node is out of date after 3 trying", ne);
                        var msg = "InstanceManager: Saving system message caused NodeIsOutOfDateException. Trying again.";
                        SnTrace.Workflow.WriteError("WF: ERROR {0}", msg);
                        stateContent = (WorkflowHandlerBase)Node.LoadNodeByVersionId(stateContent.VersionId);
                    }
                    catch (Exception e)
                    {
                        var msg = string.Format("InstanceManager:  Cannot write back a system message to the workflow state content. InstanceId: {0}. Path: {1}. Message: {2}. InstanceId: {3}."
                           , stateContent.Id, stateContent.Path, abortMessage, stateContent.WorkflowInstanceGuid);
                        SnTrace.Workflow.WriteError("WF: ERROR {0}", msg);
                        SnLog.WriteWarning(msg, properties: new Dictionary<string, object> { { "Exception", e } });
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("WriteBackAbortMessage#2: {0}", e);
                throw;
            }
        }

        private static void WriteBackTheState(WorkflowStatusEnum state, Guid instanceId)
        {
            try
            {
                var stateContent = GetStateContent(instanceId);
                if (stateContent == null)
                    return;

                switch (stateContent.WorkflowStatus)
                {
                    case WorkflowStatusEnum.Created:
                        if (state == WorkflowStatusEnum.Created)
                            return;
                        break;
                    case WorkflowStatusEnum.Running:
                        if (state == WorkflowStatusEnum.Created || state == WorkflowStatusEnum.Running)
                            return;
                        break;
                    case WorkflowStatusEnum.Aborted:
                    case WorkflowStatusEnum.Completed:
                        return;
                    default:
                        break;
                }

                var times = 3;
                while (true)
                {
                    try
                    {
                        stateContent.WorkflowStatus = state;
                        stateContent.DisableObserver(typeof(WorkflowNotificationObserver));
                        using (new SystemAccount())
                            stateContent.Save(SenseNet.ContentRepository.SavingMode.KeepVersion);
                        SnTrace.Workflow.Write("WF: WriteBackTheState:{0}, id:{1}, path:{2}", state, instanceId, stateContent.Path);
                        break;
                    }
                    catch (NodeIsOutOfDateException ne)
                    {
                        if (--times == 0)
                            throw new NodeIsOutOfDateException("Node is out of date after 3 trying", ne);
                        var msg = "InstanceManager: Writing back the workflow state caused NodeIsOutOfDateException. Trying again";
                        SnTrace.Workflow.Write("WF: {0}", msg);
                        stateContent = (WorkflowHandlerBase)Node.LoadNodeByVersionId(stateContent.VersionId);
                    }
                    catch (Exception e)
                    {
                        var msg = string.Format("Workflow state is {0} but cannot write back to the workflow state content. InstanceId: {1}. Path: {2}"
                           , state, instanceId, stateContent.Path);
                        SnTrace.Workflow.WriteError("WF: ERROR {0}", msg);
                        SnLog.WriteWarning(msg, properties: new Dictionary<string, object> { { "Exception", e } });
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("WriteBackTheState: {0}", e);
                throw;
            }
        }

        private static string DumpException(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                var e = args.UnhandledException;
                var sb = new StringBuilder();
                sb.AppendLine("An unhandled exception occurred during the workflow execution. Please review the following information.<br />");
                sb.AppendLine();
                sb.Append("Workflow instance: ").Append(args.InstanceId.ToString()).AppendLine("<br />");
                sb.AppendFormat("Source activity: {0} ({1}, {2})", args.ExceptionSource.DisplayName, args.ExceptionSource.GetType().FullName, args.ExceptionSource.Id);
                sb.AppendLine("<br />");
                sb.AppendLine("<br />");

                sb.Append(DumpException(e));

                return sb.ToString();
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("DumpException#1: {0}", e);
                throw;
            }
        }
        private static string DumpException(Exception ee)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("========== Exception:").AppendLine("<br />");
                sb.Append(ee.GetType().Name).Append(":").Append(ee.Message).AppendLine("<br />");
                DumpTypeLoadError(ee as ReflectionTypeLoadException, sb);
                sb.Append(ee.StackTrace).AppendLine("<br />");
                while ((ee = ee.InnerException) != null)
                {
                    sb.Append("---- Inner Exception:").AppendLine("<br />");
                    sb.Append(ee.GetType().Name).Append(": ").Append(ee.Message).AppendLine("<br />");
                    DumpTypeLoadError(ee as ReflectionTypeLoadException, sb);
                    sb.Append(ee.StackTrace).AppendLine("<br />");
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("DumpException#2: {0}", e);
                throw;
            }
        }
        private static void DumpTypeLoadError(ReflectionTypeLoadException exc, StringBuilder sb)
        {
            try
            {
                if (exc == null)
                    return;
                sb.Append("LoaderExceptions:").AppendLine("<br />");
                foreach (var e in exc.LoaderExceptions)
                {
                    sb.Append("-- ");
                    sb.Append(e.GetType().FullName);
                    sb.Append(": ");
                    sb.Append(e.Message).AppendLine("<br />");

                    var fileNotFoundException = e as System.IO.FileNotFoundException;
                    if (fileNotFoundException != null)
                    {
                        sb.Append("FUSION LOG:").AppendLine("<br />");
                        sb.Append(fileNotFoundException.FusionLog).AppendLine("<br />");
                    }
                }
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("DumpTypeLoadError: {0}", e);
                throw;
            }
        }

        private static WorkflowHandlerBase GetStateContent(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                WorkflowHandlerBase stateContent = null;
                var exts = args.GetInstanceExtensions<ContentWorkflowExtension>();
                if (exts != null)
                {
                    var ext = exts.FirstOrDefault();
                    if (ext != null)
                        stateContent = Node.Load<WorkflowHandlerBase>(ext.WorkflowInstancePath);
                }
                return stateContent;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("GetStateContent#1: {0}", e);
                throw;
            }
        }
        internal static WorkflowHandlerBase GetStateContent(Guid instanceId)
        {
            try
            {
                if (!RepositoryInstance.ContentQueryIsAllowed)
                    return null;
                var stateContent = (WorkflowHandlerBase)SenseNet.Search.ContentQuery.Query(SafeQueries.WorkflowStateContent,
                    null, WorkflowHandlerBase.WORKFLOWINSTANCEGUID, instanceId).Nodes.FirstOrDefault();
                return stateContent;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("GetStateContent#2: {0}", e);
                throw;
            }
        }

        [Obsolete("##", true)]
        private static void WriteDebug(object msg)
        {
            SnTrace.Workflow.Write("WF: {0}", msg);
        }

        private static void ManageOrphanedInstances()
        {
            ExecuteSqlCommand(@"-- WF: ManageOrphanedInstances
DECLARE @Page int
DECLARE @Column nvarchar(40)
DECLARE @Sql nvarchar(4000)
SELECT @Page = Page, @Column = [Column] FROM PropertyInfoView WHERE Name = 'WorkflowInstanceGuid'
SET @Sql = N'
DECLARE @TypeId int
SELECT @TypeId = PropertySetId FROM SchemaPropertySets WHERE Name = ''Workflow''
DECLARE @ToDelete TABLE (InstanceId uniqueidentifier, SurrogateInstanceId INT NOT NULL)

;WITH Subtypes (NodeTypeId) AS (
	SELECT @TypeId
	UNION ALL
	SELECT PropertySetId FROM SchemaPropertySets  JOIN Subtypes ON	SchemaPropertySets.ParentId = Subtypes.NodeTypeId
)
INSERT INTO @ToDelete
	SELECT Id InstanceId, SurrogateInstanceId FROM [System.Activities.DurableInstancing].InstancesTable
	WHERE Id NOT IN (
		SELECT f.' + @Column + ' FROM FlatProperties f JOIN Versions V ON V.VersionId = f.VersionId JOIN Nodes n ON n.NodeId = V.NodeId
		WHERE f.Page = ' + CONVERT(nvarchar(10), @Page) + ' AND f.' + @Column + ' IS NOT NULL AND n.NodeTypeId IN (SELECT NodeTypeId FROM Subtypes)
)

DELETE FROM [System.Activities.DurableInstancing].[InstancePromotedPropertiesTable] WHERE [SurrogateInstanceId] in (SELECT SurrogateInstanceId FROM @ToDelete)
DELETE FROM [System.Activities.DurableInstancing].[KeysTable]                       WHERE [SurrogateInstanceId] in (SELECT SurrogateInstanceId FROM @ToDelete)
DELETE FROM [System.Activities.DurableInstancing].[InstanceMetadataChangesTable]    WHERE [SurrogateInstanceId] in (SELECT SurrogateInstanceId FROM @ToDelete)
DELETE FROM [System.Activities.DurableInstancing].[RunnableInstancesTable]          WHERE [SurrogateInstanceId] in (SELECT SurrogateInstanceId FROM @ToDelete)
DELETE FROM [System.Activities.DurableInstancing].[InstancesTable]                  WHERE [SurrogateInstanceId] in (SELECT SurrogateInstanceId FROM @ToDelete)
'
EXEC dbo.sp_executesql @statement = @Sql
");

        }

        private static Dictionary<Guid, int> lockOwners = new Dictionary<Guid, int>();
        private static int LOCKOWNERMAXLOADEDGENERATION = 3;
        private static void ManageOrphanedLockOwners()
        {
            try
            {
                var loaded = LoadOrphanedLockOwners();

                var keysToRemove = lockOwners.Keys.Except(loaded).ToArray();
                foreach (var key in keysToRemove)
                    lockOwners.Remove(key);

                var keysToDelete = new List<Guid>();
                foreach (var id in loaded)
                {
                    int gen;
                    if (lockOwners.TryGetValue(id, out gen))
                    {
                        gen++;
                        if (gen >= LOCKOWNERMAXLOADEDGENERATION)
                        {
                            keysToDelete.Add(id);
                            lockOwners.Remove(id);
                        }
                        else
                        {
                            lockOwners[id] = gen;
                        }
                    }
                    else
                    {
                        lockOwners.Add(id, 1);
                    }
                }
                DeleteOrphanedLockOwners(keysToDelete);
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("ManageOrphanedLockOwners: {0}", e);
            }
        }
        private static Guid[] LoadOrphanedLockOwners()
        {
            var result = new List<Guid>();
            var query = @"
SELECT L.Id FROM [System.Activities.DurableInstancing].[LockOwnersTable] L
	LEFT OUTER JOIN [System.Activities.DurableInstancing].[InstancesTable] I ON L.SurrogateLockOwnerId = I.SurrogateLockOwnerId
WHERE I.Id IS NULL";

            using (var cn = new System.Data.SqlClient.SqlConnection(ConnectionStrings.ConnectionString))
            using (var cm = new System.Data.SqlClient.SqlCommand(query, cn) { CommandType = System.Data.CommandType.Text })
            {
                cn.Open();
                using (var reader = cm.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(reader.GetGuid(0));
                }
            }
            return result.ToArray();
        }
        private static void DeleteOrphanedLockOwners(List<Guid> toDelete)
        {
            if (toDelete.Count == 0)
                return;

            var sql = new StringBuilder();
            foreach (var id in toDelete)
                sql.AppendFormat("DELETE FROM [System.Activities.DurableInstancing].[LockOwnersTable] WHERE Id = '{0}'", id).AppendLine();

            ExecuteSqlCommand(sql.ToString());

            SnTrace.Workflow.Write("WF: Deleted orphaned lock owner count:{0}", toDelete.Count);
        }

        private static void ExecuteSqlCommand(string sql)
        {
            using (var cn = new System.Data.SqlClient.SqlConnection(ConnectionStrings.ConnectionString))
            using (var cm = new System.Data.SqlClient.SqlCommand(sql, cn) { CommandType = System.Data.CommandType.Text })
            {
                cn.Open();
                cm.ExecuteNonQuery();
            }
        }

        // =========================================================================================================== RelatedContentProtector

        public static IDisposable CreateRelatedContentProtector(Node node, ActivityContext context)
        {
            return new RelatedContentProtector(node, context);
        }
        private class RelatedContentProtector : IDisposable
        {
            private Node _node;
            private ActivityContext _context;
            public RelatedContentProtector(Node node, ActivityContext context)
            {
                SnTrace.Workflow.Write("WF: RelatedContentProtector instatiating: {0}", node.Path);
                this._node = node;
                this._context = context;
                node.DisableObserver(typeof(WorkflowNotificationObserver));
            }
            private void Release()
            {
                // Load the correct state content by the GUID in the context. Do not use the path from the
                // ContentWorkflowExtension here, because it refers to the same master content!
                var stateContent = GetStateContent(_context.WorkflowInstanceId);

                if (stateContent != null && stateContent.RelatedContent != null && stateContent.RelatedContent.Id == _node.Id)
                {
                    stateContent.RelatedContentTimestamp = _node.NodeTimestamp;
                    using (new SystemAccount())
                        stateContent.Save();
                }
            }


            private bool _disposed;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            private void Dispose(bool disposing)
            {
                if (!this._disposed)
                    if (disposing)
                        this.Release();
                _disposed = true;
            }
            ~RelatedContentProtector()
            {
                Dispose(false);
            }
        }
    }
}
