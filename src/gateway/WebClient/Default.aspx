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
            
            //var params = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/MyMessages\"><RequestDataMessage><DataId>0685e460-c71b-48c3-a326-d396d0fb94a6</DataId><String>&lt;node&gt;it&apos;s my &quot;node&quot; &amp; i like it a lot&lt;node&gt;</String></RequestDataMessage></Messages>";
            var params = $('#messageToSend').val();
            var md5 = b64_md5(params) + "==";
            var clientId = Math.uuid() + "\\123456";

            $.ajax({
                url: $('#gatewayaddress').val(),
                beforeSend: function (http, settings) {
                //todo add the transmission id!
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
                            alert("Success");
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
    <input type="text" id="gatewayaddress" />

    <p>Enter message to send:</p>
    <textarea id="messageToSend" cols="100"  rows="10" >

    </textarea>
    <input type="button" id="go" name="go" value="Submit" />
    
</body>
</html>
