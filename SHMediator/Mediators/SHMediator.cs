using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.Exceptions;
using SH.Mediator.Mediators;
using SH.Mediator.SHMediatorInterceptors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator
{
    public class SHMediator : IMediator
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly SHMediatorOptions _options;

        private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> _pipelinesRR = new();
        private readonly static ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> _handlerRRTypes = new();
        private readonly static ConcurrentDictionary<Type, Type> _handlerNotificationTypes = new();
        private readonly static ConcurrentDictionary<Type, Type> _handlerRTypes = new();
        private readonly static ConcurrentDictionary<(Type RequestType, Type ResponseType), MethodInfo> _handlerRRHandleMethods = new();

        public SHMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _options = _serviceProvider.GetService<SHMediatorOptions>();
        }

        private MediatorInterceptorDelegate<TResponse> BuildPipeline<TResponse>(
            MediatorInterceptorDelegate<TResponse> terminal)
        {
            MediatorInterceptorDelegate<TResponse> next = (request, cancellationToken) => terminal(request, cancellationToken);

            if (_options?.Interceptors is null || _options.Interceptors.Count == 0)
            {
                return terminal;
            }

            foreach (var item in _options.Interceptors)
            {
                var candidate = item;
                if (candidate.IsGenericTypeDefinition)
                {
                    candidate = candidate.MakeGenericType(typeof(TResponse));
                }

                if (!typeof(IMediatorInterceptor<TResponse>).IsAssignableFrom(candidate))
                {
                    continue;
                }

                var current = MediatorInterceptorMiddlewareFactory.CreateMiddleware<TResponse>(candidate, next, _serviceProvider);
                next = (request, cancellationToken) => current.Intercept(request, cancellationToken);
            }

            return (request, cancellationToken) => next(request, cancellationToken);
        }

        public TResponse Send<TResponse>(IRequest<TResponse> request)
        {
            return SendAsync(request, CancellationToken.None).Result;
        }

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            // pipeline 缓存仅与 (RequestType, ResponseType) 相关，token 作为每次调用参数传入
            var pipeline = (MediatorInterceptorDelegate<TResponse>)_pipelinesRR.GetOrAdd(
                (request.GetType(), typeof(TResponse)),
                _ =>
                {
                    MediatorInterceptorDelegate<TResponse> terminal = (r, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        GetSendHandlerAndMethed(r, out var handler, out var method);
                        try
                        {
                            return (Task<TResponse>)method.Invoke(handler, new object[] { r, ct })!;
                        }
                        catch (Exception ex)
                        {
                            throw ex.InnerException;
                        }
                    };
                    return BuildPipeline(terminal);
                });

            return await pipeline(request, cancellationToken);
        }

        public void Send(IRequest request)
        {
            SendAsync(request, CancellationToken.None).Wait();
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = (MediatorInterceptorDelegate<Unit>)_pipelinesRR.GetOrAdd(
                (request.GetType(), typeof(Unit)),
                _ =>
                {
                    MediatorInterceptorDelegate<Unit> terminal = (r, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        GetSendHandlerAndMethed(r, out var handler, out var method);
                        try
                        {
                            return (Task<Unit>)method.Invoke(handler, new object[] { r, ct })!;
                        }
                        catch (Exception ex)
                        {
                            throw ex.InnerException;
                        }

                    };
                    return BuildPipeline(terminal);
                });
            return pipeline(request, cancellationToken);
        }

        public void Publish(INotification notification)
        {
            PublishAsync(notification, CancellationToken.None).Wait();
        }

        public Task PublishAsync(INotification notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            var pipeline = (MediatorInterceptorDelegate<Unit>)_pipelinesRR.GetOrAdd(
           (notification.GetType(), typeof(Unit)),
              _ =>
            {
                MediatorInterceptorDelegate<Unit> terminal = (r, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var list = GetSendHandlerAndMethed(r, out var _handler, out var _method);
                    var tasks = new List<Task>();
                    foreach (var (handler, method) in list)
                    {
                        var task = (Task)method.Invoke(handler, new object[] { r, ct })!;
                        tasks.Add(task);
                    }
                    return Task.WhenAll(tasks).ContinueWith(t => Unit.Value, ct);
                };
                return BuildPipeline(terminal);
            });
            return pipeline(notification, cancellationToken);
        }

        private List<(object, MethodInfo)> GetPubshHandlers(INotification notification)
        {
            var requestType = notification.GetType();
            var handlerType = _handlerNotificationTypes.GetOrAdd(requestType,
                static key =>
                {
                    var _handlerType = typeof(INotificationHandler<>).MakeGenericType(key);
                    return typeof(IEnumerable<>).MakeGenericType(_handlerType);
                }
                );
            var handlersObj = _serviceProvider.GetService(handlerType);
            List<(object, MethodInfo)> result = new();

            if (handlersObj is IEnumerable handlers)
            {
                var notificationHandlerType = typeof(INotificationHandler<>).MakeGenericType(requestType);
                var handleMethod = notificationHandlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) })
                    ?? notificationHandlerType.GetMethod("Handle", new[] { requestType });

                if (handleMethod is null)
                    throw new MissingMethodException(notificationHandlerType.FullName, "Handle");

                foreach (var handler in handlers)
                {
                    if (handler is not null)
                    {
                        result.Add((handler, handleMethod));
                    }
                }
            }
            return result;
        }

        private List<(object, MethodInfo)> GetSendHandlerAndMethed<TResponse>(IRequest<TResponse> request, out object? handler, out MethodInfo? method)
        {
            var requestType = request.GetType();
            List<(object, MethodInfo)> result = new();
            // 判断 requestType 是否实现了 非泛型 IRequest 接口
            if (request is IRequest)
            {
                GetIRequestSendHandlerAndMethed((IRequest)request, out handler, out method);
                result.Add((handler, method));
            }
            else if (request is INotification)
            {
                result = GetPubshHandlers((INotification)request);
                handler = null;
                method = null;
            }
            else
            {
                GetIRequestResponseSendHandlerAndMethed(request, out handler, out method);
                result.Add((handler, method));
            }
            return result;
        }
        private void GetIRequestResponseSendHandlerAndMethed<TResponse>(IRequest<TResponse> request, out object? handler, out MethodInfo? method)
        {
            var requestType = request.GetType();
            var handlerType = _handlerRRTypes.GetOrAdd(
                  (requestType, typeof(TResponse)),
                  static key => typeof(IRequestHandler<,>).MakeGenericType(key.RequestType, key.ResponseType));
            handler = _serviceProvider.GetService(handlerType);
            if (handler is null)
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 handler");
            method = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });
            if (method is null)
            {
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 Handle 方法");
            }
        }

        private void GetIRequestSendHandlerAndMethed(IRequest request, out object? handler, out MethodInfo? method)
        {
            var requestType = request.GetType();
            var handlerType = _handlerRTypes.GetOrAdd(
                  requestType,
                  static key => typeof(IRequestHandler<>).MakeGenericType(key));

            handler = _serviceProvider.GetService(handlerType);
            if (handler is null)
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 handler");

            method = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });
            if (method is null)
            {
                throw new MediatorException($"未找到处理 {request.GetType().FullName} 的 Handle 方法");
            }
        }
    }
}
