using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public class SHMediator : IMediator
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly SHMediatorOptions _options;
        public SHMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _options = _serviceProvider.GetService<SHMediatorOptions>() ?? new SHMediatorOptions();
        }

        private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> _handlerRRTypes = new();

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {

            foreach (var item in _options.Interceptors)
            {
                var result = await item.Sending(request);
                if (!result)
                {
                    return default;
                }
            }

            var requestType = request.GetType();
            var handlerType = _handlerRRTypes.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => typeof(IRequestHandler<,>).MakeGenericType(key.RequestType, key.ResponseType));
            dynamic handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 handler");
            }
            var response = await handler.Handle((dynamic)request);

            foreach (var item in _options.Interceptors)
            {
                response = await item.Sended(request, response );
            }

            return response;
        }
        private readonly ConcurrentDictionary<Type, Type> _handlerRTypes = new();

        public async Task Send(IRequest request)
        {
            foreach (var item in _options.Interceptors)
            {
                var result = await item.Sending(request);
                if (!result)
                {
                    return;
                }
            }

            var requestType = request.GetType();
            var handlerType = _handlerRTypes.GetOrAdd(
                requestType,
                static key => typeof(IRequestHandler<>).MakeGenericType(key));
            dynamic handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 handler");
            }
            await handler.Handle((dynamic)request);


            foreach (var item in _options.Interceptors)
            {
                item.Sended(request);
            }
        }


        private readonly ConcurrentDictionary<Type, Type> _handlerNotificationTypes = new();

        public async Task Publish(INotification notification)
        {
            foreach (var item in _options.Interceptors)
            {
                var result = await item.Publishing(notification);
                if (!result)
                {
                    return;
                }
            }



            var requestType = notification.GetType();
            var handlerType = _handlerNotificationTypes.GetOrAdd(requestType,
                static key =>
                {
                    var _handlerType = typeof(INotificationHandler<>).MakeGenericType(key);
                    return typeof(IEnumerable<>).MakeGenericType(_handlerType);
                }
                );
            var handlers = (IEnumerable<dynamic>?)_serviceProvider.GetService(handlerType);
            if (handlers != null)
            {
                var tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    tasks.Add(handler.Handle((dynamic)notification));
                }
                await Task.WhenAll(tasks);
            }

            foreach (var item in _options.Interceptors)
            {
                item.Published(notification);
            }

        }
    }
}
