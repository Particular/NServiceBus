function VideoCtrl($scope) {
    $scope.debug = false;
    $scope.errorMessage = null;

    $scope.videos = [
      { id: 'intro1', title: 'Introduction to NServiceBus - Part I', description: 'In this 2-hour presentation, Udi Dahan covers the architectural ramifications of using a service bus, and how the Bus pattern differs from RPC, as well as how to use the basic features of NServiceBus: one-way messaging, request/reply, publish/subscribe, and configuring NServiceBus.', selected: false },
      { id: 'intro2', title: 'Introduction to NServiceBus - Part II', description: 'Continuation of Introduction to NServiceBus - Part I', selected: false },
      { id: 'gems', title: 'Hidden NServiceBus Gems', description: 'Although NServiceBus has been around for a while, many developers are only familiar with the top-level public API. Join Udi Dahan for a look into some of the lesser known capabilities of NServiceBus that just might save you from having to reinvent the wheel.', selected: false },
      { id: 'integ', title: 'Reliable Integration with NServiceBus', description: "Developers are dealing with more integrations today than ever before, and handling consistency and reliability across those connections isn't getting any easier either. Many developers are already using NServiceBus in the core parts of their systems for the reliability it brings but aren't aware that it can also help with integration as well. Come see how the saga capabilities in NServiceBus make integration code simpler, more robust, and testable.", selected: false },
      { id: 'shiny', title: 'New and shiny things in NServiceBus 3.0', description: "Andreas Öhlund, one of the lead developers for NServiceBus, discusses the new and shiny things in NServiceBus 3.0. Come and get and overview of the new features in the upcoming NServiceBus 3.0 release.", selected: false },
      { id: 'day', title: 'NServiceBus on DNR TV', description: "Just another session coding with NServiceBus.", selected: false },
      { id: 'need', title: 'Who needs a service bus anyway?', description: "Although Enterprise Service Buses have been used in many larger companies, small and medium enterprises have often been put off by the high cost of these large middleware packages. These days we're seeing more open-source service buses gaining popularity and many developers are beginning to get curious - what would I use it for? Join Udi to get the scoop as well as see some patterns in action with NServiceBus", selected: false }
    ];

    $scope.orders = [];
    
    $scope.ordersReceived = [];

    var ordersHub = $.connection.ordersHub;

    ordersHub.client.orderReceived = function (data) {
        var selectedVideoTitles = [];

        for (var i = 0; i < data.VideoIds.length; i++) {
            var id = data.VideoIds[i];
            for (var j = 0; j < $scope.videos.length; j++) {
                if ($scope.videos[j].id === id) {
                    selectedVideoTitles.push($scope.videos[j].title);
                    break;
                }
            }
        }
        
        $scope.$apply(function(scope) {
            scope.orders.push({ number: data.OrderNumber, titles: selectedVideoTitles, status: 'Pending' });
        });
        
        $('#userWarning')
            .css({ opacity: 0 })
            .animate({ opacity: 1 }, 700);
    };
    
    ordersHub.client.orderCancelled = function (data) {
        $scope.$apply(function(scope) {
            var idx = retrieveOrderIndex(scope, data.OrderNumber);
            if (idx >= 0) {
                scope.orders[idx].status = 'Cancelled';
            }
        });
    };
    
    ordersHub.client.orderReady = function (data) {
        var items = [];
        
        for (var i = 0; i < data.VideoUrls.length; i++) {
            var item = data.VideoUrls[i];

            for (var j = 0; j < $scope.videos.length; j++) {

                if ($scope.videos[j].id === item.Id) {
                    items.push({ url: item.Url, title: $scope.videos[j].title });
                    break;
                }
            }
        }
        
        $scope.$apply(function (scope) {
            var idx = retrieveOrderIndex(scope, data.OrderNumber);
            if (idx >= 0) {
                scope.orders[idx].status = 'Complete';
            }
            scope.ordersReceived.push({ number: data.OrderNumber, items: items });
        });
    };
    
    $.connection.hub.start();
    
    $scope.cancelOrder = function (number) {
        $scope.errorMessage = null;
        
        var idx = retrieveOrderIndex($scope, number);
        if (idx >= 0) {
            $scope.orders[idx].status = 'Cancelling';
        }

        ordersHub.state.debug = $scope.debug;
        ordersHub.server.cancelOrder(number)
            .fail(function () {
                $scope.errorMessage = "We couldn't cancel you order, ensure all endpoints are running and try again!";
            });
    };
    
    $scope.placeOrder = function () {

        $scope.errorMessage = null;

        var selectedVideos = [];
        angular.forEach($scope.videos, function (video) {
            if (video.selected) {
                selectedVideos.push(video.id);
            }
        });

        if (selectedVideos.length === 0) {
            return;
        }
        
        ordersHub.state.debug = $scope.debug;
        ordersHub.server.placeOrder(selectedVideos)
            .done(function () {
                angular.forEach($scope.videos, function (video) {
                    video.selected = false;
                });
            })
            .fail(function() {
                $scope.errorMessage = "We couldn't place you order, ensure all endpoints are running and try again!";
            });
    };
    
    function retrieveOrderIndex(scope, orderNumber) {
        var idx = 0;
        
        for (; idx < scope.orders.length; idx++) {
            if (scope.orders[idx].number === orderNumber) {
                return idx;
            }
        }

        return -1;
    }
}
