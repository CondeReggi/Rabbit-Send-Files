using Cliente.Helpers;
using Cliente.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cliente
{
    public class Program
    {
        public static IConfiguration Configuration { get; private set; }

        public static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceProvider = ConfigureServices();

            using (var scope = serviceProvider.CreateScope())
            {
                try
                {
                    var app = scope.ServiceProvider.GetRequiredService<FileProcessingApp>();
                    await app.RunAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while running the application.");
                }
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(config => config.AddConsole());
            services.Configure<RabbitMqSettings>(Configuration.GetSection("RabbitMqSettings"));
            services.AddSingleton<FileProcessingApp>();

            services.AddDbContext<DataBaseContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Database")));

            services.AddScoped<FileRepository>();

            return services.BuildServiceProvider();
        }
    }
}
