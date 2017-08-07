# Salesforce Reports Connector
This salesforce reports connector allows users to log into Salesforce.com with their Salesforce credentials and pull down any report they have access to into a Qlik Sense app via the load script editor. You can either grab the source code and build the project here, or an installer will be available for download soon after it goes through beta testing.


### Download
Head over to the [releases](https://github.com/eapowertools/SalesforceReportsConnector/releases) page for the latest installer! The latest installer will be the entry at the top of the page, look for the `.msi` file under `Downloads`.


### Installation

1. Once you have downloaded the `.msi` file, double click it to launch the installer.

1. The following screen should appear. You will need to accept the license terms before you can install.
![Installation Window](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/installation.png)

1. When the installer completes, the Salesforce Reports Connector is successfully installed!


### Usage

1. Once you are in the `Data load editor`, you can click `Create new connection` and select `Salesforce Reports Connector`.

![Create new connection](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/chooseConnection.png)

1. The following window will appear. First you will need to enter a connection name (must be unique), then choose a server. If you are unsure, choose `Use Production` as this will use the default server you connect to when you use the web interface at `login.salesforce.com`. If you have a custom server, you can select this option and enter your server hostname. After you are done, click `Ok`.

![Connection Name](https://s3.amazonaws.com/eapowertools/salesforce-reports-connector/imgs/readme/newConnection.png)
