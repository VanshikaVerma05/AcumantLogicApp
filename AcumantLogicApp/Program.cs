//using Microsoft.Azure.Functions.Worker.Builder;
//using Microsoft.Extensions.Hosting;

//var builder = FunctionsApplication.CreateBuilder(args);

//builder.ConfigureFunctionsWebApplication();



//builder.Build().Run();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

public class Program
{
    public static void Main()
    {
        CreateHostBuilder().Build().Run();
    }

    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.Services.AddHttpClient();  // Register HttpClient
            })
            .ConfigureServices(services =>
            {
                // You can add other services here if needed
                // services.AddSingleton<IMyService, MyService>(); 
            });
}












