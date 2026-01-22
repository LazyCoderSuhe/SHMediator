using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public interface INotificationHandler<TNotification> where TNotification : INotification
    {
        Task Handle(TNotification notification);
    }
}
