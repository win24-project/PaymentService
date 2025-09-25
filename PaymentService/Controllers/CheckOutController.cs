using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
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
        public IActionResult Create(string priceId, string mode)
        {
            var secretKey = _configuration["StripeSecretKey"];

            StripeConfiguration.ApiKey = secretKey ?? throw new ArgumentNullException("Stripe:SecretKey is not configured.");

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
            };
            var service = new CheckoutSessionService();
            CheckoutSession session = service.Create(options);

            return Ok(new { url = session.Url, sessionId = session.Id } );
        }
    }
}
