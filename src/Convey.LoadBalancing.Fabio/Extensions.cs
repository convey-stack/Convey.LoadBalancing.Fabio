using System;
using System.Linq;
using Consul;
using Convey.Discovery.Consul;
using Convey.LoadBalancing.Fabio.Builders;
using Convey.LoadBalancing.Fabio.Http;
using Convey.LoadBalancing.Fabio.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Convey.LoadBalancing.Fabio
{
    public static class Extensions
    {
        private const string SectionName = "fabio";
        private const string RegistryName = "loadBalancing.fabio";

        public static IConveyBuilder AddFabio(this IConveyBuilder builder, string sectionName = SectionName, string consulSettings = "consul")
        {
            var options = builder.GetOptions<FabioOptions>(SectionName);
            var consulOptions = builder.GetOptions<ConsulOptions>(consulSettings);
            return builder.AddFabio(options, b => b.AddConsul(consulOptions));
        }
        
        public static IConveyBuilder AddFabio(this IConveyBuilder builder, Func<IFabioOptionsBuilder, IFabioOptionsBuilder> buildOptions,
            Func<IConsulOptionsBuilder, IConsulOptionsBuilder> buildConsulOptions)
        {
            var options = buildOptions(new FabioOptionsBuilder()).Build();
            return builder.AddFabio(options, b => b.AddConsul(buildConsulOptions));
        }
        
        public static IConveyBuilder AddFabio(this IConveyBuilder builder, FabioOptions options, ConsulOptions consulOptions)
            => builder.AddFabio(options, b => b.AddConsul(consulOptions));

        private static IConveyBuilder AddFabio(this IConveyBuilder builder, FabioOptions options, Action<IConveyBuilder> registerConsul)
        {
            if (!options.Enabled || !builder.TryRegister(RegistryName))
            {
                return builder;
            }

            registerConsul(builder);
            builder.Services.AddSingleton(options);
            builder.Services.AddTransient<FabioMessageHandler>();
            builder.Services.AddHttpClient<IFabioHttpClient, FabioHttpClient>()
                .AddHttpMessageHandler<FabioMessageHandler>();

            using (var serviceProvider = builder.Services.BuildServiceProvider())
            {
                var registration = serviceProvider.GetService<AgentServiceRegistration>();
                registration.Tags = GetFabioTags(registration.Name, options.Service);
                builder.Services.UpdateConsulRegistration(registration);
            }

            return builder;
        }
        
        public static void AddFabioHttpClient(this IConveyBuilder builder, string clientName, string serviceName)
            => builder.Services.AddHttpClient(clientName)
                .AddHttpMessageHandler(c =>
                    new FabioMessageHandler(c.GetService<FabioOptions>(), serviceName));
        
        private static void UpdateConsulRegistration(this IServiceCollection services, AgentServiceRegistration registration)
        {
            var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(AgentServiceRegistration));
            services.Remove(serviceDescriptor);
            services.AddSingleton(registration);
        }
        
        private static string[] GetFabioTags(string consulService, string fabioService)
        {
            var service = (string.IsNullOrWhiteSpace(fabioService) ? consulService : fabioService)
                .ToLowerInvariant();

            return new[] {$"urlprefix-/{service} strip=/{service}"};
        }
    }
}