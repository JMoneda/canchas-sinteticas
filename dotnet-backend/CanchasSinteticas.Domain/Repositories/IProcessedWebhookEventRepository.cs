using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Persistencia de eventos de webhook procesados, para garantizar idempotencia.</summary>
public interface IProcessedWebhookEventRepository
{
    /// <summary>Indica si el evento indicado ya fue procesado.</summary>
    bool Exists(string eventId);

    /// <summary>Registra un evento como procesado.</summary>
    void Add(ProcessedWebhookEvent processedEvent);
}
