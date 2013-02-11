function VideoCtrl($scope, $http) {
    $scope.debug = false;
    $scope.errorMessage = null;

    $scope.videos = [
      { id: 'intro1', title: 'Introduction to NServiceBus - Part I', description: 'In this 2-hour presentation, Udi Dahan covers the architectural ramifications of using a service bus, and how the Bus pattern differs from RPC, as well as how to use the basic features of NServiceBus: one-way messaging, request/reply, publish/subscribe, and configuring NServiceBus.', selected: false },
      { id: 'intro2', title: 'Introduction to NServiceBus - Part II', description: 'Continuation of Introduction to NServiceBus - Part I', selected: false },
      { id: 'gems', title: 'Hidden NServiceBus Gems', description: 'Although NServiceBus has been around for a while, many developers are only familiar with the top-level public API. Join Udi Dahan for a look into some of the lesser known capabilities of NServiceBus that just might save you from having to reinvent the wheel.', selected: false },
      { id: 'integ', title: 'Reliable Integration with NServiceBus', description: "Developers are dealing with more integrations today than ever before, and handling consistency and reliability across those connections isn't getting any easier either. Many developers are already using NServiceBus in the core parts of their systems for the reliability it brings but aren't aware that it can also help with integration as well. Come see how the saga capabilities in NServiceBus make integration code simpler, more robust, and testable.", selected: false },
      { id: 'shiny', title: 'New and shiny things in NServiceBus 3.0', description: "Andreas Öhlund, one of the lead developers for NServiceBus, discusses the new and shiny things in NServiceBus 3.0. Come and get and overview of the new features in the upcoming NServiceBus 3.0 release.", selected: false },
      { id: 'day', title: 'NServiceBus on DNR TV', description: "Just another session coding with NServiceBus.", selected: false }];

    $scope.orders = [];
    
    $scope.ordersReceived = [];

    var connection = $.connection('/ordersShipped');

    connection.start();
    
    connection.received(function (data) {

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
    });
    
    $scope.cancelOrder = function (number) {
        $scope.errorMessage = null;
        
        var idx = retrieveOrderIndex($scope, number);
        if (idx >= 0) {
            $scope.orders[idx].status = 'Cancelling';
        }

        $http.post('/CancelOrder', { orderNumber: number }, { timeout: 15000, headers: { 'Debug': $scope.debug } })
            .success(function (data, status) {
                var idx = retrieveOrderIndex($scope, data.OrderNumber);
                if (idx >= 0) {
                    scope.orders[idx].status = 'Cancelled';
                }
            })
            .error(function (data, status) {
                $scope.errorMessage = "Failed to cancel order!";
            });
    };
    
    $scope.placeOrder = function () {

        $scope.errorMessage = null;

        var selectedVideos = [];
        var selectedVideoTitles = [];
        angular.forEach($scope.videos, function (video) {
            if (video.selected) {
                selectedVideos.push(video.id);
                selectedVideoTitles.push(video.title);
            }
        });

        if (selectedVideos.length === 0) {
            return;
        }
        
        $http.post('/PlaceOrder', { videoIds: selectedVideos }, { timeout: 15000, headers: { 'Debug': $scope.debug } })
            .success(function (data, status) {
                $scope.orders.push({ number: data.OrderNumber, titles: selectedVideoTitles, status: 'Pending' });

                angular.forEach($scope.videos, function (video) {
                    video.selected = false;
                });

                $('#userWarning')
                    .css({ opacity: 0 })
                    .animate({ opacity: 1 }, 700);
            })
            .error(function (data, status) {
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
