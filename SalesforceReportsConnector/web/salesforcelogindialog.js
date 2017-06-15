define([
	"general.services/show-service/show-service",
	"text!SalesforceReportsConnector.webroot/salesforcelogindialog.ng.html",
    'css!SalesforceReportsConnector.webroot/salesforcelogindialog.css'
], function (
	showService,
	template
) {
    /** COMPONENT **/
    var component = {
        template: template,
        scope: {
            url: ""
        },
        controller: ['$scope', '$document', '$window', function ($scope, $document, $window) {
            var init = function () {
                var urlString = $scope.url.toString();
                var indexOfSlash = urlString.indexOf("/", 8);
                $scope.hostname = urlString.substr(0, indexOfSlash);
            }

            $scope.sfURL = "";
            $scope.hostname = "";
            $scope.URLStatus = "Paste URL above.";
            $scope.statusColour = "red";
            $scope.validURL = false;

            $scope.access_token = "";
            $scope.refresh_token = "";
            $scope.instance_url = "";
            $scope.issued_at = "";
            $scope.id = "";
            $scope.signature = "";
            $scope.scope = "";
            $scope.token_type = "";


            $scope.finish = function () {
                var returnVal = {
                    access_token: $scope.access_token,
                    refresh_token: $scope.refresh_token,
                    instance_url: $scope.instance_url,
                    id: $scope.id
                };
                $scope.destroyComponent();
                $scope.deferredResult.resolve(returnVal);
            };

            $scope.cancel = function () {
                $scope.destroyComponent();
                $scope.deferredResult.reject();
            };

            $scope.openLogin = function () {
                $window.open($scope.url);
            };

            $scope.urlChange = function () {
                var response = validateReturnURL( $scope.sfURL );
                if ( response['isValid'] ) {
                    $scope.validURL = true;
                    $scope.statusColour = "green";
                    $scope.URLStatus = "Valid URL.";
                } else {
                    $scope.validURL = false;
                    $scope.statusColour = "red";
                    $scope.URLStatus = response['message'];
                }
            }

            var validateReturnURL = function (url) {
                $scope.access_token = "";
                $scope.refresh_token = "";
                $scope.instance_url = "";
                $scope.id = "";
                $scope.issued_at = "";
                $scope.signature = "";
                $scope.scope = "";
                $scope.token_type = "";

                var response = {
                    isValid: true,
                    message: ""
                };
                var loginPath = "/services/oauth2/success#";
                if ( !url.startsWith( $scope.hostname + loginPath ) ) {
                    response['isValid'] = false;
                    response['message'] = "URL is not the Salesforce oauth response page (" + $scope.hostname + "/services/oatuh2/success*).";
                }
                var paramString = url.substring(($scope.hostname.length + loginPath.length), url.length);
                var paramArray = paramString.split("&");

                paramArray.forEach(function (value, index, array) {
                    var indexOfEquals = value.indexOf( "=" );
                    var key = value.substr( 0, indexOfEquals );
                    var val = value.substring(indexOfEquals + 1, value.length);
                    switch(key) {
                        case "access_token":
                            $scope.access_token = val;
                            break;
                        case "refresh_token":
                            $scope.refresh_token = val;
                            break;
                        case "instance_url":
                            $scope.instance_url = val;
                            break;
                        case "id":
                            $scope.id = val;
                            break;
                        case "issued_at":
                            $scope.issued_at = val;
                            break;
                        case "signature":
                            $scope.signature = val;
                            break;
                        case "scope":
                            $scope.scope = val;
                            break;
                        case "token_type":
                            $scope.token_type = val;
                            break;
                        default:
                            response['isValid'] = false;
                            response['message'] = "Invalid URL parameter returned: '" + value.substr(0, indexOfEquals) + "'.";
                            break;
                    } 
                });

                if ( $scope.access_token == "" ) {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'access_token'.";
                }
                else if ($scope.refresh_token == "") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'refresh_token'.";
                }
                else if ($scope.instance_url == "") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'instance_url'.";
                }
                else if ($scope.id == "") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'id'.";
                }
                else if ($scope.issued_at == "") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'issued_at'.";
                }
                else if ($scope.signature == "") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'signature'.";
                }
                else if ($scope.scope == "") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'scope'.";
                }
                else if ($scope.token_type == "" || $scope.token_type != "Bearer") {
                    response['isValid'] = false;
                    response['message'] = "Missing URL parameter 'token_type' or 'token_type' is not 'Bearer'.";
                }

                return response;
            };

            init();
        }]
    };

    function show($sce, url) {
        var trustedURL = {
            url: $sce.trustAsResourceUrl(url)
        };
        return showService.show(component, trustedURL).resultPromise;
    }

    return {
        show: show
    };

});