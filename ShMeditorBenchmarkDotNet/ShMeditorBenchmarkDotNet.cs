using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShMeditorBenchmarkDotNet
{
    public class MeditorBenchmarkDotNet
    {
        ServiceCollection services = new ServiceCollection();
        ServiceProvider serviceProvider;
  
        public MeditorBenchmarkDotNet()
        {
            services.AddSingleton<SH.Mediator.IMediator, SH.Mediator.SHMediator>();
            services.AddTransient<SH.Mediator.IRequestHandler<Request1, string>, Request1Handler>();
            serviceProvider = services.BuildServiceProvider();

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
