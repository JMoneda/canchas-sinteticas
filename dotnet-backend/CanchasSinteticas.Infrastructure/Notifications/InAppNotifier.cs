using CanchasSinteticas.Application.Abstractions;

namespace CanchasSinteticas.Infrastructure.Notifications;

/// <summary>
/// Notificador del canal "app". En el MVP el estado se refleja en la aplicación (consulta/polling del
/// pago); este notificador deja además una traza del evento. Los canales de correo y WhatsApp/SMS se
/// añaden como implementaciones adicionales activadas por configuración.
/// </summary>
public class InAppNotifier : INotificationSender
{
    /// <inheritdoc/>
    public Task NotifyAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        // El canal app se materializa en la UI (estado del pago + comprobante). Se registra el evento
        // para trazabilidad; un centro de notificaciones in-app queda fuera del alcance del MVP.
        Console.WriteLine(
            $"[notif:app] user={notification.UserId} kind={notification.Kind} " +
            $"reservation={notification.ReservationId} amount={notification.Amount} ref={notification.ReceiptReference}");
        return Task.CompletedTask;
    }
}
