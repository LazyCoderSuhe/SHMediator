using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.SHMediatorInterceptors;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SH.Mediator
{
    public static class MediatorServiceCollection
    {
        public static ServiceCollection AddSHMediator(this ServiceCollection services, Type type
            , Action<SHMediatorOptions> action = null
        )
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(type);

            services.AddSingleton<IMediator, SHMediator>();
            // 注册所有 Handler
            // 内部程序集
            RegionDi(services, typeof(IMediator));
            // 外部程序集
            RegionDi(services, type);
            // 注册默认日志拦截器
            Interceptors(services);
            // 配置选项
            ConfigOptions(services, action);
            return services;
        }

        private static void ConfigOptions(ServiceCollection services, Action<SHMediatorOptions> action)
        {
            SHMediatorOptions options = new SHMediatorOptions(services.BuildServiceProvider());
            if (options.UseLoggingInterceptor)
            {
                options.Interceptors.Add(services.BuildServiceProvider().GetRequiredService<SHMediatorLoggingInterceptor>());
            }
            if (options.UseFluentValidationInterceptor)
            {
                options.Interceptors.Add(services.BuildServiceProvider().GetRequiredService<SHFluentValidationInterceptor>());
            }
            action?.Invoke(options);
            services.AddSingleton(options);
        }

        private static void Interceptors(ServiceCollection services )
        {
            // 查找并注册所有拦截器
            var types = typeof(IMediator).Assembly.GetTypes();
            foreach ( var t in types ) {
                if (!t.IsClass || t.IsAbstract || t.IsInterface)
                    continue;
                foreach (var serviceType in t.GetInterfaces())
                {
                    if (serviceType == typeof(ISHMediatorInterceptor))
                    {
                        services.AddSingleton(typeof(ISHMediatorInterceptor), t);
                    }
                }
            }
        }

        private static void RegionDi(ServiceCollection service, Type type)
        {
            // 动态注册 IHandler
            var types = type.Assembly.GetTypes();

            foreach (var implementationType in types)
            {
                if (!implementationType.IsClass || implementationType.IsAbstract)
                    continue;

                foreach (var serviceType in implementationType.GetInterfaces())
                {
                    if (!serviceType.IsGenericType)
                        continue;

                    var def = serviceType.GetGenericTypeDefinition();
                    if (IsRegionType(def))
                        service.AddTransient(serviceType, implementationType);

                }
            }
        }

        private static bool IsRegionType(Type def)
        {
            return def == typeof(IRequestHandler<,>)
                                    || def == typeof(IRequestHandler<>)
                                    || def == typeof(INotificationHandler<>);
        }
    }
}
