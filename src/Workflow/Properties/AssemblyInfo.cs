﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Workflow.Tests")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.Workflow (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.Workflow (Release)")]
#endif

[assembly: AssemblyDescription("Workflow component for the sensenet platform.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyProduct("sensenet")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("7.3.0")]
[assembly: AssemblyFileVersion("7.3.0")]
[assembly: AssemblyInformationalVersion("7.3.0")]

[assembly: ComVisible(false)]
[assembly: Guid("6b47b471-efdf-4f7b-82b7-bcea425d2a78")]