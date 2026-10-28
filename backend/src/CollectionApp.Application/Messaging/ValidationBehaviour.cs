using FluentValidation;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CollectionApp.Application.Messaging
{
    /// <summary>
    /// Represents the validation behaviour middleware.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationBehaviour{TRequest,TResponse}"/> class.
        /// </summary>
        /// <param name="validators">The validator for the current request type.</param>
        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var errors = _validators
                .Select(v => v.Validate(request))
                .SelectMany(v => v.Errors)
                .Where(v => v is not null)
                .Distinct()
                .ToList();

            if (errors.Any())
                throw new ValidationException(errors);


            return await next();

        }
    }
}
