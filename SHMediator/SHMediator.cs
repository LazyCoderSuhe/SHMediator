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
        public SHMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> _handlerRRTypes = new();

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            var requestType = request.GetType();
            var handlerType = _handlerRRTypes.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => typeof(IRequestHandler<,>).MakeGenericType(key.RequestType, key.ResponseType));
            dynamic handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 handler");
            }
            return await handler.Handle((dynamic)request);
        }
        private readonly ConcurrentDictionary<Type, Type> _handlerRTypes = new();

        public async Task Send(IRequest request)
        {
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
        }
    }
}
