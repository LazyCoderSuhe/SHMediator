using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Threading;
using SH.Mediator.Exceptions;
using SH.Mediator.SHMediatorInterceptors;

namespace SH.Mediator.Tests
{
    [TestClass]
    public sealed class MediatorTest
    {
        [TestMethod]
        public async Task Mediator_SendAsync_NullRequest_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await mediator.SendAsync<string>(null!);
            });
        }

        [TestMethod]
        public async Task Mediator_SendAsync_NullRequestNoResponse_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await mediator.SendAsync(null!);
            });
        }

        [TestMethod]
        public async Task Mediator_PublishAsync_NullNotification_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await mediator.PublishAsync(null!);
            });
        }

        [TestMethod]
        public async Task Mediator_SendAsync_CanceledToken_Throws()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<SH.Mediator.IRequestHandler<Request1, string>, Request1Handler>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await mediator.SendAsync(new Request1(), cts.Token);
            });
        }

        [TestMethod]
        public async Task Mediator_PublishAsync_CanceledToken_Throws()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await mediator.PublishAsync(new NotifyOne(), cts.Token);
            });
        }


        [TestMethod]
        public async Task Mediator_SendAsync_PipelineCached_StillResolvesHandlerPerCall()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<SH.Mediator.IRequestHandler<Request1, string>, Request1CountingHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var r1 = await mediator.SendAsync(new Request1());
            var r2 = await mediator.SendAsync(new Request1());

            Assert.AreEqual("Handled Request1 #1", r1);
            Assert.AreEqual("Handled Request1 #2", r2);
        }

        [TestMethod]
        public async Task Mediator_SendAsync_InterceptorShortCircuits_TerminalNotInvoked()
        {
            var services = new ServiceCollection();
            var options = new SHMediatorOptions();
            options.Interceptors.Add(typeof(ShortCircuitInterceptor<>));
            services.AddSingleton(options);

            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            var handlerMock = new Mock<SH.Mediator.IRequestHandler<Request1, string>>(MockBehavior.Strict);
            services.AddTransient(_ => handlerMock.Object);
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var result = await mediator.SendAsync(new Request1());
            Assert.AreEqual("short-circuit", result);
            handlerMock.Verify(h => h.Handle(It.IsAny<Request1>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void Mediator_Send_RequestResponse()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<SH.Mediator.IRequestHandler<Request1, string>, Request1Handler>();
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();
            var request = new Request1();
            var response = mediator.SendAsync(request).GetAwaiter().GetResult();
            Assert.AreEqual("Handled Request1", response);
        }
        [TestMethod]
        public void Mediator_Send_RequestNoResponse()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();

            services.AddScoped<SH.Mediator.IRequestHandler<RequestNoRponse>, RequestNoResponseHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();
            var request = new RequestNoRponse();
            mediator.SendAsync(request).GetAwaiter().GetResult();

            var handler = serviceProvider.GetRequiredService<SH.Mediator.IRequestHandler<RequestNoRponse>>();
            Assert.AreEqual(1, ((RequestNoResponseHandler)handler).CallCount);

        }

        [TestMethod]
        public void Mediator_Send_RequestNoResponse_HandlerNotFound()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();

            var serviceProvider = services.BuildServiceProvider();



            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();
            var request = new RequestNoRponse();

            Assert.Throws<MediatorException>(() =>
            {
                mediator.SendAsync(request).GetAwaiter().GetResult();
            });

        }


        [TestMethod]
        public void Mediator_Publish_NotFoundHandler()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddScoped<SH.Mediator.INotificationHandler<NotifyOne>, CountingNotifyHandler1>();
            services.AddScoped<SH.Mediator.INotificationHandler<NotifyOne>, CountingNotifyHandler2>();
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var nati = new NotifyOne();
            mediator.PublishAsync(nati).GetAwaiter().GetResult();

            var listRreuslt = serviceProvider.GetServices<SH.Mediator.INotificationHandler<NotifyOne>>();

            foreach (var item in listRreuslt)
            {
                if (item is CountingNotifyHandler1 countingNotifyHandler1)
                {
                    Assert.AreEqual(1, countingNotifyHandler1.CallCount);
                }
                else if (item is CountingNotifyHandler2 countingNotifyHandler2)
                {
                    Assert.AreEqual(1, countingNotifyHandler2.CallCount);
                }
            }
        }


        [TestMethod]
        public void Mediator_Send_SyncMethod_Works()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<SH.Mediator.IRequestHandler<Request1, string>, Request1Handler>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var response = mediator.Send(new Request1());
            Assert.AreEqual("Handled Request1", response);
        }

        [TestMethod]
        public void Mediator_Publish_SyncMethod_Works()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<INotificationHandler<NotifyOne>, CountingNotifyHandler1>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            mediator.Publish(new NotifyOne { Name = "ok" });
            Assert.AreEqual(1, serviceProvider.GetRequiredService<CountingNotifyHandler1>().CallCount);
        }

        [TestMethod]
        public async Task Mediator_SendAsync_HandlerMissingCorrectHandleSignature_ThrowsMediatorException()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            var mockHandler = new Mock<SH.Mediator.IRequestHandler<BadHandleRequest, string>>();
            mockHandler.Setup(x => x.Handle(It.IsAny<BadHandleRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new MediatorException("Handler error"));

            services.AddTransient<SH.Mediator.IRequestHandler<BadHandleRequest, string>>(t => {
                return mockHandler.Object; 
            });

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            await Assert.ThrowsAsync<MediatorException>(async () =>
            {
                await mediator.SendAsync(new BadHandleRequest());
            });
        }

        [TestMethod]
        public async Task Mediator_ValidiatorInterceptor()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IMediator, SHMediator>();
            services.AddTransient<IValidator<NotifyOne>, NotifyValidator>();
            services.AddTransient<INotificationHandler<NotifyOne>, NotifyHandler>();
            SHMediatorOptions options = new SHMediatorOptions();
            options.UseFluentValidationInterceptor();
            // 让 mediator 能拿到 options
            services.AddSingleton(options);
            var serviceProvider = services.BuildServiceProvider();


            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var nati = new NotifyOne();
            await Assert.ThrowsAsync<MediatorValidationException>(async () =>
              {
                  await mediator.PublishAsync(nati);
              });
        }


    }
    public class NotifyOne : INotification
    {
        public string Name { get; set; }
    }

    public class NotifyValidator : AbstractValidator<NotifyOne>
    {
        public NotifyValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name cannot be empty");
        }
    }





    public class RequestNoRponse : SH.Mediator.IRequest
    {
    }

    public sealed class RequestNoResponseHandler : SH.Mediator.IRequestHandler<RequestNoRponse>
    {
        public int CallCount { get; private set; }

        public Task<SH.Mediator.Unit> Handle(RequestNoRponse request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(SH.Mediator.Unit.Value);
        }
    }


    public class Request1 : SH.Mediator.IRequest<string>
    {

    }

    public sealed class BadHandleRequest : SH.Mediator.IRequest<string>
    {
    }

    public class Request1Handler : SH.Mediator.IRequestHandler<Request1, string>
    {
        public Task<string> Handle(Request1 request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Handled {request.GetType().Name}");
        }
    }

    public sealed class BadHandleRequestHandler : SH.Mediator.IRequestHandler<BadHandleRequest, string>
    {
        Task<string> SH.Mediator.IRequestHandler<BadHandleRequest, string>.Handle(BadHandleRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult($"Handled {request.GetType().Name}");
    }

    public sealed class Request1CountingHandler : SH.Mediator.IRequestHandler<Request1, string>
    {
        private int _count;

        public Task<string> Handle(Request1 request, CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _count);
            return Task.FromResult($"Handled {request.GetType().Name} #{current}");
        }
    }

    public sealed class ShortCircuitInterceptor<TResponse> : IMediatorInterceptor<TResponse>
    {
        public Task<TResponse> Intercept(IRequest<TResponse> request)
            => Intercept(request, CancellationToken.None);

        public Task<TResponse> Intercept(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            if (typeof(TResponse) == typeof(string))
            {
                return Task.FromResult((TResponse)(object)"short-circuit");
            }

            throw new InvalidOperationException("Unexpected response type for ShortCircuitInterceptor.");
        }
    }

    public sealed class NotifyHandler : INotificationHandler<NotifyOne>
    {
        public Task Handle(NotifyOne notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class CountingNotifyHandler1 : INotificationHandler<NotifyOne>
    {
        public int CallCount { get; private set; }

        public Task Handle(NotifyOne notification, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    public sealed class CountingNotifyHandler2 : INotificationHandler<NotifyOne>
    {
        public int CallCount { get; private set; }

        public Task Handle(NotifyOne notification, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

}
