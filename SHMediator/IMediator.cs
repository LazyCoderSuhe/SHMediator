using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request);
        Task Send(IRequest request);
    }
}
