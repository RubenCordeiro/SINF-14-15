angular.module('sinfApp.controllers', [])

    .controller('AppCtrl', function ($scope, $rootScope, $ionicLoading, Restangular, AuthService, AlertPopupService, $ionicModal, $state) {

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
            $state.go('app.login');
        };

        $scope.isLoggedIn = function () {
            return AuthService.isLoggedIn();
        };

        $scope.logout = function () {
            return AuthService.logout();
        };

		$rootScope.$on('$stateChangeSuccess', function () {
			$ionicLoading.hide();
		});
    })

    .controller('HomeCtrl', function ($scope, Restangular, AuthService, $ionicPopup) {

        $scope.executeDebug = function(action) {
            Restangular.one('debug').get({ action: action }).then(function (data) {
                $ionicPopup.alert({
                    template: JSON.stringify(data)
                });
            });
        };

        $scope.reg = { user: '', pass: '' };
        $scope.register = function () {
            Restangular.all('register').post({ username: $scope.reg.user, password: $scope.reg.pass }).then(function (data) {
                $ionicPopup.alert({
                    template: JSON.stringify(data)
                });
            });
        }

        $scope.isAdmin = function () {
            return AuthService.currentUser().username == 'admin';
        };
    })

    .controller('LoginCtrl', function($scope, Restangular, AuthService, AlertPopupService, $state) {

        $scope.loginData = {};

        $scope.login = function() {
            Restangular.all('login').post($scope.loginData).then(function (data) {
                AuthService.login($scope.loginData.Username, data);
                $state.go('app.home');
            }, function (response) {
                AlertPopupService.createPopup("Error", response.data.error);
            });
        };
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
        $scope.selection = {
            warehouse: '',
            capacity: 5
        };

        $scope.init = function () {

            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('storagefacilities').getList().then(function (data) {
                $scope.warehouses = data;
                if ($scope.warehouses.length > 0) {
                    $scope.selection.warehouse = $scope.warehouses[0];
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

                    order.Processed = Math.round((numProcessed / order.OrderLines.length) * 100);
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

        $scope.pickSelected = function() {
            var checkedOrders = _.filter($scope.orders, function (order) {
                return order.checked;
            });

            var checkedOrdersIds = _.pluck(checkedOrders, 'NumDoc');

            pickingListService.set(checkedOrdersIds, $scope.selection.warehouse, $scope.selection.capacity);
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
			}, function () {
				$ionicLoading.hide();
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
				$ionicLoading.hide();
                $ionicPopup.alert({
                    title: 'Error',
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
                        return supplyline.Putaway;
                    }).length;

                    supply.Processed = Math.round((numProcessed / supply.SupplyLines.length) * 100);
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

    .controller('PutawayResultCtrl', function ($scope, $state, $stateParams, putawayListService, Restangular, $ionicLoading, $ionicPopup) {
        $scope.items = [];
        $scope.skippedSupplies = [];

        $scope.init = function () {
            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('putawaylists').post(putawayListService.get()).then(function (data) {
                if (data.Items.length > 0) {
                    Restangular.all('locations').getList({type: 'full'}).then(function (locsData) {
                        $ionicLoading.hide();
                        $scope.locations = locsData;
                        $scope.locations.push("none");
                        $scope.items = data.Items;
                    });
                } else {
                    $ionicLoading.hide();
                    $ionicPopup.alert({
                        title: 'Empty Putaway List',
                        template: '<p>Generated putaway list is empty. This might happen because all locations are full in ' + putawayListService.get().Facility + '</p>'
                    }).then(function () {
                        $state.go('app.putaway');
                    });
                }

                if (data.SkippedSupplies.length > 0) {
                    $scope.skippedSupplies = data.SkippedSupplies;
                }
            });
        };

        $scope.enableFinish = true;

        $scope.finished = function () {
            $ionicLoading.show({
                template: 'Loading...'
            });

            Restangular.all('putawaylists').patch({ Items: $scope.items, SkippedSupplies: $scope.skippedSupplies}).then(function (data) {
                $ionicLoading.hide();
                $state.go('app.putaway');
            }, function (err) {
                $ionicLoading.hide();
                $ionicPopup.alert({
                    title: 'Error',
                    template: '<p>Error occurred: ' + JSON.stringify(err) + '</p>'
                });
            });
        };
    });
