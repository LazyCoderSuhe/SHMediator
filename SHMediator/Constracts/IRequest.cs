using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{


    public interface IRequest<TResponse> 
    {
    }

    public interface IRequest : IRequest<Unit>
    {
    }

}
