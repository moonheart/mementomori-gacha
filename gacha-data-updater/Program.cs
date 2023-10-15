using MementoMori;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using updater;

internal class Program
{
    private static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddLogging(log => log.AddSimpleConsole(c => c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "));
        builder.Services.AddOptions();
        builder.Services.Configure<AuthOption>(builder.Configuration.GetSection("Auth"));
        builder.Services.Configure<UpdaterOption>(builder.Configuration.GetSection("Updater"));
        builder.Services.AddSingleton<TimeManager>();
        builder.Services.AddSingleton<MementoNetworkManager>();
        builder.Services.AddHostedService<Updater>();

        builder.Build().Run();
    }
}