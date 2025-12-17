using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using TestSearching;
using TestSearching.Entities;
using TestSearching.Factory;
using TestSearching.Interfaces;
using TestSearching.Processors;
using TestSearching.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var brConfiguration = new BrConfiguration();
var configuration = builder.Configuration;
configuration.GetSection("BrConfiguration").Bind(brConfiguration);

var lambdaConfiguration = new BrConfiguration();
configuration.GetSection("LambdaConfiguration").Bind(lambdaConfiguration);


var services = builder.Services;
services.AddScoped<PlaywrightService>();

services.AddScoped<IHtmlParserService, HtmlParserService>();
services.AddScoped<ISearchFactory<Company>, SearchProcessorFactory>();

services.AddScoped<IScraperService, ScraperService>(sp =>
{
	var playwrightService = sp.GetRequiredService<PlaywrightService>();
	playwrightService.InitializeAsync().GetAwaiter().GetResult();
	var lambdaService = sp.GetRequiredService<ILambdaService>();
	return new ScraperService(playwrightService,lambdaService);
});

services.AddHttpClient<IBcRegistryService, BcRegistryService>((sp, client) =>
{
	client.BaseAddress = new Uri(brConfiguration.BcBaseUri);
	client.DefaultRequestHeaders.Add(Constants.BC_REGISTRY_ACCOUNT_ID_HEADER, brConfiguration.BcAccountId);
	client.DefaultRequestHeaders.Add(Constants.BC_REGISTRY_API_KEY_HEADER, brConfiguration.BcXApiKey);
});

services.AddHttpClient<ILambdaService, LambdaService>((sp, client) =>
{
	client.BaseAddress = new Uri(lambdaConfiguration.BcBaseUri ?? string.Empty);
});

services.AddScoped(sp =>
{
	var scraperService = sp.GetRequiredService<IScraperService>();
	var htmlParserService = sp.GetRequiredService<IHtmlParserService>();

	return new OnSearchProcessor(scraperService, htmlParserService);
});


services.AddScoped(sp =>
{
	var bcRegistryService = sp.GetRequiredService<IBcRegistryService>();
	return new BcSearchProcessor(bcRegistryService);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
