using System;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator.SHMediatorInterceptors
{
    
    public delegate Task<TResponse> MediatorInterceptorDelegate<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken);

}
