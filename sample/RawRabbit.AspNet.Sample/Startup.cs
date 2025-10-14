using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RawRabbit.AspNet.Sample.Controllers;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Instantiation;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace RawRabbit.AspNet.Sample
{
	public class Startup
	{
		private readonly string _rootPath;

		public Startup(IWebHostEnvironment env, IConfiguration configuration)
		{
			_rootPath = env.ContentRootPath;
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddRawRabbit(new RawRabbitOptions
					{
						ClientConfiguration = GetRawRabbitConfiguration(),
						Plugins = p => p
							.UseStateMachine()
							.UseGlobalExecutionId()
							.UseHttpContext()
							.UseMessageContext(c =>
							{
								return new MessageContext
								{
									Source = c.GetHttpContext()?.Request.GetDisplayUrl() ?? string.Empty
								};
							})
					})
				.AddControllers();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			Log.Logger = GetConfiguredSerilogger();
			app.UseSerilogRequestLogging();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		private ILogger GetConfiguredSerilogger()
		{
			return new LoggerConfiguration()
				.WriteTo.File($"{_rootPath}/Logs/serilog.log", LogEventLevel.Debug)
				.WriteTo.Console()
				.CreateLogger();
		}

		private RawRabbitConfiguration GetRawRabbitConfiguration()
		{
			var section = Configuration.GetSection("RawRabbit");
			if (!section.GetChildren().Any())
			{
				throw new ArgumentException($"Unable to configuration section 'RawRabbit'. Make sure it exists in the provided configuration");
			}
			return section.Get<RawRabbitConfiguration>() ?? throw new InvalidOperationException("Failed to bind RawRabbitConfiguration");
		}
	}
}
