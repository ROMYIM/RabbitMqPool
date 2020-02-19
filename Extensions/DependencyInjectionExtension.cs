using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.DependencyInjection
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddRabbitMqHelper(this IServiceCollection services, Action<RabbitMqOptions> build, IConfiguration configuration)
        {
            var options = new RabbitMqOptions();
            options.Configuration = configuration;
            build(options);

            services.AddSingleton(options);
            services.AddSingleton<RabbitMqConnectionTable>();
            services.AddSingleton<RabbitMqHelper>();

            return services;
        }
    }
}
