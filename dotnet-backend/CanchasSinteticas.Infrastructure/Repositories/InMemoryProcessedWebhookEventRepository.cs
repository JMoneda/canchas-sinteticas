using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio en memoria de eventos de webhook procesados.</summary>
public class InMemoryProcessedWebhookEventRepository(InMemoryDatabase db) : IProcessedWebhookEventRepository
{
    /// <inheritdoc/>
    public bool Exists(string eventId) => db.ProcessedWebhookEvents.ContainsKey(eventId);

    /// <inheritdoc/>
    public void Add(ProcessedWebhookEvent processedEvent) =>
        db.ProcessedWebhookEvents[processedEvent.EventId] = processedEvent;
}
