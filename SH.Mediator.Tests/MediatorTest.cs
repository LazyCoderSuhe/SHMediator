using Microsoft.Extensions.DependencyInjection;

namespace SH.Mediator.Tests
{
    [TestClass]
    public sealed class MediatorTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var services = new ServiceCollection();
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<SH.Mediator.IRequestHandler<Request1, string>, Request1Handler>();
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetRequiredService<SH.Mediator.IMediator>();
            var request = new Request1();
            var response = mediator.Send(request).GetAwaiter().GetResult();
            Assert.AreEqual("Handled Request1", response);
        }
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
