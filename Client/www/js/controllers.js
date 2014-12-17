angular.module('sinfApp.controllers', [])

    .controller('AppCtrl', function ($scope, Restangular, AuthService, AlertPopupService, $ionicModal) {
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
            if (!AuthService.isLoggedIn()) $scope.modal.show();
        };

        // Perform the login action when the user submits the login form
        $scope.doLogin = function () {
            console.log('Doing login', $scope.loginData);

            Restangular.all('login').post($scope.loginData).then(function (data) {
                AuthService.login($scope.loginData.username, data);
                $scope.closeLogin();
            }, function (response) {
                AlertPopupService.createPopup("Error", response.data.error);
            });
        };
    })

    .controller('HomeCtrl', function ($scope, Restangular, $ionicPopup) {

        function execute(action) {
            Restangular.one('debug').get({ action: action }).then(function (data) {
                $ionicPopup.alert({
                    template: JSON.stringify(data)
                });
            });
        }

        $scope.setPicked0 =  function() { execute('reset_picked'); };
        $scope.setPicked1 =  function() { execute('set_picked'); };
        $scope.setPickedq0 = function() { execute('reset_pickedq'); };
        $scope.setPickedq1 = function() { execute('set_pickedq'); };

        $scope.reg = { user: '', pass: '' };
        $scope.register = function () {
            Restangular.all('register').post({ username: $scope.reg.user, password: $scope.reg.pass }).then(function (data) {
                $ionicPopup.alert({
                    template: JSON.stringify(data)
                });
            });
        }
    })

    .controller('PickingListsCtrl', function ($scope, Restangular, $ionicPopup, $ionicLoading) {
        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('pickinglists').getList().then(function (data) {
                $scope.pickingLists = data;
                $ionicLoading.hide();
            }, function (err) {
                $ionicLoading.hide();
                $ionicPopup.alert({
                    title: 'Error',
                    template: '<p>An error ocurred: ' + JSON.stringify(err) + '</p>'
                });
            });
        };
    })

    .controller('PickingListCtrl', function ($scope, $stateParams, Restangular, $ionicPopup, $ionicLoading) {
        $scope.id = $stateParams.id;

        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.one('pickinglists', $stateParams.id).get().then(function (data) {
                $scope.pickingList = data;
                $ionicLoading.hide();
            }, function (err) {
                $ionicLoading.hide();
                $ionicPopup.alert({
                    title: 'Error',
                    template: '<p>An error ocurred: ' + JSON.stringify(err) + '</p>'
                });
            });
        };
    })

    .controller('PickingCtrl', function ($scope, $state, $ionicPopup, Restangular, pickingListService, $ionicLoading) {
        $scope.orders = [];
        $scope.warehouses = [];
        $scope.warehouse = {
            name: ''
        };

        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('storagefacilities').getList().then(function (data) {
                $scope.warehouses = data;
                if ($scope.warehouses.length > 0) {
                    $scope.warehouse.name = $scope.warehouses[0];
                }

                $ionicLoading.hide();
            }, function () {
                $ionicLoading.hide();
            });

            Restangular.all('orders').getList().then(function (data) {
                $scope.orders = data;


                _.each($scope.orders, function(order) {
                    var numProcessed = _.filter(order.OrderLines, function(orderline) {
                        return orderline.Picked;
                    }).length;

                    order.Processed = Math.round(numProcessed / order.OrderLines.length) * 100;
                });
            });
        };

        $scope.automaticChange = function (val) {
            for (var i = 0; i < $scope.orders.length; ++i) {
                $scope.orders[i].checked = val;
            }
        };

        $scope.search = { Id: '', Entity: '', $:''};
        $scope.filterType = 'Id';

        $scope.anySelectedOrder = function () {
            return _.any($scope.orders, function (order) {
                return order.checked;
            });
        };

        $scope.showHelp = function() {
            $ionicPopup.alert({
                title: 'Help Text',
                template: '<p><strong>Automatic</strong> mode: orders are selected automatically<br><strong>Manual</strong> mode: select orders to pick</p>'
            });
        };

        $scope.pickSelected = function() {
            var checkedOrders = _.filter($scope.orders, function (order) {
                return order.checked;
            });

            var checkedOrdersIds = _.pluck(checkedOrders, 'NumDoc');

            pickingListService.set(checkedOrdersIds, $scope.warehouse.name);
            $state.go('app.pickingResult');
        };

        $scope.toggleOrder = function(Order) {
            if ($scope.isOrderShown(Order)) {
                $scope.shownOrder = null;
            } else {
                $scope.shownOrder = Order;
            }
        };

        $scope.isOrderShown = function(Order) {
            return $scope.shownOrder === Order;
        };
    })

    .controller('PickingResultCtrl', function ($scope, $state, $stateParams, pickingListService, Restangular, $ionicLoading, $ionicPopup) {
        $scope.title = /*$stateParams.pickingId*/ 'Picking List';

        $scope.items = [];
        $scope.skippedOrders = [];

        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('pickinglists').post(pickingListService.get()).then(function (data) {
                $ionicLoading.hide();
                if (data.Items.length > 0) {

                    $scope.items = data.Items;

                    for (var i = 0; i < $scope.items.length; ++i) {
                        $scope.items[i].disabled = i != 0;
                        $scope.items[i].PickedQuantity = $scope.items[i].Quantity + " " + $scope.items[i].Unit;
                    }
                } else {
                    $ionicPopup.alert({
                        title: 'Empty Picking List',
                        template: '<p>Generated picking list is empty. This might happen because there is not enough stock in ' + pickingListService.get().Facility + '</p>'
                    }).then(function () {
                        $state.go('app.picking');
                    });
                }

                if (data.SkippedOrders.length > 0) {
                    $scope.skippedOrders = data.SkippedOrders;
                }
             });
        };

        var clamp = function(num, min, max) {
            return num < min ? min : (num > max ? max : num);
        };

        $scope.inputPickedQuantityBlur = function (item) {
            item.PickedQuantity = clamp(parseFloat(item.PickedQuantity), 0, item.Quantity) + " " + item.Unit;
            if (isNaN(parseFloat(item.PickedQuantity)))
                item.PickedQuantity = 0 + " " + item.Unit;
        };

        $scope.inputPickedQuantityFocus = function (item) {
            item.PickedQuantity = parseFloat(item.PickedQuantity);
        };

        $scope.enableFinish = true;

        $scope.finished = function () {
            $ionicLoading.show({
                template: 'Loading...'
            });

            for (var i = 0; i < $scope.items.length; ++i) {
                $scope.items[i].PickedQuantity = parseFloat($scope.items[i].PickedQuantity);
            }

            Restangular.all('pickinglists').patch({ Items: $scope.items, SkippedOrders: $scope.skippedOrders}).then(function () {
                $ionicLoading.hide();
                $state.go('app.picking');
            }, function (err) {
                $ionicPopup.alert({
                    title: 'Help Text',
                    template: '<p>Error occurred: ' + JSON.stringify(err) + '</p>'
                });
            });
        };
    })

    .controller('PutawayListsCtrl', function ($scope, Restangular, $ionicPopup, $ionicLoading) {
        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('putawaylists').getList().then(function (data) {
                $scope.putawayLists = data;
                $ionicLoading.hide();
            }, function (err) {
                $ionicLoading.hide();
                $ionicPopup.alert({
                    title: 'Error',
                    template: '<p>An error ocurred: ' + JSON.stringify(err) + '</p>'
                });
            });
        };
    })

    .controller('PutawayListCtrl', function ($scope, $stateParams, Restangular, $ionicPopup, $ionicLoading) {
        $scope.id = $stateParams.id;

        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.one('putawaylists', $stateParams.id).get().then(function (data) {
                $scope.putawayList = data;
                $ionicLoading.hide();
            }, function (err) {
                $ionicLoading.hide();
                $ionicPopup.alert({
                    title: 'Error',
                    template: '<p>An error ocurred: ' + JSON.stringify(err) + '</p>'
                });
            });
        };
    })

    .controller('PutawayCtrl', function ($scope, $state, $ionicPopup, Restangular, putawayListService, $ionicLoading) {
        $scope.supplies = [];
        $scope.warehouses = [];
        $scope.warehouse = {
            name: ''
        };

        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('storagefacilities').getList().then(function (data) {
                $scope.warehouses = data;
                if ($scope.warehouses.length > 0) {
                    $scope.warehouse.name = $scope.warehouses[0];
                }

                $ionicLoading.hide();
            }, function () {
                $ionicLoading.hide();
            });

            Restangular.all('supplies').getList().then(function (data) {
                $scope.supplies = data;


                _.each($scope.supplies, function(supply) {
                    var numProcessed = _.filter(supply.SupplyLines, function(supplyline) {
                        return supplyline.Picked;
                    }).length;

                    supply.Processed = Math.round(numProcessed / supply.SupplyLines.length) * 100;
                });
            });
        };

        $scope.automaticChange = function (val) {
            for (var i = 0; i < $scope.supplies.length; ++i) {
                $scope.supplies[i].checked = val;
            }
        };

        $scope.search = { Id: '', Entity: '', $:''};
        $scope.filterType = 'Id';

        $scope.anySelectedSupply = function () {
            return _.any($scope.supplies, function (supply) {
                return supply.checked;
            });
        };

        $scope.showHelp = function() {
            $ionicPopup.alert({
                title: 'Help Text',
                template: '<p><strong>Automatic</strong> mode: supplies are selected automatically<br><strong>Manual</strong> mode: select supplies to putaway</p>'
            });
        };

        $scope.putawaySelected = function() {
            var checkedSupplies = _.filter($scope.supplies, function (supply) {
                return supply.checked;
            });

            var checkedSuppliesIds = _.pluck(checkedSupplies, 'NumDoc');

            putawayListService.set(checkedSuppliesIds, $scope.warehouse.name);
            $state.go('app.putawayResult');
        };

        $scope.toggleSupply = function(Supply) {
            if ($scope.isSupplyShown(Supply)) {
                $scope.shownSupply = null;
            } else {
                $scope.shownSupply = Supply;
            }
        };

        $scope.isSupplyShown = function(Supply) {
            return $scope.shownSupply === Supply;
        };
    })

    .controller('PutawayResultCtrl', function ($scope, $stateParams) {
        $scope.title = $stateParams.putawayId;

        $scope.items = [
            { checked: false, disabled: true, ItemId: 'IT1', Quantity: 11.2, Unit: 'kg',  StorageFacility: 'A1', StorageLocation: 'A1S1.1' },
            { checked: false, disabled: true, ItemId: 'IT2', Quantity: 2,    Unit: 'SKU', StorageFacility: 'A1', StorageLocation: 'A1S4.1' },
            { checked: false, disabled: true, ItemId: 'IT3', Quantity: 13,   Unit: 'm',   StorageFacility: 'A1', StorageLocation: 'A1S2.3' },
            { checked: false, disabled: true, ItemId: 'IT4', Quantity: 10,   Unit: 'SKU', StorageFacility: 'A1', StorageLocation: 'A1S2.2' },
            { checked: false, disabled: true, ItemId: 'IT5', Quantity: 115,  Unit: 'g',   StorageFacility: 'A1', StorageLocation: 'A1S2.1' }
        ];

        $scope.items[0].disabled = false;
        $scope.enableFinish = false;

        $scope.itemChecked = function (id ) {
            $scope.items[id].disabled = true;

            if (id + 1 < $scope.items.length) {
                $scope.items[id + 1].disabled = false;
            }

            $scope.enableFinish = true;
            for (var i = 0; i < $scope.items.length; ++i) {
                if (!$scope.items[i].checked) {
                    $scope.enableFinish = false;
                    break;
                }
            }
        };
    });
