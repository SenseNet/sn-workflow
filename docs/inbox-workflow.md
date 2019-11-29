# Sending Emails to Content Lists

![Sending emails to content lists](https://raw.githubusercontent.com/SenseNet/sn-workflow/master/docs/images/workflow-inbox-list.png "Sending emails to content lists")

In sensenet lists and libraries can be configured to receive emails from *Microsoft Exchange Server* or any other email server that enables connecting through *POP3* protocol. An email address can be bound to lists and libraries, thus emails can be sent directly to them (e.g. to document libraries and memo lists), with the attachments being saved as files. Optionally the emails themselves can be saved too. 

In this article you can learn how to configure content lists and libraries to be able to receive attachments sent by email. 

> This article focuses mainly on **Exchange** integration, however it is also possible to use the well-known **POP3** protocol for receiving emails through any email server that is capable of publishing emails through POP3 (e.g. GMail, Hotmail) See the [Connecting through POP3](#connectpop3) section below.

- lists can be configured to receive emails from an existing mailbox
- the feature is configurable on every list separately
- processing of incoming emails can be fine adjusted by creating a custom mail processor workflow based on the built-in one
- built-in workflow activities are available for developers to connect to a mail server and receive mails
- there is a simple API for developers for retrieving mails, Exchange push/pull-notifications and subscription handling

## Connecting to an Exchange server
sensenet can be connected to a Microsoft Exchange server in two modes:
- **push notifications**: an Exchange server can send notifications about incoming emails to a web service defined in sensenet. A custom workflow can be defined to process the incoming emails. 
- **polling unread mails**: a custom workflow can be defined to retrieve unread emails from an Exchange mailbox with a given time interval.

The difference between the two methods is summarized in the following table:


|   | PRO  | CON  |
|---|---|---|
| Push notifications  | - Nearly instantaneous notifications <br/> - No wasted traffic  | - Listener must be accessible by the Exchange server <br/> - More complex configuration  |
| Email polling  | - Client does not need to be accessible (can be behind a proxy or firewall) <br/> - Easy to configure | - Receive notifications only as frequently as the client polls <br/> - Wasted traffic |

sensenet supports both notification types when an email address is bound to a Content List. The **default method is pull notifications**, but this can easily be changed by changing the workflow that processes incoming emails.

## Configuring pull notifications
The mailbox can be defined by giving the email address in the *Incoming Email Settings* of a Content List. A workflow is started after the mailbox address is given that runs periodically and retrieves unread mails from the Exchange Server. This workflow then processes the incoming emails and creates new Content in the Content List according to the Incoming Email Settings.

> To configure the portal to use email **polling**, you simply have to give your lists an email address (see below), and optionally set the *ExchangeAddress* element in the settings content to use a specific mail server address instead of using autodiscover.

> Make sure the user running the **app pool** of the application has the necessary **permissions** to access the Exchange mailbox.

## Configuring push notifications
To enable push notifications the system needs to subscribe to a mailbox. This mailbox can be defined by giving the email address in the *Incoming Email Settings* of a Content List. Then, whenever a new email is sent to the defined mailbox in Exchange, the Exchange server sends a *notification* to the web service - whose url address was given with the subscription automatically - with info about the new email. The web service runs in the portal context of sensenet and creates a content under the list in the *IncomingEmails* SystemFolder persisting the ID of the incoming email. The workflow that runs in the background processes the incoming email and creates new Content in the Content List according to the *Incoming Email Settings*.

> To configure the portal to use push notifications, you will have to set the *MailProcessingMode* to *ExchangePush*, *PushNotificationServicePath* and optionally the *ExchangeAddress* elements in the settings content, and also set the *PushNotification* parameter of the *Mail Processor Workflow*'s *ExchangePoller* activity to *True*. Then you can go on an set the email addresses of your lists.

> Make sure the user running the **app pool** of the application has the necessary **permissions** to access the Exchange mailbox.

## Settings
To enable Exchange Integration for either push notification or email polling, or using POP3 protocol, you need to modify the settings in the following Setting file:

* */Root/System/Settings/MailProcessor.settings*

```json
{
	MailProcessingMode: "ExchangePull",
	StatusPollingIntervalInMinutes: 120,
	PushNotificationServicePath: "http://example.com{0}?action=ExchangeService.asmx",
	ExchangeAddress: "https://exchangeserveraddress/EWS/Exchange.asmx",
	POP3: {
		Server: "pop3.live.com",
		Password: "",
		Port: 995,
		SSL: true
	}
}
```

The following settings are available:
- **MailProcessingMode**: controls how the system gets the emails sent to lists. Possible values are:
   - *ExchangePull*: the system polls the Exchange server for new mails periodically. You can modify the time period in the customizable workflow as it is described later in this article.
   - *ExchangePush*: a logic will be executed on every system startup that re-subscribes to all mailboxes for every Content List that is configured to receive mails. This can be useful when the portal has been unavailable for the Exchange server and therefore the server deleted the subscription. Also a subscription to the mailbox events in Exchange Server will occur after setting the email address for a List. 
   - *POP3*: the system connects to the mail server using the POP3 protocol. This is a simple method for getting emails that needs a mailbox on the *Incoming Email Settings* page and the password in the settings file. All mails that were sent to that mailbox will be processed and deleted.
- **StatusPollingIntervalInMinutes**: used with push notifications. For active subscriptions the Exchange server sends a status message to the webservice, to check if the receiver is still available. These messages are ignored - but otherwise served to indicate available service - by sensenet. Set it to a higher value if you want to receive these status checks less frequently. The polling interval will be in effect for new subscriptions.
- **PushNotificationServicePath**: used with push notifications. The path of the web service handling push notifications. A built-in service application is placed at */Root/(apps)/ContentList/ExchangeService.asmx*, this will be addressed by the exchange server. The path here therefore should include the host of sensenet, and the rest will be handled by the portal (the '{0}' pattern will be replaced with the path of the appropriate Content List, and the *?action=ExchangeService.asmx* will instruct the portal to run the webservice defined for Content Lists among the global applications).
- **ExchangeAddress**: the address of the exchange EWS web services. It is usually in the given format, only the host of the smtp server needs to be adjusted. If this key is commented out the autodiscover services of exchange will be used to resolve the address of the appropriate Exchange server.
- **POP3Server**: POP address of the email server, e.g. pop.gmail.com.
- **POP3 password**: if POP3 mode is configured, you have to provide the password for all the mailboxes here to let the system check for emails periodically.
- **POP3Port**: port number, default is *995*.
- **POP3SSL**: *True* if SSL connection is needed.

## List Configuration - Incoming Email Settings
To bind an email address to a Content List, you need to access the *Incoming Email Settings* page through the *Settings* menu of the List.

![Incoming Email Settings](https://raw.githubusercontent.com/SenseNet/sn-workflow/master/docs/images/workflow-inbox-gui-settings.png "Incoming Email Settings")

On this interface the following settings are available:
- **Email address of Content List**: the email address of the existing mailbox that will act as the email address of this List. If the corresponding mailbox does not exist, create it beforehand.
- **Group attachments**: the way attachments of incoming emails should be grouped when put into the Content List. The following options are available by default:
   - **Save all attachments as children of separate Email content**: a content of the *Email* content type will be created for each email, with the subject of the email as its name; attachments will be put under this content. The Email content will have the Sender, the Subject, the Body and the Sent date of the incoming email in its Fields.
   - **Save all attachments in root**: no container will be created and all attachments of every email will be put into the Content List directly.
   - **Save all attachments in folders grouped by subject**: a Folder with the name of the subject of the incoming email will be created, and attachments will be put into the corresponding Folder. Attachments of emails with the same subject will be put into the same Folder.
   - **Save all attachments in folders grouped by sender**: a Folder with the name of the sender of the incoming email will be created, and attachments will be put into the corresponding Folder. Attachments of emails from the same sender will be put into the same Folder.
- **Inbox folder**: a relative path of the folder in the list where you want to place emails and attachments. By default it is empty; you can set it to a relative path like *emails* or *emails/subfolder*. If the path does not exist, it will be created automatically by the workflow, using the defined container type (default: *Folder*) defined in the workflow.
- **Save original email**: if this is set to true, a file with the extension *.eml* will be created containing the incoming email, and will be put into the same folder to which the attachments of the email go, according to the grouping selected above.
- **Overwrite files with same name**: if this is checked, existing files with the same name will be overwritten. If left unchecked, filenames with incremental suffix will be used when a file with the same name already exists in the target folder or Content List.
- **Accept emails only from users in local groups**: if you check this, only users that are members of local groups will be able to send emails to the list. The Sender (or From) email address will be checked for this.
- **Incoming email workflow**: by default the system launches the *MailProcessor workflow* that will create the necessary content for the incoming email and the attachments according to the above given configuration. This is the only built-in workflow for this job, however a custom workflow can be created for this, inheriting from the *MailProcessorWorkflow*, and then can be selected here. Also, the MailProcessorWorkflow can be configured for both push / pull notifications - by default it uses pull notifications.

> Make sure that the referred mailbox exists in Exchange Server and the user running the app pool of the sensenet application has the necessary permissions to access it. In case of push notifications if the subscription cannot be created an error message will appear at the top of the view.

> Make sure that the selected Content Types can be created in the Content List, so Folders, Files or Email Content Types are listed in the Content List's *Allowed Content Types* field!

## Mail Processor Workflow
sensenet comes with a built-in *Mail processor workflow* that is executed when setting a mailbox for a list and runs continously in the background. This can be exchanged to a user-defined custom workflow at any time, in the Incoming Email Settings for a Content List. 

<a name="connectpop3"></a>
## Connecting through POP3
POP3 protocol is a widely used protocol for accessing mailboxes. It is easy to configure: you only have to set the MailProcessingMode, the POP3 address and port number published by the mail server, and the password in the MailProcessor Settings as described above. Than you can provide the necessary mailbox on the *Inoming email settings* page of the content list.

> As the protocol does not know the concept of *read* emails, **processed emails are deleted immediately from the server**. If you want to keep a copy of the mail in the content repository, you can choose the *Save original email* option on the settings page.

## Managing push subscriptions
When subscribing to notifications for a mailbox the Microsoft Exchange Server will start sending status messages periodically with the configured time interval. It is possible to unsubscribe from a mailbox event (delete the subscription) by answering to a status message with the appropriate message. A subscription is automatically deleted if the Exchange server cannot access the webservice running on sensenet for a definite time interval. Therefore it is quite cumbersome to determine if a subscription is still active or not. sensenet provides you the following means to manage subscriptions:

- **Deleting subscriptions**: to delete an existing subscription, simply erase the contents of the List Email Field in the Incoming Email Settings and save the settings. This way the next status message (or new mail notification) sent by the Exchange server will not be processed and a return message will be sent to the Exchange server to delete the subscription.

- **Reactivating subscriptions**: if the portal has been down for a given period of time it might happen that the Exchange server has deleted the subscription and will not send any notifications about incoming emails. If this happens there are two options to renew the subscriptions:
   - **automatically:** configure the subscription service in the settings file so that when the portal is restarted it will automatically re-subscribe to every defined mailbox in the system. Re-subscription on active subscriptions does not cause problems since in this case the previous subscriptions will automatically be deleted with the next status message (or new mail message).
   - **manually:** go to the Incoming Email Settings of the Content List, and erase the contents of the List Email email Field. Save the settings and then once again open the settings and fill in the List Email. Upon saving the settings once again a new subscription will take place to the events of the given Exchange mailbox.

#### Notification on unprocessed mails
Receiving unread emails that arrived in the mailbox while the portal or the connection between the portal and the Exchange server was down is done by using watermark indications. Every time a new email is processed in the watermark provided by the Exchange server is saved on the IncomingEmails SystemFolder under the list. This watermark identifies the last processed email. When renewing subscriptions the watermark of the lastly created workflow is sent to the Exchange server, and according to this information the Exchange server will immediately send notifications of emails that arrived after the email that corresponds to the specified watermark - in other words sensenet is notified of all the yet unprocessed emails.

> Please note, that the last watermark is persisted on the IncomingEmails SystemFolder (in its *Description* field) under the list, therefore if you delete this folder you will not get notifications of unprocessed mails upon the very next subscription.

## Troubleshooting
To troubleshoot any problems emerging with Exchange Integration, read the following article:

- [Troubleshooting Exchange Integration](http://wiki.sensenet.com/Troubleshooting_Exchange_Integration)