using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator.SHMediatorInterceptors
{
    public class MediatorLoggingInterceptor<TResponse> : DefaultMediatorInterceptor<TResponse>
    {
        private readonly ILogger<MediatorLoggingInterceptor<TResponse>> _logger;

        public MediatorLoggingInterceptor(ILogger<MediatorLoggingInterceptor<TResponse>> logger, MediatorInterceptorDelegate<TResponse> next)
            : base(next)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public override Task<TResponse> Intercept(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger?.LogInformation("请求类型{RequestType} 正在发送", request.GetType().Name);
            try
            {
                return base.Intercept(request, cancellationToken);
            }
            finally
            {
                _logger?.LogInformation("请求类型{RequestType} 已经发送", request.GetType().Name);
            }
        }


    }

    public static class MediatorLoggingInterceptorExtensions
    {
        public static SHMediatorOptions UseMediatorLoggingInterceptor(this SHMediatorOptions builder)
        {
            return builder.UseSHMediatorInterceptor(typeof(MediatorLoggingInterceptor<>));
        }
    }
}
