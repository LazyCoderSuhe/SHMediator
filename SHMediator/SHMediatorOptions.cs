using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.SHMediatorInterceptors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public class SHMediatorOptions
    {
        private readonly ServiceProvider _services;
        public SHMediatorOptions()
        {
            
        }
        public SHMediatorOptions(ServiceProvider services)
        {
            _services = services;
        }

        public bool UseLoggingInterceptor
        {
            get; set;
        } = true;
        public bool UseFluentValidationInterceptor
        {
            get;
            set;
        } = true;
        public List<ISHMediatorInterceptor> Interceptors { get; } = new();

        public void AddInterceptor<T>() where T : ISHMediatorInterceptor
        {
            var interceptor = _services.GetService<T>();
            if (interceptor == null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }
            Interceptors.Add(interceptor);
        }
    }

}
