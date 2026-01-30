using System;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator.SHMediatorInterceptors
{


    public interface IMediatorInterceptor<TResponse>
    {
        public Task<TResponse> Intercept(IRequest<TResponse> request, CancellationToken cancellationToken);
    }


    public abstract class DefaultMediatorInterceptor<TResponse> : IMediatorInterceptor<TResponse>
    {
        private readonly MediatorInterceptorDelegate<TResponse> _next;
        protected DefaultMediatorInterceptor(MediatorInterceptorDelegate<TResponse> next)
        {
            _next = next;
        }

        public virtual Task<TResponse> Intercept(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            return _next(request, cancellationToken);
        }
    }
}
