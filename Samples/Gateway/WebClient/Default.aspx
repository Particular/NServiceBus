<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>

    <script src="Scripts/jquery-1.4.1.js" type="text/javascript"></script>
    <script src="Scripts/md5.js" type="text/javascript"></script>
    <script src="Scripts/Math.uuid.js" type="text/javascript"></script>

    

</head>
<body>

<script type="text/javascript">
    $(document).ready(function () {
        $('#go').click(function () {

            var params = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/Headquarter.Messages\"><UpdatePrice></UpdatePrice></Messages>";
            var md5 = b64_md5(params) + "==";
            var clientId = Math.uuid() + "\\123456";

            $.ajax({
                url: $('#gatewayaddress').val(),
                beforeSend: function (http) {
                    http.setRequestHeader("Content-MD5", md5);
                    http.setRequestHeader("NServiceBus.CallType", "Submit");
                    http.setRequestHeader("NServiceBus.Id", clientId);
                    http.setRequestHeader("NServiceBus.TimeToBeReceived", "00:10:00");
                },
                contentType: 'text/xml',
                data: params,
                processData: false,
                type: 'POST',
                success: function (data) {
                    if (data != "OK") {
                        alert("Failed to submit the request to the gateway");
                        return;
                    }

                    $.ajax({
                        url: $('#gatewayaddress').val(),
                        beforeSend: function (http, settings) {
                            http.setRequestHeader("Content-MD5", md5);
                            http.setRequestHeader("NServiceBus.CallType", "Ack");
                            http.setRequestHeader("NServiceBus.Id", clientId);
                        },
                        contentType: 'text/xml',
                        data: '',
                        processData: false,
                        type: 'POST',
                        success: function (data) {
                            alert("Success - Check the output of the headquarter server process");
                        },
                        error: function (http, status, error) {
                            alert("Failed ack: " + status);
                        }
                    }); //ajax
                },
                error: function (http, status, error) {
                    alert("Failed submit: " + status);
                }
            }); //ajax

        });
    });
	</script>

    <h1>Test on IE as involves Cross Origin Resource Sharing</h1>

    <p>Enter Gateway address:</p>
    <input type="text" id="gatewayaddress" value="http://localhost/NServiceBus/Gateways/headquarter/"/>

    <input type="button" id="go" name="go" value="Send price update command to server" />
    
</body>
</html>
