using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Mediator
{
    public interface IMediator
    {
        TResponse Send<TResponse>(IRequest<TResponse> request);
        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        void Send(IRequest request);
        Task SendAsync(IRequest request, CancellationToken cancellationToken = default);

        void Publish(INotification notification);
        Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
    }
}
