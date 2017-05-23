define( [
    'qvangular',
    'text!SalesforceReportsConnector.webroot/connectdialog.ng.html',
    'css!SalesforceReportsConnector.webroot/connectdialog.css',
    'SalesforceReportsConnector.webroot/salesforcelogindialog'
], function ( qvangular, template, css, salesforcelogindialog ) {
    return {
        template: template,
        controller: [
            '$scope', '$sce', 'input', function ( $scope, $sce, input ) {
                function init() {
                    $scope.isEdit = input.editMode;
                    $scope.id = input.instanceId;
                    $scope.titleText = "Load Data Example";

                    $scope.someText = "someConnectionString";
                    $scope.provider = "introConnector.exe"; // Connector filename
                    $scope.connectionInfo = "";
                    $scope.connectionSuccessful = false;
                    $scope.connectionString = createCustomConnectionString( $scope.provider, "host=localhost;" );

                    input.serverside.sendJsonRequest( "getInfo" ).then( function ( info ) {
                        $scope.info = info.qMessage;
                    } );

                }


                /* Event handlers */

                $scope.loadData = function () {

                    input.serverside.createNewConnection( $scope.someText, $scope.connectionString );
                    $scope.destroyComponent();
                };

                $scope.onEscape = $scope.onCancelClicked = function () {
                    $scope.destroyComponent();
                };


                $scope.showLogin = function () {
                   var url = "https://login.salesforce.com/services/oauth2/authorize?response_type=token&client_id=" +
                        "3MVG9i1HRpGLXp.qErQ40T3OFL3qRBOgiz5J6AYv5uGazuHU3waZ1hDGeuTmDXVh_EadH._6FJFCwBCkMTCXk" +
                        "&redirect_uri=https%3A%2F%2Flogin.salesforce.com%2Fservices%2Foauth2%2Fsuccess";
                    
	
                    salesforcelogindialog.show( $sce, url ).then( function () {
                        $scope.destroyComponent();
                    });
                }


                /* Helper functions */

                function createCustomConnectionString( filename, connectionstring ) {
                    return "CUSTOM CONNECT TO " + "\"provider=" + filename + ";" + connectionstring + "\"";
                }


                init();
            }
        ]
    };
} );