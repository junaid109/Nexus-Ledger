using NexusLedger.PaymentGateway.Domain.Events;

namespace NexusLedger.PaymentGateway.Domain.Interfaces;

public interface IPaymentEventProducer
{
    Task PublishPaymentInitiatedAsync(PaymentInitiatedEvent @event);
}
