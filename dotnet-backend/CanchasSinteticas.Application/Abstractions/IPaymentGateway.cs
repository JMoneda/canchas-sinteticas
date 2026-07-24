using CanchasSinteticas.Domain.Enums;

namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Abstracción del proveedor de pagos. Aísla la lógica de negocio del proveedor concreto
/// (Wompi, PayU, etc.), de modo que se pueda cambiar o añadir otro sin tocar Domain/Application.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Crea una transacción de cobro y devuelve la información de checkout.</summary>
    Task<GatewayTransactionResult> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Consulta el estado actual de una transacción en el proveedor.</summary>
    Task<GatewayTransactionResult> GetTransactionAsync(string transactionId, PaymentGatewayCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>Solicita el reembolso (total) de una transacción aprobada.</summary>
    Task<GatewayRefundResult> RefundAsync(string transactionId, decimal amount, PaymentGatewayCredentials credentials, CancellationToken cancellationToken = default);
}

/// <summary>Credenciales del proveedor para una sede o para la plataforma.</summary>
public record PaymentGatewayCredentials(
    string BaseUrl,
    string PublicKey,
    string PrivateKey,
    string IntegritySecret,
    string EventsSecret,
    string? MerchantRef);

/// <summary>Datos para crear una transacción de cobro.</summary>
public record CreateTransactionRequest(
    string PaymentId,
    string Reference,
    decimal Amount,
    PaymentMethod Method,
    string? CustomerEmail,
    string? ReturnUrl,
    PaymentGatewayCredentials Credentials);

/// <summary>Resultado de crear/consultar una transacción.</summary>
public record GatewayTransactionResult(
    string TransactionId,
    string RawStatus,
    string? Reference,
    string? CheckoutUrl);

/// <summary>Resultado de una solicitud de reembolso.</summary>
public record GatewayRefundResult(string RefundReference, string RawStatus);
