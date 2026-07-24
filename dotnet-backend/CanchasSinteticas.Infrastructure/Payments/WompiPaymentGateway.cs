using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Infrastructure.Payments;

/// <summary>
/// Implementación de <see cref="IPaymentGateway"/> sobre Wompi (Bancolombia). Usa el Web Checkout
/// (redirección con firma de integridad) para cobrar y la API REST para consultar el estado y
/// solicitar reembolsos. El estado autoritativo se recibe por webhook, no por la respuesta síncrona.
/// </summary>
public class WompiPaymentGateway(HttpClient httpClient) : IPaymentGateway
{
    private const string Currency = "COP";
    private const string CheckoutBaseUrl = "https://checkout.wompi.co/p/";

    /// <inheritdoc/>
    public Task<GatewayTransactionResult> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        // Wompi Web Checkout: el cobro se correlaciona por nuestra 'reference' (el PaymentId).
        // La firma de integridad evita que se altere el monto/referencia en la URL.
        var amountInCents = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero);
        var integrity = Sha256Hex($"{request.Reference}{amountInCents}{Currency}{request.Credentials.IntegritySecret}");

        var query = new List<string>
        {
            $"public-key={Uri.EscapeDataString(request.Credentials.PublicKey)}",
            $"currency={Currency}",
            $"amount-in-cents={amountInCents}",
            $"reference={Uri.EscapeDataString(request.Reference)}",
            $"signature:integrity={integrity}",
        };

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
            query.Add($"redirect-url={Uri.EscapeDataString(request.ReturnUrl)}");
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            query.Add($"customer-data:email={Uri.EscapeDataString(request.CustomerEmail)}");

        var checkoutUrl = $"{CheckoutBaseUrl}?{string.Join('&', query)}";

        // Aún no existe id de transacción de Wompi (se conoce al confirmar por webhook);
        // usamos nuestra referencia como correlación.
        return Task.FromResult(new GatewayTransactionResult(
            TransactionId: request.Reference,
            RawStatus: "PENDING",
            Reference: request.Reference,
            CheckoutUrl: checkoutUrl));
    }

    /// <inheritdoc/>
    public async Task<GatewayTransactionResult> GetTransactionAsync(string transactionId, PaymentGatewayCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{credentials.BaseUrl}/transactions/{transactionId}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.PrivateKey);

            using var res = await httpClient.SendAsync(req, cancellationToken);
            res.EnsureSuccessStatusCode();

            using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var data = doc.RootElement.GetProperty("data");

            var status = data.GetProperty("status").GetString() ?? "UNKNOWN";
            var reference = data.TryGetProperty("reference", out var r) ? r.GetString() : null;

            return new GatewayTransactionResult(transactionId, status, reference, null);
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentGatewayError($"No se pudo consultar la transacción en el proveedor: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<GatewayRefundResult> RefundAsync(string transactionId, decimal amount, PaymentGatewayCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{credentials.BaseUrl}/refunds");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.PrivateKey);
            req.Content = JsonContent.Create(new
            {
                transaction_id = transactionId,
                amount_in_cents = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero),
            });

            using var res = await httpClient.SendAsync(req, cancellationToken);
            res.EnsureSuccessStatusCode();

            using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var data = doc.RootElement.GetProperty("data");

            var refundId = data.TryGetProperty("id", out var id) ? id.GetString() : transactionId;
            var status = data.TryGetProperty("status", out var s) ? s.GetString() : "PENDING";

            return new GatewayRefundResult(refundId ?? transactionId, status ?? "PENDING");
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentGatewayError($"No se pudo solicitar el reembolso en el proveedor: {ex.Message}");
        }
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
