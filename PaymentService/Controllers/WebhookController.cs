using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;


namespace PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly HttpClient _auth;
        public WebhookController(IConfiguration configuration, IHttpClientFactory factory)
        {
            _configuration = configuration;
            _auth = factory.CreateClient("Auth");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["stripe-signature"];
            var webhookSecret = _configuration["StripeWebhookSecret"];

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    payload, signature, webhookSecret
                );
            }
            catch (Exception ex)
            {
                return BadRequest($"Webhook Error: {ex.Message}");
            }
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    {
                        var s = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                        s.Metadata.TryGetValue("account_Id", out var accountId);
                        var dto = new
                        {
                            accountId = accountId,
                            customerId = s.Customer,
                            subscriptionStatus = "active",

                        };

                        await _auth.PostAsJsonAsync("/profile/add-subscription", dto);
                        break;
                    }
                case "invoice.payment_succeeded":
                    {
                        var inv = (Invoice)stripeEvent.Data.Object;

                        //var periodEnd = inv.PeriodEnd == default ? DateTime.UtcNow : inv.PeriodEnd;

                        var dto = new
                        {
                            customerId = inv.CustomerId,
                            //currentPeriodEnd = periodEnd,
                            subscriptionStatus = "active"
                        };

                        await _auth.PostAsJsonAsync("/api/", dto);
                        break;
                    }

                case "invoice.payment_failed":
                    {
                        var inv = (Invoice)stripeEvent.Data.Object;
                        var dto = new
                        {
                            customerId = inv.CustomerId,
                            subscriptionStatus = "past_due"
                        };
                        await _auth.PostAsJsonAsync("/api/", dto);
                        break;
                    }

            }
            return Ok();
        }
    }
}

