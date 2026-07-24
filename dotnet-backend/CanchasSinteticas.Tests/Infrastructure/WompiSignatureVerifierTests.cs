using CanchasSinteticas.Infrastructure.Payments;

namespace CanchasSinteticas.Tests.Infrastructure;

/// <summary>Pruebas de la verificación de firma del webhook (autenticidad, FR-005).</summary>
public class WompiSignatureVerifierTests
{
    private readonly WompiSignatureVerifier verifier = new();

    [Fact]
    public void Verify_true_cuando_el_checksum_coincide()
    {
        var props = new[] { "TX-123", "APPROVED", "12000000" };
        const string timestamp = "1753370590";
        const string secret = "events_test_secret";

        var checksum = verifier.ComputeChecksum(props, timestamp, secret);

        Assert.True(verifier.Verify(props, timestamp, secret, checksum));
    }

    [Fact]
    public void Verify_false_cuando_se_altera_un_valor()
    {
        var props = new[] { "TX-123", "APPROVED", "12000000" };
        const string timestamp = "1753370590";
        const string secret = "events_test_secret";
        var checksum = verifier.ComputeChecksum(props, timestamp, secret);

        // Un atacante cambia el estado a APPROVED pero no puede recomputar la firma sin el secreto.
        var tampered = new[] { "TX-123", "DECLINED", "12000000" };

        Assert.False(verifier.Verify(tampered, timestamp, secret, checksum));
    }

    [Fact]
    public void Verify_false_cuando_el_secreto_es_incorrecto()
    {
        var props = new[] { "TX-123", "APPROVED", "12000000" };
        const string timestamp = "1753370590";
        var checksum = verifier.ComputeChecksum(props, timestamp, "correct_secret");

        Assert.False(verifier.Verify(props, timestamp, "wrong_secret", checksum));
    }

    [Fact]
    public void Verify_false_cuando_no_hay_checksum()
    {
        var props = new[] { "TX-123", "APPROVED" };
        Assert.False(verifier.Verify(props, "1", "secret", null));
    }
}
