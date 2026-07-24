using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Pagos: recepción de eventos del proveedor y consulta de estado.</summary>
[Route("api/payments")]
public class PaymentsController(
    PaymentWebhookService webhooks,
    PaymentService payments) : ApiControllerBase
{
    /// <summary>
    /// Recibe los eventos del proveedor de pagos. Es público (sin JWT): su autenticidad se valida por
    /// la firma del evento. Responde 200 siempre que el evento se reciba, sin exponer detalles internos.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        var accepted = await webhooks.ProcessAsync(rawBody);
        return accepted ? Ok(new { received = true }) : Ok(new { received = false });
    }

    /// <summary>Consulta el estado de un pago (titular o dueño de la sede).</summary>
    [HttpGet("{paymentId}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentStatusOutput), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetStatus(string paymentId) =>
        Ok(payments.GetStatus(CurrentUserId, paymentId));
}
