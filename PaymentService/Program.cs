using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Configuration.AddAzureKeyVault(new Uri("https://group-project-keyvault.vault.azure.net/"), new DefaultAzureCredential());

var stripeSecretKey = builder.Configuration["StripeSecretKey"];


var app = builder.Build();


Stripe.StripeConfiguration.ApiKey = stripeSecretKey;


app.MapControllers();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());


app.Urls.Add("https://group-project-paymentservice-eyh8h2ewfqhvgddc.swedencentral-01.azurewebsites.net/");

app.Run();
