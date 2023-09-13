using Azure.Messaging.ServiceBus;
using CS.Services.ServiceBusProcessor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS.Services.ServiceBusProcessor.Extensions
{
    public static class CSServiceBusProcessorExtensions
    {
        public static IServiceCollection AddCSServiceBusProcessor(this IServiceCollection services, Action<ServiceBusSettings> configureSettings)
        {
            return services.AddSingleton<ICSServiceBusProcessor>(serviceProvider =>
            {
                var settings = new ServiceBusSettings();
                configureSettings(settings);
                return ActivatorUtilities.CreateInstance<CSServiceBusProcessor>(serviceProvider, settings);
            });
        }
    }
}
