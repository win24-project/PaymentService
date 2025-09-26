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
            _auth = factory.CreateClient("auth");
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
                        s.Metadata.TryGetValue("accountId", out var accountId);

                        var customerId = s.CustomerId;
                        var subscriptionStatus = "active";

                        var accessToken = Request.Headers["Authorization"].ToString()?.Replace("Bearer", "");
                        var Client = new HttpClient();
                        Client.DefaultRequestHeaders.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        var response = await Client.PostAsync("https://group-project-authservice-ebbpd0c8g2fabqdr.swedencentral-01.azurewebsites.net/change-subscription-status?status=active", new StringContent(""));
       
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

