using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SH.Mediator.SHMediatorInterceptors
{
    public class SHFluentValidationInterceptor : DefaultSHMediatorInterceptor
    {
        private ConcurrentDictionary<Type, Type> _validators = new();
        private Lazy<IServiceProvider> _serviceProvider
            => new Lazy<IServiceProvider>(() => services.BuildServiceProvider());

        private readonly ServiceCollection services;
        public SHFluentValidationInterceptor(ServiceCollection serviceCollection)
        {
            services = serviceCollection;
        }

        public override async Task<bool> Sending(IRequest request)
        {
            await ValidationCore(request, request.GetType());
            return await base.Sending(request);
        }
        public override async Task<bool> Sending<T>(IRequest<T> request)
        {
           await ValidationCore(request, request.GetType());
            return await base.Sending(request);
        }

        public override async Task<bool> Publishing(INotification notification)
        {
            await ValidationCore(notification, notification.GetType());
            return await base.Publishing(notification);
        }

        private async Task ValidationCore<IRequest>(IRequest request, Type type)
        {
            var validatorType = _validators.GetOrAdd(type,
                static key => typeof(IValidator<>).MakeGenericType(key)
                );
            var validator = _serviceProvider.Value.GetService(validatorType);
            if (validator is not null)
            {
                var method = validatorType.GetMethod("ValidateAsync", new Type[] { type, typeof(System.Threading.CancellationToken) });
                var result = await (Task<ValidationResult>)method.Invoke(validator, new object[] { request, default });
                if (!result.IsValid)
                {
                    throw new MediatorValidationException(result.Errors);
                }
            }
        }
    }
}
