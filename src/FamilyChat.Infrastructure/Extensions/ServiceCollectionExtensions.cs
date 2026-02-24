using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Options;
using FamilyChat.Infrastructure.Auth;
using FamilyChat.Infrastructure.Email;
using FamilyChat.Infrastructure.Ids;
using FamilyChat.Infrastructure.Options;
using FamilyChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyChat.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddDbContext<FamilyChatDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("SqlServer"));
        });

        services.AddScoped<IFamilyChatDbContext>(provider => provider.GetRequiredService<FamilyChatDbContext>());
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IUlidGenerator, UlidGenerator>();

        return services;
    }
}
