using CollectionApp.Domain.Utils;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CollectionApp.Application.Messaging
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Add messaging module
        /// </summary>
        /// <param name="services">An IServiceCollection</param>
        /// <param name="assemblies">A list of Assemblies</param>
        /// <returns>An IServiceCollection</returns>
        public static IServiceCollection AddMessaging(this IServiceCollection services, List<Assembly> assemblies) {

            if (assemblies != null && assemblies.Count > 0)
            {
                //MediaR
                services.AddMediatR(configuration =>
                {
                    configuration.RegisterServicesFromAssemblies(assemblies.ToArray());
                    configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
                });

                //Fluent Validation
                services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);
            }
            

            return services;
        }

        /// <summary>
        /// Specifies a custom error to use if validation fails.
        /// </summary>
        /// <typeparam name="T">The type being validated.</typeparam>
        /// <typeparam name="TProperty">The property being validated.</typeparam>
        /// <param name="rule">The current rule.</param>
        /// <param name="error">The error to use.</param>
        /// <returns>The same rule builder.</returns>
        public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty> rule, Error error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error), "The error is required");
            }

            return rule.WithErrorCode(error.Code).WithMessage(error.Message);
        }
    }
}
