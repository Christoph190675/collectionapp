using MediatR;

namespace CollectionApp.Application.Health
{
    /// <summary>
    /// Get the health status of the API and of the database.
    /// </summary>
    public sealed record GetHealthStatusQuery : IRequest<HealthStatusDto>;

    public sealed class GetHealthStatusQueryHandler : IRequestHandler<GetHealthStatusQuery, HealthStatusDto>
    {
        private readonly IDatabaseHealthChecker _databaseHealthChecker;
        private readonly TimeProvider _timeProvider;

        public GetHealthStatusQueryHandler(IDatabaseHealthChecker databaseHealthChecker, TimeProvider timeProvider)
        {
            _databaseHealthChecker = databaseHealthChecker;
            _timeProvider = timeProvider;
        }

        public async Task<HealthStatusDto> Handle(GetHealthStatusQuery request, CancellationToken cancellationToken)
        {
            var healthy = await _databaseHealthChecker.IsHealthyAsync(cancellationToken);

            return new HealthStatusDto(
                HealthStatusDto.Ok,
                healthy ? HealthStatusDto.Ok : HealthStatusDto.Ko,
                _timeProvider.GetUtcNow().UtcDateTime);
        }
    }
}
