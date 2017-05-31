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

                    $scope.hasName = false;
                    $scope.hasCustomURL = false;
                    $scope.radioButtonValue = "production";
                    $scope.connectorName = "";
                    $scope.salesforceURL = "https://login.salesforce.com/services/oauth2/authorize?response_type=token&client_id=" +
                        "3MVG9i1HRpGLXp.qErQ40T3OFL3qRBOgiz5J6AYv5uGazuHU3waZ1hDGeuTmDXVh_EadH._6FJFCwBCkMTCXk" +
                        "&redirect_uri=https%3A%2F%2Flogin.salesforce.com%2Fservices%2Foauth2%2Fsuccess";
                    $scope.URL = "";

                    $scope.customURLStatus = "Enter custom URL here.";
                    $scope.urlStatusColour = "red";

                };

                /* Event handlers */

                $scope.onEscape = $scope.onCancelClicked = function () {
                    $scope.destroyComponent();
                };

                $scope.showLogin = function () {
                    salesforcelogindialog.show( $sce, $scope.salesforceURL ).then( function ( result ) {
                        var connectionString = createCustomConnectionString( "SalesforceReportsConnector.exe", "host=" + result['host'] + ";token=blahblah;" );
                        console.log( result['name'] + ":" + result['host'] + ":" + result['username'] + ":" + result['password'] );
                        input.serverside.createNewConnection( result['name'], connectionString, result['username'], result['password'] );

                        $scope.destroyComponent();
                    } );
                };

                $scope.nameChange = function () {
                    if ( $scope.connectorName == "" ) {
                        $scope.hasName = false;
                    } else {
                        $scope.hasName = true;
                    }
                };

                $scope.urlChange = function () {
                    if ($scope.URL == "") {
                        $scope.hasCustomURL = false;
                        $scope.urlStatusColour = "red";
                        $scope.customURLStatus = "Enter custom URL here.";
                    } else {
                        var response = validateCustomURL($scope.URL);
                        if (response['isValid']) {
                            $scope.hasCustomURL = true;
                            $scope.urlStatusColour = "green";
                            $scope.customURLStatus = "URL is valid.";
                        } else {
                            $scope.hasCustomURL = false;
                            $scope.urlStatusColour = "red";
                            $scope.customURLStatus = response['message'];
                        }
                    }
                };

                $scope.radioSelection = function () {
                    //$scope.connectorName = $scope.radioButtonValue;
                };


                /* Helper functions */

                function createCustomConnectionString( filename, connectionstring ) {
                    return "CUSTOM CONNECT TO " + "\"provider=" + filename + ";" + connectionstring + "\"";
                };

                function validateCustomURL(url) {
                    var response = {
                        isValid: true,
                        message: ""
                    }
                    url = url.toLowerCase();
                    if ( !url.startsWith( 'https://' ) ) {
                        response['isValid'] = false;
                        response['message'] = "Host must be secure (https://).";
                    }
                    else if (!(url.endsWith('.salesforce.com') || url.startsWith('.salesforce.com/'))) {
                        response['isValid'] = false;
                        response['message'] = "Server must be on the salesforce domain (*.salesforce.com).";
                    }
                    return response;
                };

                init();
            }
        ]
    };
} );