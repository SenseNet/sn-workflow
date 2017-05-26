using System;
using System.Collections.Generic;
using System.Activities.Hosting;
using System.Linq;

namespace SenseNet.Workflow
{
    public class ContentWorkflowExtension : IWorkflowInstanceExtension
    {
        public WorkflowInstanceProxy _instance;

        public string WorkflowInstancePath { get; set; }
        public string ContentPath { get; set; }

        private string _uid;

        public string UID
        {
            get 
            {
                if (string.IsNullOrEmpty(_uid))
                    _uid = Guid.NewGuid().ToString();
                return _uid; 
            }
            set { _uid = value; }
        }
        
        public IEnumerable<object> GetAdditionalExtensions()
        {
            yield break;
        }

        public int RegisterWait(WfContent content, string bookMarkName)
        {
            var myGuid = _instance.Id;
            var wfNodePath = WorkflowInstancePath;
            return InstanceManager.RegisterWait(content.Id, myGuid, bookMarkName, wfNodePath);
        }

        public void ReleaseWait(int notificationId)
        {
            InstanceManager.ReleaseWait(notificationId);
        }

        public int[] RegisterWaitForMultipleContent(IEnumerable<WfContent> contents, string bookMarkName)
        {
            var myGuid = _instance.Id;
            var wfNodePath = WorkflowInstancePath;

            return contents.Select(content => 
                InstanceManager.RegisterWait(content.Id, myGuid, bookMarkName, wfNodePath)).ToArray();
        }

        public void SetInstance(WorkflowInstanceProxy instance)
        {
            _instance = instance;
        }
    }
}
