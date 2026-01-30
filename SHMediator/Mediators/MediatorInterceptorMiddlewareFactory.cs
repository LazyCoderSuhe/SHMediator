using SH.Mediator.SHMediatorInterceptors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator.Mediators
{
    public static class MediatorInterceptorMiddlewareFactory
    {

        public static IMediatorInterceptor<TResponse> CreateMiddleware<TResponse>(
            Type middlewareType,
            MediatorInterceptorDelegate<TResponse> next,
            IServiceProvider serviceProvider)
        {
            var constructors = middlewareType.GetConstructors();
            var constructor = constructors.FirstOrDefault();
            if (constructor == null)
            {
                throw new InvalidOperationException($"Type {middlewareType.FullName} does not have a public constructor.");
            }

            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType == typeof(MediatorInterceptorDelegate<TResponse>))
                {
                    parameterInstances[i] = next;
                }
                else
                {
                    var service = serviceProvider.GetService(parameterType);
                    if (service == null)
                    {
                        throw new InvalidOperationException($"Unable to resolve service for type {parameterType.FullName} while attempting to activate {middlewareType.FullName}.");
                    }
                    parameterInstances[i] = service;
                }
            }

            return (IMediatorInterceptor<TResponse>)Activator.CreateInstance(middlewareType, parameterInstances);
        }
    }
}
