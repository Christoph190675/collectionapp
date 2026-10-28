using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace CollectionApp.Api.Http.Configurations
{
    public class CustomRateLimitConfiguration : RateLimitConfiguration
    {
        public CustomRateLimitConfiguration(IOptions<IpRateLimitOptions> ipOptions, IOptions<ClientRateLimitOptions> clientOptions) : base(ipOptions, clientOptions) { }

        public override ICounterKeyBuilder EndpointCounterKeyBuilder { get; } = new EndpointCounterKeyBuilder();
    }
}