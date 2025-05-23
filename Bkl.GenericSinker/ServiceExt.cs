using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ServiceExt
{
    public static void AddDynamicHostedService( this IServiceCollection services, Type t1)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (!typeof(IHostedService).IsAssignableFrom(t1))
            throw new InvalidOperationException($"{t1.FullName} 必须继承 IHostedService");

        var method = typeof(ServiceCollectionHostedServiceExtensions)
            .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), new[] { typeof(IServiceCollection) });

        var genericMethod = method.MakeGenericMethod(t1);
        genericMethod.Invoke(null, new object[] { services });
    }

    public static BklConfig.Database AddGaussDb(this IServiceCollection services, IConfiguration conf)
    {
        BklConfig.Database gaussConfig = new BklConfig.Database();
        conf.GetSection("GaussDb").Bind(gaussConfig);
        var gaussDecryptConfig = gaussConfig.GetDecrypt();
        services.AddSingleton(gaussDecryptConfig);
        services.AddSingleton(new DbContextOptionsBuilder<BklDbContext>().UseOpenGauss(gaussDecryptConfig.GaussDb));
        services.AddDbContext<BklDbContext>((serviceProvider, builder) =>
        {
            builder.UseOpenGauss(gaussDecryptConfig.GaussDb);
        });
        return gaussDecryptConfig;
    }

    public static BklConfig.Database AddMySQL(this IServiceCollection services, IConfiguration conf, string sqlConfName)
    {
        BklConfig.Database gaussConfig = new BklConfig.Database();
        conf.GetSection(sqlConfName).Bind(gaussConfig);
        var Oceanbase = gaussConfig.GetEncrypt();

        services.AddSingleton(Oceanbase);
        services.AddSingleton(new DbContextOptionsBuilder<BklDbContext>().UseMySQL(Oceanbase.GetConnectionString()));
        services.AddDbContext<BklDbContext>((serviceProvider, builder) =>
        {
            builder.UseMySQL(Oceanbase.GetConnectionString());
        });
        return Oceanbase;
    }

}
