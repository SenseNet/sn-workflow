using System;
using System.Collections.Generic;
using System.Linq;
using System.Activities;
using Microsoft.Exchange.WebServices.Data;
using SenseNet.ContentRepository.Mail;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Search;

namespace SenseNet.Workflow.Activities
{

    public sealed class ExchangePoller : AsyncCodeActivity<EmailMessage[]>
    {
        public InArgument<bool> PushNotification { get; set; }
        public InArgument<string> ContentListPath { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var pushNotification = PushNotification.Get(context);
            var contentListPath = ContentListPath.Get(context);

            if (string.IsNullOrEmpty(contentListPath))
            {
                SnLog.WriteError("ExchangePoller activity: ContentList path is empty.", categories: ExchangeHelper.ExchangeLogCategory);
                return null;
            }

            var GetMessageInfosDelegate = new Func<bool, string, EmailMessage[]>(GetMessageInfos);
            context.UserState = GetMessageInfosDelegate;
            return GetMessageInfosDelegate.BeginInvoke(pushNotification, contentListPath, callback, state);            
        }

        protected override EmailMessage[] EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var getMessageInfosDelegate = (Func<bool, string, EmailMessage[]>)context.UserState;
            return getMessageInfosDelegate.EndInvoke(result);            
        }

        private EmailMessage[] GetMessageInfos(bool pushNotification, string contentListPath)
        {
            var contentList = Node.LoadNode(contentListPath);
            var mailboxEmail = contentList["ListEmail"] as string;

            if (!pushNotification)
            {
                var service = ExchangeHelper.CreateConnection(mailboxEmail);
                if (service == null)
                    return new EmailMessage[0];

                var items = ExchangeHelper.GetItems(service, mailboxEmail);
                var infos = items.Select(item => EmailMessage.Bind(service, item.Id, new PropertySet(BasePropertySet.FirstClassProperties, ItemSchema.Body, ItemSchema.Attachments, ItemSchema.MimeContent)));

                return infos.ToArray();
            }
            else
            {
                var incomingEmails = Search.ContentQuery.Query(ContentRepository.SafeQueries.InFolder,
                    new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                    RepositoryPath.Combine(contentListPath, ExchangeHelper.PUSHNOTIFICATIONMAILCONTAINER));

                if (incomingEmails.Count == 0)
                    return new EmailMessage[0];

                var service = ExchangeHelper.CreateConnection(mailboxEmail);
                if (service == null)
                    return new EmailMessage[0];

                var msgs = new List<EmailMessage>();
                foreach (var emailnode in incomingEmails.Nodes)
                {
                    var ids = emailnode["Description"] as string;
                    if (string.IsNullOrEmpty(ids))
                        continue;

                    var idList = ids.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    msgs.AddRange(idList.Select(id =>
                        EmailMessage.Bind(service, id,
                            new PropertySet(BasePropertySet.FirstClassProperties,
                                ItemSchema.Body, ItemSchema.Attachments, ItemSchema.MimeContent))));

                    // delete email node 
                    emailnode.ForceDelete();
                }
                return msgs.ToArray();
            }
        }
    }
}
