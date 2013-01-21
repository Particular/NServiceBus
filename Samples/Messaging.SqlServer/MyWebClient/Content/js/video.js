function VideoCtrl($scope, $http) {
    $scope.debug = false;
    $scope.errors = false;
    
    $scope.videos = [
      { id: 'intro1', title: 'Introduction to NServiceBus - Part I', description: 'In this 2-hour presentation, Udi Dahan covers the architectural ramifications of using a service bus, and how the Bus pattern differs from RPC, as well as how to use the basic features of NServiceBus: one-way messaging, request/reply, publish/subscribe, and configuring NServiceBus.', selected: false },
      { id: 'intro2', title: 'Introduction to NServiceBus - Part II', description: 'Continuation of Introduction to NServiceBus - Part I', selected: false }];

    $scope.orders = [];
   
    $scope.nothingSelected = function() {
        /*
        $.each($scope.videos, function (index, value) {
            if (value.selected) {
                return false;
            }
        });
        */
        return false;
    };
    
    $scope.placeOrder = function () {
        
        $scope.errors = false;
        
        var selectedVideos = [];
        var selectedVideoTitles = [];
        angular.forEach($scope.videos, function (video) {
            if (video.selected) {
                selectedVideos.push(video.id);
                selectedVideoTitles.push(video.title);
            }
        });

        $http.post('/PlaceOrder', selectedVideos, { headers: { 'Debug': $scope.debug } })
            .success(function (data, status) {
                $scope.orders.push({ number: data.OrderNumber, title: selectedVideoTitles.join(", "), status: 'Pending' });

                angular.forEach($scope.videos, function(video) {
                    video.selected = false;
                });
            })
            .error(function (data, status) {
                $scope.errors = true;
                $scope.errorMessage = data;
            });
    };
}
