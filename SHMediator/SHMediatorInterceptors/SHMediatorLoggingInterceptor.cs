using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator.SHMediatorInterceptors
{
    public class SHMediatorLoggingInterceptor : ISHMediatorInterceptor
    {
        private readonly ILogger<SHMediatorLoggingInterceptor> _logger;

        public SHMediatorLoggingInterceptor(ServiceProvider serviceProvider)
        {
             _logger = serviceProvider.GetService<ILogger<SHMediatorLoggingInterceptor>>();
        }



        public void Published(INotification notification) =>
            _logger?.LogInformation("通知类型{NotificationType} 已经发布", notification.GetType().Name);


        public Task<bool> Publishing(INotification notification )
        {           
            _logger?.LogInformation("通知类型{NotificationType} 正在发布", notification.GetType().Name);
            return Task.FromResult(true);
        }

        public T Sended<T>(IRequest<T> request, T response )
        {
            _logger?.LogInformation("请求类型{RequestType} 已经发送，响应类型{ResponseType}", request.GetType().Name, typeof(T).Name);
            return response;
        }

        public void Sended(IRequest request ) =>
            _logger?.LogInformation("请求类型{RequestType} 已经发送", request.GetType().Name);


        public Task<bool> Sending<T>(IRequest<T> request )
        {         
            _logger?.LogInformation("请求类型{RequestType} 正在发送，响应类型{ResponseType}", request.GetType().Name, typeof(T).Name);
            return Task.FromResult(true);
        }

        public Task<bool> Sending(IRequest request)
        {
            _logger?.LogInformation("请求类型{RequestType} 正在发送", request.GetType().Name);
            return Task.FromResult(true);
        }
    }
}
