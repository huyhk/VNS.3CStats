using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VNS.ThreeCStats.Infrastructure.Identity;
using VNS.ThreeCStats.Infrastructure.Persistence;
using VNS.ThreeCStats.Infrastructure.Sources.Ceb;

namespace VNS.ThreeCStats.Infrastructure;

public static class DependencyInjection
{
    public const string EmergencyCookieScheme = "EmergencyLoginCookie";
    public const string SuperAdminPolicy = "SuperAdmin";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthenticationOptions>(
            configuration.GetSection(AuthenticationOptions.SectionName));
        services.Configure<EmergencyLoginOptions>(
            configuration.GetSection(EmergencyLoginOptions.SectionName));

        services.AddSingleton<IEmergencyLoginService, EmergencyLoginService>();
        services.AddSingleton<CebMatchParser>();
        services.AddScoped<CebIngestionService>();
        services.AddHttpClient<CebSourceClient>(CebSourceClient.ConfigureHttpClient);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=3cstats.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication()
            .AddCookie(EmergencyCookieScheme, options =>
            {
                options.Cookie.Name = ".VNS.3CStats.Emergency";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = false;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SuperAdminPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(
                    IdentityConstants.ApplicationScheme,
                    EmergencyCookieScheme);
                policy.RequireRole(AppRoles.SuperAdmin);
            });
        });

        return services;
    }
}
