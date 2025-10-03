using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;
using System.Globalization;


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
                        var s = (Session)stripeEvent.Data.Object;

                        // metadata key you set when creating the session
                        s.Metadata.TryGetValue("account_id", out var accountId);
                        if (string.IsNullOrWhiteSpace(accountId))
                            accountId = s.ClientReferenceId;

                        var customerId = s.CustomerId;

                        var dto = new
                        {
                            accountId,
                            customerId,
                            subscriptionStatus = "active"

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
                case "customer.subscription.deleted":
                    {
                        var sub = (Subscription)stripeEvent.Data.Object;

                        var dto = new
                        {
                            subscriptionStatus = "canceled"
                        };
                        await _auth.PostAsJsonAsync($"/profile/change-subscription-status?customerId={sub.CustomerId}&subscriptionStatus=canceled", dto);
                        break;
                    }
                case "customer.subscription.updated":
                    {
                        var sub = (Subscription)stripeEvent.Data.Object;

                        List<string> priceId = ["price_1SBYqpPZLXb0VQaIu5bHdpEW", "price_1SBC2qPZLXb0VQaIKiKGmL61", "price_1SBC34PZLXb0VQaIsVYINbGc"];
                        List<string> planNames = ["basic", "standard", "premium"];
                        string planName = planNames[priceId.IndexOf(sub.Items.Data[0].Price.Id)];
                        var dto = new
                        {
                            customerId = sub.CustomerId,
                            membershipPlan = planName
                        };
                        await _auth.PostAsJsonAsync($"/profile/change-membership-plan?customerId={sub.CustomerId}&membershipPlan={planName}", dto);
                        break;
                    }
                case "customer.subscription.created":
                    {
                        var sub = (Subscription)stripeEvent.Data.Object;

                        List<string> priceId = ["price_1SBYqpPZLXb0VQaIu5bHdpEW", "price_1SBC2qPZLXb0VQaIKiKGmL61", "price_1SBC34PZLXb0VQaIsVYINbGc"];
                        List<string> planNames = ["basic", "standard", "premium"];
                        string planName = planNames[priceId.IndexOf(sub.Items.Data[0].Price.Id)];
                        var dto = new
                        {
                            customerId = sub.CustomerId,
                            membershipPlan = planName
                        };
                        await _auth.PostAsJsonAsync($"/profile/change-membership-plan?customerId={sub.CustomerId}&membershipPlan={planName}", dto);
                        break;
                    }

            }
            return Ok();
        }
    }
}

