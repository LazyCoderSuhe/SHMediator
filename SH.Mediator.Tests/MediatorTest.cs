using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SH.Mediator.Exceptions;
using SH.Mediator.SHMediatorInterceptors;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace SH.Mediator.Tests
{
    [TestClass]
    public sealed class MediatorTest
    {
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
            var response = mediator.Send(request).GetAwaiter().GetResult();
            Assert.AreEqual("Handled Request1", response);
        }
        [TestMethod]
        public void Mediator_Send_RequestNoResponse()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            // 使用 Moq 框架來模擬 IRequestHandler<RequestNoResponse>
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            var mockHandler = new Mock<SH.Mediator.IRequestHandler<RequestNoRponse>>();
            services.AddTransient<SH.Mediator.IRequestHandler<RequestNoRponse>, SH.Mediator.IRequestHandler<RequestNoRponse>>(t => mockHandler!.Object);
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();
            var request = new RequestNoRponse();
            mediator.Send(request).GetAwaiter().GetResult();
            // 断言 Handle 方法是否被呼叫過一次
            mockHandler.Verify(h => h.Handle(It.IsAny<RequestNoRponse>()), Times.Once);

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
                mediator.Send(request).GetAwaiter().GetResult();
            });

        }


        [TestMethod]
        public void Mediator_Publish_NotFoundHandler()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SHMediatorOptions());
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            var mockNotifyHandler = new Mock<SH.Mediator.INotificationHandler<Notify>>();
            services.AddTransient<SH.Mediator.INotificationHandler<Notify>>(t => mockNotifyHandler.Object);
            var mockNotificationHandler2 = new Mock<SH.Mediator.INotificationHandler<Notify>>();
            services.AddTransient<SH.Mediator.INotificationHandler<Notify>>(t => mockNotificationHandler2.Object);
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var nati = new Notify();
            mediator.Publish(nati).GetAwaiter().GetResult();
            mockNotifyHandler.Verify(h => h.Handle(It.IsAny<Notify>()), Times.Once);
           // mockNotificationHandler2.Verify(h => h.Handle(It.IsAny<Notify>()), Times.Once);
        }

        [TestMethod]
        public async Task Mediator_LoggerInterceptor()
        {
            var services = new ServiceCollection();
            SHMediatorOptions options = new SHMediatorOptions(services.BuildServiceProvider());
            var interceptorMock = new Mock<ISHMediatorInterceptor>();
            interceptorMock
                .Setup(x => x.Publishing(It.IsAny<INotification>()))
                .ReturnsAsync(true);
            interceptorMock
                .Setup(x => x.Published(It.IsAny<INotification>()));

            options.Interceptors.Add(interceptorMock.Object);

            services.AddSingleton(options);
            services.AddLogging();
            services.AddSingleton<IMediator, SHMediator>();

            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var nati = new Notify();
            await mediator.Publish(nati);
            interceptorMock.Verify(h => h.Publishing(It.IsAny<INotification>()), Times.Once);
            interceptorMock.Verify(h => h.Published(It.IsAny<INotification>()), Times.Once);

        }



        [TestMethod]
        public async Task Mediator_ValidiatorInterceptor()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IMediator, SHMediator>();
            services.AddTransient<IValidator<Notify>, NotifyValidator>();
            SHMediatorOptions options = new SHMediatorOptions(services.BuildServiceProvider());
            options.Interceptors.Add(new SHFluentValidationInterceptor(services));

            services.AddSingleton(options);
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();

            var nati = new Notify();
            await Assert.ThrowsAsync<MediatorValidationException>(async () =>
              {
                  await mediator.Publish(nati);
              });
        }

        [TestMethod]
        public void Mediator_ValidMediatorInterceptor() {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSHMediator(typeof(Notify));
            Assert.IsTrue(true);
                  
        }
    }
    public class Notify : INotification
    {
        public string Name { get; set; }
    }

    public class NotifyValidator : AbstractValidator<Notify>
    {
        public NotifyValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name cannot be empty");
        }
    }

    



    public class RequestNoRponse : SH.Mediator.IRequest
    {
    }


    public class Request1 : SH.Mediator.IRequest<string>
    {

    }

    public class Request1Handler : SH.Mediator.IRequestHandler<Request1, string>
    {
        public Task<string> Handle(Request1 request)
        {
            return Task.FromResult("Handled Request1");
        }
    }


}
