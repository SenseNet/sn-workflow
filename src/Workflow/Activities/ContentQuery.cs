using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace SenseNet.Workflow.Activities
{
    public class ContentQuery : AsyncCodeActivity<SenseNet.Search.QueryResult>
    {
        public InArgument<string> QueryText { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var queryText = QueryText.Get(context);

            var queryDelegate = new Func<string, SenseNet.Search.QueryResult>(RunQuery);
            context.UserState = queryDelegate;
            return queryDelegate.BeginInvoke(queryText, callback, state);
        }

        protected override SenseNet.Search.QueryResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var queryDelegate = (Func<string, SenseNet.Search.QueryResult>)context.UserState;
            return (SenseNet.Search.QueryResult)queryDelegate.EndInvoke(result);
        }

        private SenseNet.Search.QueryResult RunQuery(string text)
        {
            return SenseNet.Search.ContentQuery.Query(text);
        }
    }
}
