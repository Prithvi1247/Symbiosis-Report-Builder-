using BetaSnapReporting.Interfaces;
using BetaSnapReporting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BetaSnapReporting.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IMetadataService, MetadataService>(); // Sprint 3 Registration
        services.AddSingleton<IFilterService, FilterService>();
        return services;
    }
}