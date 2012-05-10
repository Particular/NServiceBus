﻿using System;
using NServiceBus.Config;
using NServiceBus.Unicast.Queuing.Ftp;


namespace NServiceBus
{
    public static class ConfigureFtpQueue
    {
        public static Configure FtpTransport(this Configure config)
        {            
            var ftpQueue = config.Configurer.ConfigureComponent<FtpMessageQueue>(DependencyLifecycle.SingleInstance);
            var cfg = Configure.GetConfigSection<FtpQueueConfig>();
            
            if (cfg != null)
            {
                ftpQueue.ConfigureProperty(t => t.ReceiveDirectory, cfg.ReceiveDirectory);

                if (!String.IsNullOrEmpty(cfg.UserName))
                {
                    ftpQueue.ConfigureProperty(t => t.UserName, cfg.UserName);
                    ftpQueue.ConfigureProperty(t => t.Password, cfg.Password);
                }
            }
            
            return config;
        }
    }
}
