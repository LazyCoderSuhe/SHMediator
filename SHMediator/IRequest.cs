using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public interface IRequest<TResponse>: IRequest
    {
    }


    public interface IRequest 
    {
    }

}
