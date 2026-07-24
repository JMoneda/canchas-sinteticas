using CanchasSinteticas.Application.Services;

namespace CanchasSinteticas.Api.BackgroundJobs;

/// <summary>
/// Servicio en segundo plano (en proceso) que expira periódicamente los pagos vencidos y libera sus
/// franjas. No es una cola de mensajes: es un temporizador dentro del mismo proceso.
/// </summary>
public class PaymentExpirySweeper(IServiceScopeFactory scopeFactory, ILogger<PaymentExpirySweeper> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested
            && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PaymentExpiryService>();
                var expired = service.SweepExpired();
                if (expired > 0)
                    logger.LogInformation("Se expiraron {Count} pagos vencidos y se liberaron sus franjas.", expired);

                var settlement = scope.ServiceProvider.GetRequiredService<MatchSettlementService>();
                var settled = await settlement.SweepAsync();
                if (settled > 0)
                    logger.LogInformation("Se liquidaron {Count} partidos con recaudo incompleto vencido.", settled);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al expirar pagos vencidos.");
            }
        }
    }
}
