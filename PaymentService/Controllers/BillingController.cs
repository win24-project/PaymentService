using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BillingController : ControllerBase
    {

        private readonly IConfiguration _cfg;
        private readonly HttpClient _auth;

        public BillingController(IConfiguration configuration, IHttpClientFactory factory)
        {
            _cfg = configuration;
            _auth = factory.CreateClient("Auth");
        }

        [HttpPost("portal")]
        public async Task<IActionResult> CreatePortalSession()
        {

            Stripe.StripeConfiguration.ApiKey = _cfg["StripeSecretKey"];

            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (accountId == null || email == null)
            {
                return Unauthorized("User information is missing.");
            }

            var map = await _auth.GetFromJsonAsync<UserStripeModel>($"/profile/{accountId}");
            if (map == null || string.IsNullOrWhiteSpace(map.customerId))
            {
                return BadRequest("No customer ID found for user.");
            }

            var portal = new Stripe.BillingPortal.SessionService();
            var session = await portal.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = map.customerId,
                ReturnUrl = "https://happy-mud-02a876a03.2.azurestaticapps.net"
            });

            return Ok(new { url = session.Url });
        }
    }
}
