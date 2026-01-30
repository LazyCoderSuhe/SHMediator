using System;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator
{
    public interface IRequestHandler<TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
    }
    public interface IRequestHandler<TRequest>: IRequestHandler<TRequest, Unit>
    {
    }
}
