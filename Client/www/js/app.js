// Ionic Starter App

// angular.module is a global place for creating, registering and retrieving Angular modules
// 'starter' is the name of this angular module example (also set in a <body> attribute in index.html)
// the 2nd parameter is an array of 'requires'
// 'starter.controllers' is found in controllers.js
angular.module('sinfApp', ['ionic', 'angularMoment', 'sinfApp.controllers'])

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

    .config(function ($stateProvider, $urlRouterProvider) {
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
                url: '/picking/:pickingId',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/pickingResult.html',
                        controller: 'PickingResultCtrl'
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
                url: '/putaway/:putawayId',
                views: {
                    'menuContent': {
                        templateUrl: 'templates/putawayResult.html',
                        controller: 'PutawayResultCtrl'
                    }
                }
            });

        // if none of the above states are matched, use this as the fallback
        $urlRouterProvider.otherwise('/app/home');
    });
