using Azure.Messaging.ServiceBus;
using CS.Services.Mail.Interfaces;
using CS.Services.Mail.Models;
using CS.Services.ServiceBusProcessor.Interfaces;
using CS.Services.ServiceBusProcessor.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS.Services.ServiceBusProcessor
{
    public class CSServiceBusProcessor : ICSServiceBusProcessor
    {
        private readonly ServiceBusClient serviceBusClient;
        private Azure.Messaging.ServiceBus.ServiceBusProcessor serviceBusProcessor;

        private readonly IMailService mailService;
        private readonly ServiceBusSettings serviceBusSettings;
        private readonly ILogger logger;

        public CSServiceBusProcessor(
            IMailService mailService,
            ServiceBusSettings serviceBusSettings,
            ILogger<ServiceBusReceiver> logger)
        {
            this.serviceBusSettings = serviceBusSettings;
            this.mailService = mailService;
            this.logger = logger;
            serviceBusClient = new ServiceBusClient(serviceBusSettings.ConnectionString);
        }

        public async Task ProcessMessageAsync()
        {
            var processorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 10,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            };

            serviceBusProcessor =  serviceBusClient.CreateProcessor(serviceBusSettings.QueueName, processorOptions);


            // Register handlers to process messages and errors
            serviceBusProcessor.ProcessMessageAsync += MessageHandlerAsync;
            serviceBusProcessor.ProcessErrorAsync += ErrorHandlerAsync;

            await serviceBusProcessor.StartProcessingAsync().ConfigureAwait(false);
        }
        public async ValueTask DisposeAsync()
        {
            if (serviceBusProcessor != null)
            {
                await serviceBusProcessor.StopProcessingAsync().ConfigureAwait(false);

                await serviceBusProcessor.DisposeAsync().ConfigureAwait(false);
            }

            if (serviceBusClient != null)
            {
                await serviceBusClient.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task MessageHandlerAsync(ProcessMessageEventArgs args)
        {
            var appointment = args.Message.Body.ToObjectFromJson<Appointment>();

            var mail = new MailData();

            mail.To = new List<string> { appointment.PatientEmail };
            mail.Subject = $"Appointment reminder for {appointment.AppointmentStart:g}";
            mail.IsHtml = true;

            StringBuilder sb = new StringBuilder();
            sb.Append($"Dear Mr./Mrs. {appointment.PatientFirstName} {appointment.PatientLastName}<br>");
            sb.Append($"This is a reminder that you have an appointment scheduled for ");
            sb.Append($"{appointment.AppointmentStart.ToString("dd.MM.yyy")} at {appointment.AppointmentStart.ToString("H:mm")}<br>");
            sb.Append("We look forward to seeing you.<br>");
            sb.Append("Best regards.");

            mail.Body = sb.ToString();

            await mailService.SendAsync(mail).ConfigureAwait(false);

            await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);

        }

        private Task ErrorHandlerAsync(ProcessErrorEventArgs arg)
        {
            logger.LogError(arg.Exception, "Message handler encountered an exception");
            // Or use you own Logging service 
            return Task.CompletedTask;
        }
    }
}
