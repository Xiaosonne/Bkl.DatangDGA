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
}
