angular.module('sinfApp.controllers', [])

    .controller('AppCtrl', function ($scope, $ionicModal, $timeout) {
        // Form data for the login modal
        $scope.loginData = {};

        // Create the login modal that we will use later
        $ionicModal.fromTemplateUrl('templates/login.html', {
            scope: $scope
        }).then(function (modal) {
            $scope.modal = modal;
        });

        // Triggered in the login modal to close it
        $scope.closeLogin = function () {
            $scope.modal.hide();
        };

        // Open the login modal
        $scope.login = function () {
            $scope.modal.show();
        };

        // Perform the login action when the user submits the login form
        $scope.doLogin = function () {
            console.log('Doing login', $scope.loginData);

            // Simulate a login delay. Remove this and replace with your login
            // code if using a login system
            $timeout(function () {
                $scope.closeLogin();
            }, 1000);
        };
    })

    .controller('HomeCtrl', function ($scope) {

    })

    .controller('PickingCtrl', function ($scope) {

    })

    .controller('PutawayCtrl', function ($scope, $ionicPopup) {
        $scope.automatic = false;

        $scope.orders = [
            { checked: false, Id: 'Order 1', Entity: 'EMP1', Date: '2014/12/04T14:50', Processed: 0 },
            { checked: true, Id: 'Order 1', Entity: 'EMP2', Date: '2014/12/04T14:50', Processed: 50 },
            { checked: false, Id: 'Order 2', Entity: 'EMP3', Date: '2014/12/04T14:50', Processed: 80 }
        ];

        $scope.automaticChange = function () {
            console.log('automaticChange: ' + $scope.automatic);
            for (var i = 0; i < $scope.orders.length; ++i) {
                $scope.orders[i].checked = $scope.automatic;
            }
        };

        $scope.showHelp = function() {
            $ionicPopup.alert({
                title: 'Help Text',
                template: '<p><strong>Automatic</strong> mode: orders are selected automatically<br><strong>Manual</strong> mode: select orders to putaway</p>'
            });
        };
    });
