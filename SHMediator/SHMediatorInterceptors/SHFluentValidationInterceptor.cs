using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using SH.Mediator;
using SH.Mediator.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator.SHMediatorInterceptors
{


    public class FluentValidationInterceptor<TResponse> : DefaultMediatorInterceptor<TResponse>
    {
        private static readonly ConcurrentDictionary<Type, Type> _validators = new();
        private readonly IServiceProvider _serviceProvider;

        public FluentValidationInterceptor(MediatorInterceptorDelegate<TResponse> next, IServiceProvider serviceProvider)
            : base(next)
        {
            _serviceProvider = serviceProvider;
        }

       
        public override async Task<TResponse> Intercept(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validatorType = _validators.GetOrAdd(request.GetType(),
                static key => typeof(IValidator<>).MakeGenericType(key));

            var validator = _serviceProvider.GetService(validatorType);
            if (validator is not null)
            {
                var method = validatorType.GetMethod("ValidateAsync", new[] { request.GetType(), typeof(CancellationToken) });
                if (method is null)
                {
                    throw new MissingMethodException(validatorType.FullName, "ValidateAsync");
                }

                var result = await (Task<ValidationResult>)method.Invoke(validator, new object[] { request, cancellationToken })!;
                if (!result.IsValid)
                {
                    throw new MediatorValidationException(result.Errors);
                }
            }

            return await base.Intercept(request, cancellationToken);
        }
    }

    public static class FluentValidationInterceptorExtensions
    {
        public static SHMediatorOptions UseFluentValidationInterceptor(this SHMediatorOptions builder)
        {
            builder.UseSHMediatorInterceptor(typeof(FluentValidationInterceptor<>));
            return builder;
        }
    }



}
