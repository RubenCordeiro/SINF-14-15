angular.module('sinfApp', ['ionic', 'angularMoment', 'sinfApp.controllers', 'restangular', 'ngCookies'])

    // singleton, this service can be injected into any route in order to check the current user session information
    .provider('AuthService', function AuthServiceProvider() {

        var currentUser = {
            username: '',
            access_token: ''
        };

        function token() {
            if (currentUser.username && currentUser.access_token)
                return currentUser.access_token;
            else
                return null;
        }

        this.token = token;

        function CurrentUser($cookieStore) {

            this.storage = $cookieStore;

            this.login = function (username, access_token) {
                if (username && access_token) {
                    currentUser.username = username;
                    currentUser.access_token = access_token;
                    this.storage.put('user', username);
                    this.storage.put('token', access_token);
                }
            };

            this.logout = function () {
                this.storage.remove('user');
                this.storage.remove('token');
                currentUser.username = '';
                currentUser.access_token = '';
            };

            this.isLoggedIn = function () {
                return currentUser.access_token;
            };

            this.currentUser = function () {
                return currentUser;
            };

            this.token = token;
        }

        this.$get = ['$cookieStore', function ($cookieStore) {
            return new CurrentUser($cookieStore);
        }];
    })

    .service('AlertPopupService', ['$ionicPopup', function ($ionicPopup) {

        this.createPopup = function (headerMessage, bodyMessage, okAction) {
            $ionicPopup.alert({
                title: headerMessage,
                content: bodyMessage
            }).then(function (res) {
                if (okAction)
                    okAction();
            });
        }
    }])

    .run(function ($ionicPlatform, $state, Restangular) {
        $ionicPlatform.ready(function () {
            // Hide the accessory bar by default (remove this to show the accessory bar above the keyboard
            // for form inputs)
            if (window.cordova && window.cordova.plugins.Keyboard) {
                cordova.plugins.Keyboard.hideKeyboardAccessoryBar(true);
            }
            if (window.StatusBar) {
                // org.apache.cordova.statusbar required
                StatusBar.styleDefault();
            }
        });

        Restangular.setErrorInterceptor(function(response, deferred, responseHandler) {
            if(response.status === 401) {
                $state.go('app.login');
                return false; // error handled
            }

            return true; // error not handled
        });
    })

    .config(function ($stateProvider, $urlRouterProvider, $ionicConfigProvider, RestangularProvider, AuthServiceProvider) {
        $ionicConfigProvider.views.maxCache(0);

        $stateProvider

            .state('app', {
                url: '/app',
                abstract: true,
                templateUrl: 'templates/menu.html',
                controller: 'AppCtrl'
            })

            .state('app.login', {
                url: '/login',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/login.html',
                        controller: 'LoginCtrl'
                    }
                }
            })

            .state('app.home', {
                url: '/home',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/home.html',
                        controller: 'HomeCtrl'
                    }
                }
            })

            .state('app.pickingLists', {
                url: '/pickingLists',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/pickingLists.html',
                        controller: 'PickingListsCtrl'
                    }
                }
            })

            .state('app.pickingList', {
                url: '/pickingLists/:id',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/pickingList.html',
                        controller: 'PickingListCtrl'
                    }
                }
            })

            .state('app.picking', {
                url: '/picking',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/picking.html',
                        controller: 'PickingCtrl'
                    }
                }
            })

            .state('app.pickingResult', {
                url: '/pickingResult',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/pickingResult.html',
                        controller: 'PickingResultCtrl'
                    }
                }
            })

            .state('app.putawayLists', {
                url: '/putawayLists',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/putawayLists.html',
                        controller: 'PutawayListsCtrl'
                    }
                }
            })

            .state('app.putawayList', {
                url: '/putawayLists/:id',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/putawayList.html',
                        controller: 'PutawayListCtrl'
                    }
                }
            })

            .state('app.putaway', {
                url: '/putaway',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/putaway.html',
                        controller: 'PutawayCtrl'
                    }
                }
            })

            .state('app.putawayResult', {
                url: '/putawayResult',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/putawayResult.html',
                        controller: 'PutawayResultCtrl'
                    }
                }
            });

        // if none of the above states are matched, use this as the fallback
        $urlRouterProvider.otherwise('/app/home');

        RestangularProvider.setBaseUrl("http://localhost/Picking/api");

        RestangularProvider.addFullRequestInterceptor(function (element, operation, what, url, headers) {
            if (!AuthServiceProvider.token())
                return;

            return {
                headers: _.extend(headers, {'Authorization': 'Basic ' + AuthServiceProvider.token()})
            }
        });
    })

    .service('pickingListService', function () {
        var orders = [];
        var facility = '';
        var capacity = 0.0;

        this.set = function(o, f, c) {
            orders = o;
            facility = f;
            capacity = c;
        };

        this.get = function() {
            return { Orders: orders, Facility: facility, AvailableCapacity: capacity };
        };
    })

    .service('putawayListService', function () {
        var supplies = [];
        var facility = '';

        this.set = function(s, f) {
            supplies = s;
            facility = f;
        };

        this.get = function() {
            return { Supplies: supplies, Facility: facility };
        };
    });
