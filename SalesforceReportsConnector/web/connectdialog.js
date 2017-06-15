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
                    $scope.URL = "";

                    $scope.customURLStatus = "Enter custom URL here.";
                    $scope.urlStatusColour = "red";

                };

                /* Event handlers */

                $scope.onEscape = $scope.onCancelClicked = function () {
                    $scope.destroyComponent();
                };

                $scope.showLogin = function () {
                    if ( $scope.radioButtonValue == "production" ) {
                        $scope.URL = "https://login.salesforce.com/";
                    }

                    input.serverside.sendJsonRequest("API-getSalesforcePath", $scope.URL).then(function (info) {
                        var url = info.qMessage;
                        salesforcelogindialog.show($sce, url).then(function (result) {
                            $scope.destroyComponent();
                            input.serverside.sendJsonRequest("API-getUsername", $scope.URL, result['access_token'], result['refresh_token'], result['instance_url'], result['id']).then(function (info) {
								var results = JSON.parse( info.qMessage );
                                var username = results['username'];
                                var host = results['host'];
                                input.serverside.sendJsonRequest( "API-BuildConnectionString", host, $scope.URL, result['access_token'], result['refresh_token'] ).then( function ( connString ) {
                                    var connectionString = createCustomConnectionString( "SalesforceReportsConnector.exe", connString.qMessage );
                                    input.serverside.createNewConnection( $scope.connectorName, connectionString, username );
                                } );
                            });
                        });


                    });



                };

                $scope.nameChange = function () {
                    if ( $scope.connectorName == "" ) {
                        $scope.hasName = false;
                    } else {
                        $scope.hasName = true;
                    }
                };

                $scope.urlChange = function () {
                    if ( $scope.URL == "" ) {
                        $scope.hasCustomURL = false;
                        $scope.urlStatusColour = "red";
                        $scope.customURLStatus = "Enter custom URL here.";
                    } else {
                        var response = validateCustomURL( $scope.URL );
                        if ( response['isValid'] ) {
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

                /* Helper functions */

                function createCustomConnectionString( filename, connectionstring ) {
                    return "CUSTOM CONNECT TO " + "\"provider=" + filename + ";" + connectionstring + "\"";
                };

                function validateCustomURL( url ) {
                    var response = {
                        isValid: true,
                        message: ""
                    }
                    url = url.toLowerCase();
                    if ( !url.startsWith( 'https://' ) ) {
                        response['isValid'] = false;
                        response['message'] = "Host must be secure (https://).";
                    } else if ( !( url.endsWith( '.salesforce.com' ) || url.startsWith( '.salesforce.com/' ) ) ) {
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