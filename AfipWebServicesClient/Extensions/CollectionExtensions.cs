using AfipWebServicesClient;
using Microsoft.Extensions.DependencyInjection;

namespace Eternet.Invoices.Out.WebApi.Extensions;

public static class CollectionExtensions
{
    public static void AddAfipServices(this IServiceCollection services)
    {
        services.AddSingleton<IAfipFeSoapClientFactory, AfipFeSoapClientFactory>();
    }

}