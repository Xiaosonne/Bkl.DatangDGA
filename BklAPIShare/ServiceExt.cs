using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

public static class ServiceExt
{
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
    public static BklConfig.Database AddOceanbase(this IServiceCollection services, IConfiguration conf)
    {
        BklConfig.Database gaussConfig = new BklConfig.Database();
        conf.GetSection("Oceanbase").Bind(gaussConfig);
        var Oceanbase = gaussConfig.GetEncrypt();

        services.AddSingleton(Oceanbase);
        services.AddSingleton(new DbContextOptionsBuilder<BklDbContext>().UseMySQL(Oceanbase.GetConnectionString()));
        services.AddDbContext<BklDbContext>((serviceProvider, builder) =>
        {
            builder.UseMySQL(Oceanbase.GetConnectionString());
        });
        return Oceanbase;
    }

    public static BklConfig.Database AddDatabase(this IServiceCollection services, IConfiguration conf)
    {
        return services.AddMySQL(conf);
    }
    public static BklConfig.Database AddMySQL(this IServiceCollection services, IConfiguration conf)
    {
        BklConfig.Database gaussConfig = new BklConfig.Database();
        conf.GetSection("MySql").Bind(gaussConfig);
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
