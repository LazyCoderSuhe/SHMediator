using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator.SHMediatorInterceptors
{
    public interface ISHMediatorInterceptor
    {

        Task<bool> Sending<T>(IRequest<T> request);
        T Sended<T>(IRequest<T> request, T response);

        Task<bool> Sending(IRequest request);
        void Sended(IRequest request);

        Task<bool> Publishing(INotification notification);
        void Published(INotification notification);
    }

    public abstract class DefaultSHMediatorInterceptor : ISHMediatorInterceptor
    {

        public virtual Task<bool> Publishing(INotification notification ) => Task.FromResult(true);
        public virtual void Published(INotification notification ) {}


        public virtual  Task<bool>  Sending<T>(IRequest<T> request) => Task.FromResult(true);
        public virtual T Sended<T>(IRequest<T> request, T response ) => response;


        public virtual  Task<bool> Sending(IRequest request) => Task.FromResult(true);
        public virtual void Sended(IRequest request) { }
    }
}
