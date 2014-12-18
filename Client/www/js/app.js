// Ionic Starter App

// angular.module is a global place for creating, registering and retrieving Angular modules
// 'starter' is the name of this angular module example (also set in a <body> attribute in index.html)
// the 2nd parameter is an array of 'requires'
// 'starter.controllers' is found in controllers.js
angular.module('sinfApp', ['ionic', 'angularMoment', 'sinfApp.controllers', 'restangular', 'ngCookies'])

    // singleton, this service can be injected into any route in order to check the current user session information
    .provider('AuthService', function AuthServiceProvider() {

        var currentUser;

        function token() {
            if (currentUser && currentUser.access_token)
                return currentUser.access_token;
            else
                return null;
        }

        this.token = token;

        function CurrentUser($cookieStore) {

            this.storage = $cookieStore;

            this.login = function (user, access_token) {
                if (user && access_token) {
                    currentUser = user;
                    currentUser.access_token = access_token;
                    this.storage.put('user', user);
                    this.storage.put('token', access_token);
                }
            };

            this.logout = function () {
                this.storage.remove('user');
                this.storage.remove('token');
                currentUser = null;
            };

            this.isLoggedIn = function () {
                return currentUser != null;
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

    .run(function ($ionicPlatform) {
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
            return {
                headers: _.extend(headers, {'Authorization': AuthServiceProvider.token()})
            }
        });
    })

    .service('pickingListService', function () {
        var orders = [];
        var facility = '';

        this.set = function(o, f) {
            orders = o;
            facility = f;
        };

        this.get = function() {
            return { Orders: orders, Facility: facility };
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
