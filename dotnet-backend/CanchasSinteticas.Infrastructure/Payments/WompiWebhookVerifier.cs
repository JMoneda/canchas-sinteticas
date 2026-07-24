using System.Text.Json;
using CanchasSinteticas.Application.Abstractions;

namespace CanchasSinteticas.Infrastructure.Payments;

/// <summary>
/// Verifica y traduce los eventos de webhook de Wompi al evento normalizado de la aplicación.
/// Comprueba el checksum con el <c>events secret</c>; si no coincide, devuelve null (FR-005).
/// </summary>
public class WompiWebhookVerifier(PaymentsOptions options, WompiSignatureVerifier signature) : IPaymentWebhookVerifier
{
    /// <inheritdoc/>
    public PaymentWebhookEvent? VerifyAndParse(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("signature", out var signatureEl)
                || !signatureEl.TryGetProperty("properties", out var propsEl)
                || !signatureEl.TryGetProperty("checksum", out var checksumEl))
            {
                return null;
            }

            var timestamp = root.TryGetProperty("timestamp", out var tsEl)
                ? tsEl.ToString()
                : string.Empty;

            var values = new List<string>();
            foreach (var prop in propsEl.EnumerateArray())
            {
                var path = prop.GetString();
                if (path is null || !TryResolve(root, path, out var value))
                    return null;

                values.Add(value);
            }

            var checksum = checksumEl.GetString();
            if (!signature.Verify(values, timestamp, options.Wompi.EventsSecret, checksum))
                return null;

            var transaction = root.GetProperty("data").GetProperty("transaction");
            var txId = transaction.GetProperty("id").ToString();
            var reference = transaction.TryGetProperty("reference", out var refEl) ? refEl.GetString() ?? string.Empty : string.Empty;
            var rawStatus = transaction.GetProperty("status").GetString() ?? "UNKNOWN";

            return new PaymentWebhookEvent(
                EventId: checksum ?? txId,
                TransactionId: txId,
                Reference: reference,
                Status: MapStatus(rawStatus),
                RawStatus: rawStatus);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PaymentWebhookStatus MapStatus(string rawStatus) => rawStatus.ToUpperInvariant() switch
    {
        "APPROVED" => PaymentWebhookStatus.Approved,
        "DECLINED" => PaymentWebhookStatus.Declined,
        "VOIDED" => PaymentWebhookStatus.Voided,
        "ERROR" => PaymentWebhookStatus.Error,
        _ => PaymentWebhookStatus.Pending,
    };

    private static bool TryResolve(JsonElement root, string path, out string value)
    {
        value = string.Empty;
        var current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return false;
        }

        value = current.ToString();
        return true;
    }
}
