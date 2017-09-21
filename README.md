# Salesforce Reports Connector
This salesforce reports connector allows users to log into Salesforce.com with their Salesforce credentials and pull down any report they have access to into a Qlik Sense app via the load script editor. You can either grab the source code and build the project here, or follow the download instructions for an installer.

[Download](#download)  
[Installation](#installation)  
[Setup](#setup)  
[Limitations](#limitations)  

### Download
Head over to the [releases](https://github.com/eapowertools/SalesforceReportsConnector/releases) page for the latest installer! The latest installer will be the entry at the top of the page, look for the `.msi` file under `Downloads`.


### Installation

1. Once you have downloaded the `.msi` file, double click it to launch the installer.

1. The following screen should appear. You will need to accept the license terms before you can install.  
![Installation Window](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/installation.png)

1. When the installer completes, the Salesforce Reports Connector is successfully installed! _If you are running Qlik Sense Desktop, you will need to restart Qlik Sense Desktop for the connector to appear._


### Setup

1. Once you are in the `Data load editor`, you can click `Create new connection` and select `Salesforce Reports Connector`.  
![Create new connection](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/chooseConnection.png)

1. The following window will appear. First you will need to enter a connection name (must be unique), then choose a server. If you are unsure, choose `Use Production` as this will use the default server you connect to when you use the web interface at `login.salesforce.com`. If you have a custom server, you can select this option and enter your server hostname. After you are done, click `Ok`.  
![Connection Name](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/newConnectionName.png)

1. You will now need to login to salesforce so you can allow the connector to access your reports. When you see the following screen, click the `Login/Authenticate` button. This will open a new window in your default web browser on your machine.  
![Login](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/emptyAuth.png)

1. Upon clicking `Login/Authenticate`, if you are *NOT* logged in, you will see the following screen. You will need to login here. If you are logged in, go directly to the next step.  
![Salesforce Login](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/salesforceLogin.png)

1. Once you are logged in, you will see the following salesforce page. You will need to click the `Allow` button to allow the connector to access your reports. If you are signed in as the wrong user, click `Not You?`.
![Authorize](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/apiAuth.png)

1. After clicking `Allow`, you will reach an empty white page with the text `Remote Access Application Authorization`. Copy the **ENTIRE** URL `http://...` as you will need this for the next step.
![Access Granted](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/authGranted.png)

1. Go back to Qlik Sense and the `Salesforce Login and Authentication` window should still be open. Paste the entire url copies from your salesforce page into box. The text underneath should turn green and read _valid URL_.
![Complete](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/completedAuth.png)

1. Click finish! At this point the connector should appear on the right under your list of connectors. You can then go and click select data!


### Limitations

1. The Salesforce API limits the amount of rows to be fetched to 2000 rows. This means you can only load 2000 per report beacuse of a Salesforce limitation.
