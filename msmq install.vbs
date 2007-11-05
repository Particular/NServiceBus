set qinfo = CreateObject("MSMQ.MSMQQueueInfo")
qinfo.PathName = ".\private$\client"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\error"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\messagebus"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\subscriptions"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\distributorStorage"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\distributorcontrolbus"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\distributordatabus"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\worker"
qInfo.Delete
qinfo.Create true
qinfo.PathName = ".\private$\worker2"
qInfo.Delete
qinfo.Create true
