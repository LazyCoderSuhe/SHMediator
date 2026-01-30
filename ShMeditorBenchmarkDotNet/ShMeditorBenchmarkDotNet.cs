using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SH.Mediator;
using SH.Mediator.Mediators;
using SH.Mediator.SHMediatorInterceptors;
using System.Threading;

namespace ShMeditorBenchmarkDotNet
{
    [MemoryDiagnoser]
    public class MeditorBenchmarkDotNet
    {
        private ServiceProvider _provider = default!;
        private IMediator _mediator = default!;
        private Request1 _request = default!;

        private Notify _notify = default!;
        private CancellationToken _ct;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            var options = new SHMediatorOptions();
            options.UseFluentValidationInterceptor();
            services.AddSingleton(options);
            services.AddSingleton<IMediator, SHMediator>();
            services.AddTransient<IRequestHandler<Request1, string>, Request1Handler>();
            services.AddTransient<IValidator<Notify>, NotifyValidator>();

            _provider = services.BuildServiceProvider();
            _mediator = _provider.GetRequiredService<IMediator>();
            _request = new Request1();
            _notify = new Notify { Name = "ok" };
            _ct = CancellationToken.None;
        }

        [GlobalCleanup]
        public void Cleanup() => _provider.Dispose();

        [Benchmark]
        public Task<string> SendAsync_RequestResponse() => _mediator.SendAsync(_request);

        [Benchmark]
        public Task PublishAsync_Validation_WithCancellationToken()
            => _mediator.PublishAsync(_notify, _ct);

    }

    public sealed class Request1 : IRequest<string>
    {
    }

    public sealed class Request1Handler : IRequestHandler<Request1, string>
    {
        public Task<string> Handle(Request1 request, CancellationToken cancellationToken)
            => Task.FromResult("ok");

  
    }

    public sealed class Notify : INotification
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class NotifyValidator : AbstractValidator<Notify>
    {
        public NotifyValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
