using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SH.Mediator.Tests
{
    [TestClass]
    public class MediatorServiceCollectionTests
    {
        public class Notify: IRequest
        {
            public string? Title { get; set; }
            public string? Message { get; set; }
        }
        public class NotifyHandler : IRequestHandler<Notify>
        {
            public Task<Unit> Handle(Notify request, CancellationToken cancellationToken)
            {
                // Handle the notification logic here
                return Task.FromResult(Unit.Value);
            }
        }


        public class NotifyValidator : AbstractValidator<Notify>
        {
            public NotifyValidator()
            {
                RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
                RuleFor(x => x.Message).NotEmpty().WithMessage("Message is required.");
            }
        }

        [TestMethod]
        public void Mediator_RegionValidater_Tests()
        {
            var services = new ServiceCollection();
            MediatorServiceCollection.RegionValidater(services, [typeof(NotifyValidator)]);
            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetService<IValidator<Notify>>();
            Assert.IsNotNull(validator);
        }



        public class SampleRequest : IRequest<string>
        {
            public string Message { get; set; }
            public SampleRequest(string message)
            {
                Message = message;
            }
        }

        public class SampleRequestHanderler : IRequestHandler<SampleRequest, string>
        {
            public Task<string> Handle(SampleRequest request, CancellationToken cancellationToken)
                => Task.FromResult($"Handled: {request.Message}");
        }
        public class FalseRequestValidator : AbstractValidator<SampleRequest>
        {
            public FalseRequestValidator()
            {
                RuleFor(x => x.Message)
                    .NotEmpty().WithMessage("Message must be 'ValidMessage'.");
            }
        }

        [TestMethod]
        public  void Mediator_RequestValidation_FailsForInvalidRequest()
        {
            // Arrange
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
          //  serviceCollection.AddLogging();
            serviceCollection.AddSHMediator(typeof(FalseRequestValidator));
            var mediator = serviceCollection.BuildServiceProvider().GetRequiredService<IMediator>();

            var invalidRequest = new SampleRequest("");
            // Act & Assert
             Assert.ThrowsAsync<MediatorValidationException>(async () =>
            {
                await mediator.SendAsync(invalidRequest);
            }).Wait();
        }

        [TestMethod]
        public void Mediator_RequestValidation_Success()
        {
            // Arrange
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            //  serviceCollection.AddLogging();
            serviceCollection.AddSHMediator(typeof(FalseRequestValidator));
            var mediator = serviceCollection.BuildServiceProvider().GetRequiredService<IMediator>();

            var validRequest = new SampleRequest("ValidMessage");
            // Act & Assert
            var response =  mediator.SendAsync(validRequest).Result;

            Assert.Contains("Handled", response);
        }

    }
}
