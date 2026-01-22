using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator.Tests
{
    [TestClass]
    public class MediatorServiceCollectionTests
    {
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
            public Task<string> Handle(SampleRequest request)
            {
                return Task.FromResult($"Handled: {request.Message}");
            }
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
        public async Task Mediator_RequestValidation_FailsForInvalidRequest()
        {
            // Arrange
            ServiceCollection serviceCollection = new ServiceCollection();
          //  serviceCollection.AddLogging();
            serviceCollection.AddSHMediator(typeof(FalseRequestValidator));
            var mediator = serviceCollection.BuildServiceProvider().GetRequiredService<IMediator>();

            var invalidRequest = new SampleRequest(null);
            // Act & Assert
            await Assert.ThrowsAsync<MediatorValidationException>(async () =>
            {
                await mediator.Send(invalidRequest);
            });
        }

        [TestMethod]
        public async Task Mediator_RequestValidation_Success()
        {
            // Arrange
            ServiceCollection serviceCollection = new ServiceCollection();
            //  serviceCollection.AddLogging();
            serviceCollection.AddSHMediator(typeof(FalseRequestValidator));
            var mediator = serviceCollection.BuildServiceProvider().GetRequiredService<IMediator>();

            var validRequest = new SampleRequest("ValidMessage");
            // Act & Assert
            var response = await mediator.Send(validRequest);
            
            Assert.IsTrue(response.Contains("Handled"));
        }

    }
}
