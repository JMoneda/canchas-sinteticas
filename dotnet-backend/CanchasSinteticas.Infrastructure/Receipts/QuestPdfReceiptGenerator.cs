using System.Globalization;
using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CanchasSinteticas.Infrastructure.Receipts;

/// <summary>Genera el PDF de un comprobante con QuestPDF.</summary>
public class QuestPdfReceiptGenerator : IReceiptGenerator
{
    static QuestPdfReceiptGenerator()
    {
        // Licencia gratuita Community de QuestPDF.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    public byte[] GeneratePdf(Receipt receipt)
    {
        var money = new CultureInfo("es-CO");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A5);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Comprobante de pago").FontSize(18).SemiBold();
                    col.Item().Text($"N° {receipt.Number}").FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Canchas Sintéticas").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(6);
                    Row(col, "Fecha", receipt.IssuedAt.ToString("yyyy-MM-dd HH:mm"));
                    Row(col, "Sede", receipt.VenueName);
                    Row(col, "Cancha", receipt.CourtName);
                    Row(col, "Pagador", receipt.PayerName);
                    Row(col, "Método", receipt.Method);
                    Row(col, "Referencia", receipt.GatewayReference);
                    if (receipt.MatchId is not null)
                        Row(col, "Tipo", "Parte de partido (pago dividido)");
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Total pagado").SemiBold();
                        r.ConstantItem(140).AlignRight().Text(receipt.Amount.ToString("C0", money)).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Documento generado automáticamente. ").FontColor(Colors.Grey.Medium);
                    t.Span(receipt.GatewayReference).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static void Row(ColumnDescriptor col, string label, string value) =>
        col.Item().Row(r =>
        {
            r.ConstantItem(120).Text(label).FontColor(Colors.Grey.Darken1);
            r.RelativeItem().Text(value);
        });
}
