using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas de generación y control de acceso a comprobantes (US3, T045).</summary>
public class ReceiptAccessTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void El_titular_y_el_dueno_pueden_ver_el_comprobante_un_tercero_no()
    {
        var w = new TestWorld();
        var (_, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing("TX", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        payment.MarkApproved("TX", "REF-1", Now);
        w.Payments.Update(payment);

        var service = w.BuildReceiptService(new FixedClock(Now));
        service.GenerateFor(payment);

        // Titular (client1) y dueño (owner1) acceden; un tercero recibe 403.
        var (receiptClient, pdfClient) = service.GetReservationReceipt("client1", "res1");
        Assert.Equal("REF-1", receiptClient.GatewayReference);
        Assert.NotEmpty(pdfClient);

        var (_, _) = service.GetReservationReceipt("owner1", "res1");

        Assert.Throws<NotAuthorizedError>(() => service.GetReservationReceipt("intruso", "res1"));
    }

    [Fact]
    public void Sin_pago_aprobado_no_hay_comprobante()
    {
        var w = new TestWorld();
        w.SeedPendingReservation(Now); // pago pendiente, sin comprobante
        var service = w.BuildReceiptService(new FixedClock(Now));

        Assert.Throws<NotFoundError>(() => service.GetReservationReceipt("client1", "res1"));
    }

    [Fact]
    public void GenerateFor_es_idempotente_por_pago()
    {
        var w = new TestWorld();
        var (_, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing("TX", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        payment.MarkApproved("TX", "REF-1", Now);
        w.Payments.Update(payment);
        var service = w.BuildReceiptService(new FixedClock(Now));

        var first = service.GenerateFor(payment);
        var second = service.GenerateFor(payment);

        Assert.Equal(first.Id, second.Id);
    }
}
