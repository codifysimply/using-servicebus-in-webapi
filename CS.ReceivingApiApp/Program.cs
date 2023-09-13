using Azure.Messaging.ServiceBus;
using CS.Services.Mail.Extensions;
using CS.Services.ServiceBusProcessor.Extensions;
using CS.Services.ServiceBusProcessor.Interfaces;

namespace CS.ReceivingApiApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddCSServiceBusProcessor(settings =>
            {
                settings.QueueName = builder.Configuration.GetValue<string>("QueueName");
                settings.ConnectionString = builder.Configuration.GetValue<string>("ServicBusConnectionString");
            });

            builder.Services.AddMailService(settings =>
            {
                settings.SmtpServer = builder.Configuration.GetValue<string>("SmtpServer");
                settings.SmtpPort = builder.Configuration.GetValue<int>("SmtpPort");
                settings.SmtpUser = builder.Configuration.GetValue<string>("SmtpUser"); ;
                settings.SmtpPassword = builder.Configuration.GetValue<string>("SmtpPassword"); ;
                settings.From = builder.Configuration.GetValue<string>("From");
            });


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.MapControllers();

            var serviceBusProcessor = app.Services.GetService<ICSServiceBusProcessor>();
            serviceBusProcessor.ProcessMessageAsync().GetAwaiter().GetResult();

            app.Run();
        }
    }
}