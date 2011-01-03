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
            var params = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/NServiceBus.Unicast.Transport\"><CompletionMessage><ErrorCode>0</ErrorCode></CompletionMessage></Messages>";
            var md5 = b64_md5(params) + "==";
            var clientId = Math.uuid();

            $.ajax({
                url: 'http://localhost:8090/Gateway/',
                beforeSend: function (http, settings) {
                    http.setRequestHeader("Content-MD5", md5);
                    http.setRequestHeader("NServiceBus.CallType", "Submit");
                    http.setRequestHeader("NServiceBus.Id", clientId);
                },
                contentType: 'text/xml',
                data: params,
                processData: false,
                type: 'POST',
                success: function (data) {
                    $.ajax({
                        url: 'http://localhost:8090/Gateway/',
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

//            var http = new XMLHttpRequest();

//            http.open("POST", "http://localhost:8090/Gateway/");

//            http.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
//            http.setRequestHeader("Content-length", params.length);
//            http.setRequestHeader("Content-MD5", md5);
//            http.setRequestHeader("NServiceBus.CallType", "Submit");
//            http.setRequestHeader("NServiceBus.Id", clientId);
//            http.setRequestHeader("Connection", "Keep-Alive");

//            http.onreadystatechange = function () {//Call a function when the state changes.
//                if (http.readyState == 4) {
//                    if (http.status == 200) {

//                        http.open("POST", "http://localhost:8090/Gateway/");

//                        http.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
//                        http.setRequestHeader("Content-length", 0);
//                        http.setRequestHeader("Content-MD5", md5);
//                        http.setRequestHeader("NServiceBus.CallType", "Ack");
//                        http.setRequestHeader("NServiceBus.Id", clientId);
//                        http.setRequestHeader("Connection", "close");

//                        http.onreadystatechange = function () {//Call a function when the state changes.
//                            if (http.readyState == 4) {
//                                if (http.status == 200) {
//                                    alert("ack successful");
//                                }
//                                else
//                                    alert(http.status);

//                            }
//                        }

//                        http.send(params);
//                    }
//                }
//            }

//            http.send(params);

        });
    });
	</script>

    <input type="button" id="go" name="go" value="Submit" />
    
</body>
</html>
