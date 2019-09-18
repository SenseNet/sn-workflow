using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using IO = System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using SenseNet.Workflow.Tests.Implementations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Workflow.Tests
{
    public class WorkflowTestBase : TestBase
    {
        private static readonly string DatabaseName = "sn7tests_wf";

        private static readonly string ConnectionStringBase =
            @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";

        #region [AssemblyInitialize]

        private static RepositoryInstance _repositoryInstance;

        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext testContext)
        {
            SnTrace.SnTracers.Clear();
            SnTrace.SnTracers.Add(new SnFileSystemTracer());
            SnTrace.SnTracers.Add(new SnWfDebugViewTracer());
            SnTrace.Test.Enabled = true;
            SnTrace.Workflow.Enabled = true;
            SnTrace.Test.Write("------------------------------------------------------");

            var connectionString = GetConnectionString(DatabaseName);
            ConnectionStrings.ConnectionString = connectionString;
            var connectionStringsAcc = new PrivateType(typeof(ConnectionStrings));
            connectionStringsAcc.SetStaticProperty("SecurityDatabaseConnectionString", connectionString);

            PrepareDatabase();

            SenseNet.Configuration.Workflow.TimerInterval = 5.0d / 60.0d;

            var repositoryInstance = Repository.Start(CreateRepositoryBuilder(false));
            using (new SystemAccount())
                PrepareRepository();
            repositoryInstance.Dispose();

            repositoryInstance = Repository.Start(CreateRepositoryBuilder(true));
            using (new SystemAccount())
                InstallInitialRepositoryStructure();
            _repositoryInstance = repositoryInstance;

            //new SnMaintenance().Shutdown();
        }
        [AssemblyCleanup]
        public static void CleanupAssembly()
        {
            _repositoryInstance.Dispose();
        }

        private static void PrepareDatabase()
        {
            // D:\dev\github\sensenet\src\Storage\Data\SqlClient\Scripts\
            var scriptRootPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\..\sensenet\src\Storage\Data\SqlClient\Scripts"));

            // D:\dev\github\sn-workflow\src\Workflow\Data\Scripts\
            var wfScriptRootPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Workflow\Data\Scripts"));

            var dbid = ExecuteSqlScalarNative<int?>($"SELECT database_id FROM sys.databases WHERE Name = '{DatabaseName}'", "master");
            if (dbid == null)
            {
                // create database
                var sqlPath = IO.Path.Combine(scriptRootPath, "Create_SenseNet_Database_Templated.sql");
                string sql;
                using (var reader = new IO.StreamReader(sqlPath))
                    sql = reader.ReadToEnd();
                sql = sql.Replace("{DatabaseName}", DatabaseName);
                ExecuteSqlCommandNative(sql, "master");
            }
            // prepare meta-database
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_Security.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_01_Schema.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_02_Procs.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_03_Data_Phase1.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_04_Data_Phase2.sql"), DatabaseName);

            // prepare workflow-database
            ExecuteSqlScriptNative(IO.Path.Combine(wfScriptRootPath, @"SqlWorkflowInstanceStoreSchema.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(wfScriptRootPath, @"SqlWorkflowInstanceStoreLogic.sql"), DatabaseName);
        }
        private static void ExecuteSqlScriptNative(string scriptPath, string databaseName)
        {
            string sql;
            using (var reader = new IO.StreamReader(scriptPath))
                sql = reader.ReadToEnd();
            ExecuteSqlCommandNative(sql, databaseName);
        }
        private static void ExecuteSqlCommandNative(string sql, string databaseName)
        {
            var cnstr = GetConnectionString(databaseName);
            var scripts = sql.Split(new[] { "\r\nGO", "\r\ngo" }, StringSplitOptions.RemoveEmptyEntries);

            using (var cn = new SqlConnection(cnstr))
            {
                cn.Open();
                foreach (var script in scripts)
                {
                    using (var proc = new SqlCommand(script, cn))
                    {
                        proc.CommandType = CommandType.Text;
                        proc.ExecuteNonQuery();
                    }
                }
            }
        }
        private static T ExecuteSqlScalarNative<T>(string sql, string databaseName)
        {
            var cnstr = GetConnectionString(databaseName);
            using (var cn = new SqlConnection(cnstr))
            {
                cn.Open();
                using (var proc = new SqlCommand(sql, cn))
                {
                    proc.CommandType = CommandType.Text;
                    return (T)proc.ExecuteScalar();
                }
            }
        }
        private static string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName ?? DatabaseName};{ConnectionStringBase}";
        }


        private static void PrepareRepository()
        {
            SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure();
            ContentTypeInstaller.InstallContentType(LoadCtds());
            SaveInitialIndexDocumentsAsync_copy(CancellationToken.None).GetAwaiter().GetResult();
            RebuildIndex_copy();
        }

        private static void InstallInitialRepositoryStructure()
        {
            var admins = Group.Administrators;
            admins.AddMember(User.Administrator);
            admins.Save();

            if (Node.LoadNode(Repository.WorkflowDefinitionPath) == null)
            {
                var workflowsFolder = new SystemFolder(Repository.SystemFolder) { Name = "Workflows" };
                workflowsFolder.Save();
            }

            if (Node.LoadNode("/Root/Localization") == null)
            {
                var localizationFolder = new SystemFolder(Repository.Root, "Resources") { Name = "Localization" };
                localizationFolder.Save();
                var localizationRoot = IO.Path.GetFullPath(IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\..\nuget\snadmin\install-workflow\import\Localization"));
                foreach (var path in IO.Directory.GetFiles(localizationRoot, "*.xml"))
                {
                    var name = IO.Path.GetFileName(path);
                    var resourceNode = new Resource(localizationFolder) {Name = name};
                    resourceNode.Binary.SetStream(new IO.FileStream(path, IO.FileMode.Open));
                    resourceNode.Save();
                }

                var resManAcc = new PrivateType(typeof(SenseNetResourceManager));
                resManAcc.InvokeStatic("Reset");
            }

        }
        private static string[] LoadCtds()
        {
            var ctdRootPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\..\sensenet\src\nuget\snadmin\install-services\import\System\Schema\ContentTypes"));
            var xmlSources = IO.Directory.GetFiles(ctdRootPath, "*.xml")
                .Union(GetWorkflowCtdPaths())
                .Select(p =>
                {
                    using (var r = new IO.StreamReader(p))
                        return r.ReadToEnd();
                })
                .ToArray();
            return xmlSources;
        }

        private static string[] GetWorkflowCtdPaths()
        {
            var importCtdRoot = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\nuget\snadmin\install-workflow\import\System\Schema\ContentTypes"));

            return new[]
            {
                IO.Path.Combine(importCtdRoot, "WorkflowCtd.xml"),
                IO.Path.Combine(importCtdRoot, "WorkflowDefinitionCtd.xml")
            };
        }

        protected static RepositoryBuilder CreateRepositoryBuilder(bool startEngines)
        {
            var blobMetaDataProvider = (IBlobStorageMetaDataProvider)Activator.CreateInstance(typeof(MsSqlBlobMetaDataProvider));

            //TODO: TEST: settingsObserverType is not null
            //var settingsObserverType = Type.GetType("SenseNet.ContentRepository.SettingsCache");

            var builder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                //.StartIndexingEngine(false)
                .StartWorkflowEngine(startEngines)
                .StartIndexingEngine(startEngines)
                .DisableNodeObservers()
                //.EnableNodeObservers(settingsObserverType)
                .UseTraceCategories("Test", "Event", "Custom")
                .UseBlobMetaDataProvider(blobMetaDataProvider)
                .UseBlobProviderSelector(new BuiltInBlobProviderSelector());

            return builder as RepositoryBuilder;
        }
        protected static async Task SaveInitialIndexDocumentsAsync_copy(CancellationToken cancellationToken)
        {
            var idSet = await DataStore.LoadNotIndexedNodeIdsAsync(0, 11000, cancellationToken).ConfigureAwait(false);
            var nodes = Node.LoadNodes(idSet);

            if (nodes.Count == 0)
                return;

            var tasks = nodes.Select(async node =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                await DataStore.SaveIndexDocumentAsync(node, false, false, cancellationToken).ConfigureAwait(false);
            });

            await tasks.WhenAll();
        }

        protected static void RebuildIndex_copy()
        {
            var paths = new List<string>();
            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += (o, e) => { paths.Add(e.Path); };
            populator.ClearAndPopulateAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        #endregion

        #region [TestInitialize]

        [TestInitialize]
        public void InitializeWorkflowTest()
        {
        }

        #endregion

    }
}
