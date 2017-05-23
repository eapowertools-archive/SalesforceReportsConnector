define([
	"general.services/show-service/show-service",
	"text!introConnector.webroot/salesforcelogindialog.ng.html",
    'css!introConnector.webroot/salesforcelogindialog.css',
	"assets/client/dialogs/directives/dialog-directives"
], function (
	showService,
	template,
    css,
    dialogDirective
) {
    var aboutDialogIsOpened = false;

    /** COMPONENT **/
    var component = {
        template: template,
        scope: {
            url: ""
        },
        controller: ['$scope', '$document', '$window', function ($scope, $document, $window) {
            $scope.sfURL = "";

            $scope.finish = function () {
                // do something here to process the URL
                console.log( $scope.sfURL );


                $scope.destroyComponent();
                aboutDialogIsOpened = false;
                $scope.deferredResult.resolve();
            };

            $scope.cancel = function () {
                $scope.destroyComponent();
                aboutDialogIsOpened = false;
                $scope.deferredResult.reject();
            };

            $window.open($scope.url);
        }]
    };

    template.urlLoaded = function () {
    }

    function getAboutDialogIsOpenedParam() {
        return aboutDialogIsOpened;
    }

    function show($sce, url) {
        aboutDialogIsOpened = true;

        var input = {
            url: $sce.trustAsResourceUrl(url)
        };
        return showService.show(component, input).resultPromise;
    }

    return {
        show: show,
        getAboutDialogIsOpenedParam: getAboutDialogIsOpenedParam
    };

});