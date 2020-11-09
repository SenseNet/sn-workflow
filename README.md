# Legacy feature alert

Please note that this is a legacy feature which is no longer supported on the latest (and greatest) sensenet version.

# sensenet as a service (SNaaS) - use sensenet from the cloud

For a monthly subscription fee, we store all your content and data, relieving you of all maintenance-related tasks and installation, ensuring easy onboarding, easy updates, and patches.

https://www.sensenet.com/pricing

# Workflow for sensenet
Workflow component for the [sensenet](https://github.com/SenseNet/sensenet) platform, based on _Windows Workflow Foundation 4.5_

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Workflow.Install.svg)](https://www.nuget.org/packages/SenseNet.Workflow.Install)

You may install this component even if you only have the **sensenet Services** main component installed. That way you'll get the workflow engine and the backend part of the built-in workflows.

If you also have the [sensenet WebPages](https://github.com/SenseNet/sn-webpages) component installed (which gives you a UI framework built on *ASP.NET WebForms* for sensenet), you'll also get UI elements for workflows:

- content list integration
- views for managing workflows
- front-end parts of the built-in workflows

The built-in workflows (_approval, registration, forgotten password_) will work without the UI layer, but you'll have to make the necessary content modifications (e.g. setting the result of a task) that these workflows expect programmatically (or on your custom UI) to move them forward.

> To find out which packages you need to install, take a look at the available [sensenet components](https://github.com/SenseNet/sensenet.github.io/blob/master/_docs/sensenet-components.md).

> To learn more about workflows in sensenet, visit the [main Workflow article](/docs/workflow.md).

# sensenet as a service (SNaaS) - use sensenet from the cloud

For a monthly subscription fee, we store all your content and data, relieving you of all maintenance-related tasks and installation, ensuring easy onboarding, easy updates, and patches.

https://www.sensenet.com/pricing