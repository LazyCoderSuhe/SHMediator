using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public interface IRequestHandler<TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request);
    }

    public interface IRequestHandler<TRequest>
    {
        Task Handle(TRequest request);
    }
}
