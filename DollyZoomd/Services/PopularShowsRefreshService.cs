using DollyZoomd.Options;
using DollyZoomd.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DollyZoomd.Services;

public class PopularShowsRefreshService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DiscoverOptions> discoverOptions,
    ILogger<PopularShowsRefreshService> logger) : BackgroundService
{
    private readonly int _checkIntervalHours = Math.Max(1, discoverOptions.Value.PopularRefreshCheckHours);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CheckAndRefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_checkIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAndRefreshAsync(stoppingToken);
        }
    }

    private async Task CheckAndRefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var discoverService = scope.ServiceProvider.GetRequiredService<IDiscoverService>();
            await discoverService.EnsurePopularShowsFreshAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background refresh for popular shows failed.");
        }
    }
}