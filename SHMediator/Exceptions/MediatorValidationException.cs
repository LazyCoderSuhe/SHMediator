using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator.Exceptions
{
    public class MediatorValidationException: Exception
    {
        public IEnumerable<string> Errors { get; private set; } = Array.Empty<string>();
        public MediatorValidationException(IEnumerable<ValidationFailure> errors) 
        {
            Errors = errors.Select(x => x.ErrorMessage);
        }
    }
}
