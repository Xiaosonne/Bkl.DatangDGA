using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
