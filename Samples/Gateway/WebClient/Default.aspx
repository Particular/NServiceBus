<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="Scripts/jquery-1.7.1.min.js" type="text/javascript"></script>
    <script src="Scripts/md5.js" type="text/javascript"></script>
    <script src="Scripts/Math.uuid.js" type="text/javascript"></script>
</head>
<body>
    <script type="text/javascript">
        $(document).ready(function () {
            $('#go').click(function () {

                var message = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/Headquarter.Messages\"><UpdatePrice></UpdatePrice></Messages>";
                var md5 = b64_md5(message) + "==";
                var clientId = Math.uuid() + "\\123456";

                $.ajax({
                    url: $('#gatewayaddress').val(),
                    data: {
                        Message: message,
                        "NServiceBus.CallType": "Submit",
                        "Content-MD5": md5,
                        "NServiceBus.Id": clientId,
                        "NServiceBus.TimeToBeReceived": "00:10:00"

                    },
                    dataType: 'jsonp',
                    success: function (data) {
                        if (data.status != "OK") {
                            alert("Failed to submit the request to the gateway");
                            return;
                        }
                        $.ajax({
                            url: $('#gatewayaddress').val(),
                            dataType: 'jsonp',
                            data: {
                                "NServiceBus.CallType": "Ack",
                                "Content-MD5": md5,
                                "NServiceBus.Id": clientId
                            },
                            success: function () {
                                alert("Success - Check the output of the headquarter server process");
                            },
                            error: function (http, status) {
                                alert("Failed ack: " + status);
                            }
                        }); //ajax
                    },
                    error: function (http, status) {
                        alert("Failed submit: " + status);
                    }
                }); //ajax

            });
        });
	</script>
    <h1>Click the button below to make a JSONP request to the nservicebus gateway</h1>
    <p>Gateway address:</p>
    <input type="text" id="gatewayaddress" value="http://localhost/NServiceBus/Gateways/headquarter/" size="60"/><br/>
    <input type="button" id="go" name="go" value="Send price update command to server" />
</body>
</html>
