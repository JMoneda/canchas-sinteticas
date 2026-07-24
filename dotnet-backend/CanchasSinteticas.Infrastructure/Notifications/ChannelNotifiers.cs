using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Infrastructure.Payments;

namespace CanchasSinteticas.Infrastructure.Notifications;

/// <summary>
/// Notificador por correo. Se activa por configuración (<c>Payments:Notifications:Email</c>). En este
/// MVP registra el envío; el adaptador SMTP real se conecta detrás de esta misma clase.
/// </summary>
public class EmailNotifier(PaymentsOptions options) : INotificationSender
{
    /// <inheritdoc/>
    public Task NotifyAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        if (!options.Notifications.Email.Enabled)
            return Task.CompletedTask;

        Console.WriteLine($"[notif:email] user={notification.UserId} kind={notification.Kind} amount={notification.Amount}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Notificador por WhatsApp/SMS. Se activa por configuración
/// (<c>Payments:Notifications:WhatsAppSms</c>). El adaptador de mensajería real se conecta aquí.
/// </summary>
public class WhatsAppSmsNotifier(PaymentsOptions options) : INotificationSender
{
    /// <inheritdoc/>
    public Task NotifyAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        if (!options.Notifications.WhatsAppSms.Enabled)
            return Task.CompletedTask;

        Console.WriteLine($"[notif:whatsapp/sms] user={notification.UserId} kind={notification.Kind} amount={notification.Amount}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Compositor que despacha cada notificación por todos los canales (app + correo + WhatsApp/SMS).
/// Cada canal decide por configuración si actúa (FR-026/FR-026a).
/// </summary>
public class CompositeNotificationSender(
    InAppNotifier inApp,
    EmailNotifier email,
    WhatsAppSmsNotifier whatsappSms) : INotificationSender
{
    private readonly INotificationSender[] channels = [inApp, email, whatsappSms];

    /// <inheritdoc/>
    public async Task NotifyAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        foreach (var channel in channels)
            await channel.NotifyAsync(notification, cancellationToken);
    }
}
