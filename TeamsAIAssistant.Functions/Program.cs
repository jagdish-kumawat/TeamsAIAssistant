using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamsAIAssistant.Functions.Interfaces;
using TeamsAIAssistant.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        s.AddSingleton<IAzureStorageHelper, AzureStorageHelper>();
    })
    .Build();

host.Run();
