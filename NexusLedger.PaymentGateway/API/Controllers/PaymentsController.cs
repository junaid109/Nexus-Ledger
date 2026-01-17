using NexusLedger.PaymentGateway.Domain.Models;
using NexusLedger.PaymentGateway.Infrastructure.Idempotency;
using Microsoft.AspNetCore.Mvc;

namespace NexusLedger.PaymentGateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ILogger<PaymentsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public IActionResult ProcessPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation("Received payment request from {FromAccount} to {ToAccount}", request.FromAccount, request.ToAccount);

        // TODO: Publish to Kafka
        
        var response = new PaymentResponse(
            TransactionId: Guid.NewGuid(),
            Status: "Pending",
            Timestamp: DateTime.UtcNow
        );

        return Accepted(response);
    }
}
