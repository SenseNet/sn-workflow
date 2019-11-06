# Install sensenet Workflow from NuGet

This article is **for developers** about installing the **Workflow** component for [sensenet](https://github.com/SenseNet) from NuGet. Before you can do that, please install at least the core layer, [sensenet Services](https://github.com/SenseNet/sensenet/tree/master/docs/install-sn-from-nuget.md), which is a prerequisite of this component.

>About choosing the components you need, take look at [this article](https://github.com/SenseNet/sensenet/tree/master/docs/sensenet-components.md) that describes the main components and their relationships briefly.

![sensenet Workflow](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_workflows.png "sensenet Workflow")

## What is in the package?
### Workflow engine and API
You will be able to use the full workflow engine and API, even if you only have the Services layer installed. Starting/stopping workflows, automatic wake-up and polling will work.
### Built-in workflows
The following workflows are included in the package:

- [approval](http://wiki.sensenet.com/Approval_workflow_sample)
- [public registration](http://wiki.sensenet.com/Registration_workflow_sample)
- forgotten password

These are simple workflows that contain only a couple of activities, but they are good examples and starting points of a more complex business case.

### Rich user interface
If you have the [WebPages](https://github.com/SenseNet/sn-webpages) layer, you will get the necessary views and actions for starting and aborting workflows and the predefined UI elements for the built-in workflows above.
### Content List integration
If you also have the [Workspaces](https://github.com/SenseNet/sn-workspaces) component installed, you will be able to assign workflows to your content lists and use the [Mail processor workflow](/docs/inbox-workflow.md) that lets your users send emails (with extracted attachments) directly into your document libraries.

## Installing the NuGet packages in Visual Studio
### Main Workflow package
To get started, **stop your web site** and install the workflow 'install' package the usual way:

1. Open your **web application** that already contains the *Services* component installed in *Visual Studio*.
2. Install the following NuGet package (either in the Package Manager console or the Manage NuGet Packages window)

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Workflow.Install.svg)](https://www.nuget.org/packages/SenseNet.Workflow.Install)

> `Install-Package SenseNet.Workflow.Install -Pre`

### Dll-only package in other projects
If you need to work with the Workflow API in another project (a class library), you should install the dll-only package (as opposed to the main package above that contains import material):

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Workflow.svg)](https://www.nuget.org/packages/SenseNet.Workflow)

> `Install-Package SenseNet.Workflow -Pre`

### Workflow UI elements: the optional Portlets package
If you have the [sensenet WebPages](https://github.com/SenseNetsn-webpages) component installed, **you have to install a third workflow NuGet package** that contains **portlets and other UI elements** for workflows:

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Workflow.Portlets.svg)](https://www.nuget.org/packages/SenseNet.Workflow.Portlets)

> `Install-Package SenseNet.Workflow.Portlets -Pre`

If you do not install the package above, you may encounter errors on user interfaces that need these portlets to function properly.

## Installing the workflow component
To complete the install process, please execute the *install-workflow* SnAdmin command as usual:

1. Open a command line and go to the *[web]\Admin\bin* folder of your project.
2. Execute the install-workflow command with the SnAdmin tool.

```text
.\snadmin install-workflow
```

##### Optional parameters:
- *overwriteemailworkflow*: if you execute the package with a 'false' value for this parameter, it will set the default mail processor workflow on content lists only if the 'Incoming email workflow' field is empty and will not overwrite existing references. Default is *true*.

If there were no errors, you are good to go! Hit F5 in Visual Studio and start experimenting with sensenet Workflow!

## Troubleshooting
If you encounter one of these errors after installing the workflow component, please take a look at the possible solutions below.
### 1. Could not load type StartWorkflowPortlet
If you see a similar error message on one of the pages, please make sure you installed the *SenseNet.Workflow.Portlets* package described above.