using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Infrastructure.Payments;

namespace CanchasSinteticas.Tests.Infrastructure;

/// <summary>Pruebas del verificador de webhook de Wompi (autenticidad y fail-closed).</summary>
public class WompiWebhookVerifierTests
{
    private readonly WompiSignatureVerifier signature = new();

    private static PaymentsOptions Options(string eventsSecret) =>
        new() { Wompi = new WompiOptions { EventsSecret = eventsSecret } };

    private string BuildBody(string txId, string status, long amountInCents, string timestamp, string checksum) =>
        $$"""
        {
          "event": "transaction.updated",
          "data": { "transaction": { "id": "{{txId}}", "status": "{{status}}", "reference": "pay-1", "amount_in_cents": {{amountInCents}} } },
          "timestamp": {{timestamp}},
          "signature": {
            "properties": ["data.transaction.id", "data.transaction.status", "data.transaction.amount_in_cents"],
            "checksum": "{{checksum}}"
          }
        }
        """;

    [Fact]
    public void Con_secreto_y_checksum_correcto_parsea_el_evento()
    {
        const string secret = "events_secret";
        var checksum = signature.ComputeChecksum(["TX1", "APPROVED", "12000000"], "1753370590", secret);
        var body = BuildBody("TX1", "APPROVED", 12000000, "1753370590", checksum);

        var evt = new WompiWebhookVerifier(Options(secret), signature).VerifyAndParse(body);

        Assert.NotNull(evt);
        Assert.Equal(PaymentWebhookStatus.Approved, evt!.Status);
        Assert.Equal("pay-1", evt.Reference);
    }

    [Fact]
    public void Sin_secreto_configurado_no_acepta_ningun_evento()
    {
        // Aunque el checksum "cuadre" con secreto vacío, se rechaza por fallar cerrado.
        var checksum = signature.ComputeChecksum(["TX1", "APPROVED", "12000000"], "1", string.Empty);
        var body = BuildBody("TX1", "APPROVED", 12000000, "1", checksum);

        var evt = new WompiWebhookVerifier(Options(string.Empty), signature).VerifyAndParse(body);

        Assert.Null(evt);
    }

    [Fact]
    public void Checksum_invalido_devuelve_null()
    {
        var body = BuildBody("TX1", "APPROVED", 12000000, "1753370590", "deadbeef");

        var evt = new WompiWebhookVerifier(Options("events_secret"), signature).VerifyAndParse(body);

        Assert.Null(evt);
    }
}
