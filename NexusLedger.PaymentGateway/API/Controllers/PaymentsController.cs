using NexusLedger.PaymentGateway.Domain.Models;
using NexusLedger.PaymentGateway.Domain.Events;
using NexusLedger.PaymentGateway.Infrastructure.Idempotency;
using Microsoft.AspNetCore.Mvc;
using Confluent.Kafka;

namespace NexusLedger.PaymentGateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly IProducer<string, string> _producer;

    public PaymentsController(ILogger<PaymentsController> logger, IProducer<string, string> producer)
    {
        _logger = logger;
        _producer = producer;
    }

    [HttpPost]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation("Received payment request from {FromAccount} to {ToAccount}", request.FromAccount, request.ToAccount);

        var transactionId = Guid.NewGuid();
        var paymentEvent = new PaymentInitiated
        {
            TransactionId = transactionId,
            Amount = request.Amount,
            Currency = request.Currency,
            FromAccount = request.FromAccount.ToString(),
            ToAccount = request.ToAccount.ToString(),
            Timestamp = DateTime.UtcNow
        };

        var message = new Message<string, string>
        {
            Key = transactionId.ToString(),
            Value = System.Text.Json.JsonSerializer.Serialize(paymentEvent)
        };

        await _producer.ProduceAsync("payments-topic", message);

        _logger.LogInformation("PaymentInitiated event published for TransactionId: {TransactionId}", transactionId);

        var response = new PaymentResponse(
            TransactionId: transactionId,
            Status: "Pending",
            Timestamp: paymentEvent.Timestamp
        );

        return Accepted(response);
    }
}
