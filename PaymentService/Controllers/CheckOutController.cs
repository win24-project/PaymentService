using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using static System.Net.WebRequestMethods;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;

namespace PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckOutController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public CheckOutController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Authorize]
        public IActionResult Create(string priceId, string mode)
        {
            var secretKey = _configuration["StripeSecretKey"];

            StripeConfiguration.ApiKey = secretKey ?? throw new ArgumentNullException("StripeSecretKey is not configured.");

            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst("email")?.Value;
            if (accountId == null || email == null)
            {
                return Unauthorized("User information is missing.");
            }

            string? StripecustomerId = null;


            var options = new CheckoutSessionCreateOptions
            {

                Mode = mode,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId, 
                        Quantity = 1,
                    },
                },
                SuccessUrl = "https://happy-mud-02a876a03.2.azurestaticapps.net/success",
                CancelUrl = "https://happy-mud-02a876a03.2.azurestaticapps.net/cancel",

                ClientReferenceId = accountId,
                Customer = StripecustomerId,
                CustomerEmail = email,
                Metadata = new Dictionary<string, string>
                {
                    { "account_id", accountId },
                }
            };

            var service = new CheckoutSessionService();
            CheckoutSession session = service.Create(options);

            return Ok(new { url = session.Url, sessionId = session.Id } );
        }
    }
}
