# Workflow for sensenet ECM
Workflow component for the [sensenet ECM](https://github.com/SenseNet/sensenet) platform, based on _Windows Workflow Foundation 4.5_

[![Join the chat at https://gitter.im/SenseNet/sn-workflow](https://badges.gitter.im/SenseNet/sn-workflow.svg)](https://gitter.im/SenseNet/sn-workflow?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![NuGet](https://img.shields.io/nuget/v/SenseNet.Workflow.Install.svg)](https://www.nuget.org/packages/SenseNet.Workflow.Install)

You may install this component even if you only have the **sensenet ECM Services** main component installed. That way you'll get the workflow engine and the backend part of the built-in workflows.

If you also have the [sensenet ECM WebPages](https://github.com/SenseNet/sn-webpages) component installed (which gives you a UI framework built on *ASP.NET WebForms* for sensenet ECM), you'll also get UI elements for workflows:

- content list integration
- views for managing workflows
- front-end parts of the built-in workflows

The built-in workflows (_approval, registration, forgotten password_) will work without the UI layer, but you'll have to make the necessary content modifications (e.g. setting the result of a task) that these workflows expect programmatically (or on your custom UI) to move them forward.

> To find out which packages you need to install, take a look at the available [sensenet ECM components](http://community.sensenet.com/docs/sensenet-components).

> To learn more about workflows in sensenet ECM, visit the [main Workflow article](/docs/workflow.md).

## Installation
To get started, install the Workflow component from NuGet:
- [Install sensenet ECM Workflow from NuGet](/docs/install-workflow-from-nuget.md)