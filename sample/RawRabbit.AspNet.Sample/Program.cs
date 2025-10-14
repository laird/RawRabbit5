using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RawRabbit.AspNet.Sample;

var builder = WebApplication.CreateBuilder(args);

// Configure services
var startup = new Startup(builder.Environment, builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure middleware
startup.Configure(app, app.Environment);

app.Run();
