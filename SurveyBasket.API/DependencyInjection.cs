using FluentValidation.AspNetCore;
using MapsterMapper;
using SurveyBasket.API.Services;
using System.Reflection;

namespace SurveyBasket.API;

public static class DependencyInjection
{
     public static IServiceCollection AddDependencies(this IServiceCollection services)
     {
        services.AddControllers();

        services
            .AddSwaggerServices()
            .AddMapsterConfig()
            .AddFluentValidateion();


        services.AddScoped<IPollSerivce, PollSerivce>();

        return services;
     }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IServiceCollection AddMapsterConfig(this IServiceCollection services)
    {

        // Add Mapster
        var mappingConfig = TypeAdapterConfig.GlobalSettings;

        mappingConfig.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton<IMapper>(new Mapper(mappingConfig));

        return services;
    }

    public static IServiceCollection AddFluentValidateion(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}