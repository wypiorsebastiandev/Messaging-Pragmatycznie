using TicketFlow.Services.SystemMetrics.Core;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.ExposeApiForFrontend();
app.UseAnomalyEndpoints();
app.ExposeLiveMetrics();

app.Run();