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
        private readonly IServiceProvider _serviceProvider;
        public SHFluentValidationInterceptor(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override Task<bool> Sending(IRequest request)
        {
            ValidationCore(request, request.GetType());
            return base.Sending(request);
        }
        public override Task<bool> Sending<T>(IRequest<T> request )
        {
            ValidationCore(request, request.GetType());
            return base.Sending(request);
        }

        public override Task<bool> Publishing(INotification notification)
        {
            ValidationCore(notification, notification.GetType());
            return base.Publishing(notification );
        }

        private void ValidationCore(dynamic request , Type type)
        {
            var validatorType = _validators.GetOrAdd(type,
                static key => typeof(IValidator<>).MakeGenericType(key)
                );
            dynamic validator = _serviceProvider.GetService(validatorType);
            if (validator is not null)
            {
                var result = validator.ValidateAsync(request).Result as ValidationResult;
                if (!result.IsValid)
                {
                    throw new MediatorValidationException(result.Errors);
                }
            }
        }
    }
}
