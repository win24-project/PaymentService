using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Configuration.AddAzureKeyVault(new Uri("https://group-project-keyvault.vault.azure.net/"), new DefaultAzureCredential());



var app = builder.Build();

var cfg = app.Services.GetRequiredService<IConfiguration>();
var stripeSecretKey = cfg["StripeSecretKey"];
Stripe.StripeConfiguration.ApiKey = stripeSecretKey;


app.UseHttpsRedirection();
app.MapControllers();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentService API");
    c.RoutePrefix = string.Empty;
});
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();


app.Run();
