using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;


namespace PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : Controller
    {

        private readonly IConfiguration _configuration;
        public WebhookController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["stripe-signature"];
            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            Event ev;
            try
            {
                ev = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    webhookSecret
                );
            }
            catch (StripeException e)
            {
                return BadRequest(e.Message);
            }

            if(ev.Type == "checkout.session.completed")
            {
                var sessiom = (Stripe.Checkout.Session)ev.Data.Object;
            }
            return Ok();
        }
    }
}
