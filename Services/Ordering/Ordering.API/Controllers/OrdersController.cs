using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Services.Ordering.API.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IOrderQueries _orderQueries;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IMediator mediator, IOrderQueries orderQueries, ILogger<OrdersController> logger)
        {
            _mediator = mediator;
            _orderQueries = orderQueries;
            _logger = logger;
        }

        [HttpPut("{orderId}/complete")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> CompleteOrderAsync(int orderId)
        {
            _logger.LogInformation(
                "Completing order {OrderId} requested by {UserId}", 
                orderId, 
                User.FindFirst("sub")?.Value);

            if (orderId <= 0)
            {
                _logger.LogWarning("Invalid orderId: {OrderId}", orderId);
                return BadRequest(new { error = "Invalid order id" });
            }

            try
            {
                var command = new CompleteOrderCommand(orderId);
                var result = await _mediator.Send(command);
                
                if (!result)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound();
                }

                return Ok();
            }
            catch (OrderingDomainException ex)
            {
                _logger.LogError(ex, "Domain error completing order {OrderId}: {Message}", orderId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error completing order {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
} 