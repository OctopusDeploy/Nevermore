using Microsoft.Extensions.DependencyInjection;
using Nevermore.Advanced;

namespace Nevermore
{
    public static class NevermoreServiceCollectionExtensions
    {
        public static void AddNevermore(this IServiceCollection services, RelationalStoreConfiguration options)
        {
            services.AddSingleton<IRelationalStoreConfiguration>(options);
            services.AddSingleton<IRelationalStore, RelationalStore>();

            services.AddScoped(s => s.GetRequiredService<IRelationalStore>().BeginReadTransaction());
            services.AddScoped(s => s.GetRequiredService<IRelationalStore>().BeginWriteTransaction());
            services.AddScoped(s => s.GetRequiredService<IRelationalStore>().BeginTransaction());
        }

        public static void AddNevermore(this IServiceCollection services, string connectionString)
        {
            var options = new RelationalStoreConfiguration(connectionString);
            AddNevermore(services, options);
        }
    }
}