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
                    $scope.salesforceURL = "https://login.salesforce.com/services/oauth2/authorize?response_type=token&client_id=" +
                        "3MVG9i1HRpGLXp.qErQ40T3OFL3qRBOgiz5J6AYv5uGazuHU3waZ1hDGeuTmDXVh_EadH._6FJFCwBCkMTCXk" +
                        "&redirect_uri=https%3A%2F%2Flogin.salesforce.com%2Fservices%2Foauth2%2Fsuccess";

                    $scope.someText = "someConnectionString";
                    $scope.connectionInfo = "";
                    $scope.connectionSuccessful = false;

                    /*input.serverside.sendJsonRequest( "getInfo" ).then( function ( info ) {
                        $scope.info = info.qMessage;
                    } );*/

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
	
                   salesforcelogindialog.show($sce, $scope.salesforceURL).then(function (result) {
                       var connectionString = createCustomConnectionString("SalesforceReportsConnector.exe", "host=" + result['host'] + ";token=blahblah;");
                       console.log(result['name'] + ":" + result['host'] + ":" + result['username'] + ":" + result['password']);
                       input.serverside.createNewConnection(result['name'], connectionString, result['username'], result['password']);

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