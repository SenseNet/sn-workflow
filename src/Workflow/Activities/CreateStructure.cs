using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Workflow.Activities.Design;
using SenseNet.Diagnostics;

namespace SenseNet.Workflow.Activities
{
    [Designer(typeof(CreateStructureDesigner))]
    public class CreateStructure : NativeActivity<WfContent>
    {
        public InArgument<string> FullPath { get; set; }
        public InArgument<string> ContainerTypeName { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            using (new SystemAccount())
            {
                var content = Content.Load(FullPath.Get(context));
                if (content != null)
                {
                    // return the leaf content if exists
                    Result.Set(context, new WfContent(content.ContentHandler));
                    return;
                }

                // create structure
                var ctName = ContainerTypeName.Get(context);
                
                content = string.IsNullOrEmpty(ctName)
                    ? RepositoryTools.CreateStructure(FullPath.Get(context)) 
                    : RepositoryTools.CreateStructure(FullPath.Get(context), ctName);

                Result.Set(context, new WfContent(content.ContentHandler));
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            SnTrace.Workflow.Write("CreateStructure.Cancel: {0}", FullPath.Get(context));
            base.Cancel(context);
        }
    }
}
