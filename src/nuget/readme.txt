************************************************************************************
                                 sensenet platform
                                      Workflow
************************************************************************************

To finalize the installation and get started with sensenet Workflow, please follow these steps:

(install guide is accessible online at: http://community.sensenet.com/docs/install-workflow-from-nuget)

1. Build your solution, make sure that there are no build errors.

2. Install sensenet Workflow using SnAdmin

   - open a command line and go to the \Admin\bin folder
   - execute the install-workflow command with the SnAdmin tool
      - optional parameters:
         - overwriteemailworkflow: if you execute the package with a 'false' value for this parameter,
								   it will set the default mail processor workflow on content lists
								   only if the 'Incoming email workflow' field is empty and will not
								   overwrite existing references. Default is true.

   .\snadmin install-workflow   

You are good to go! Hit F5 and start creating workflows!
For more information and support, please visit http://sensenet.com