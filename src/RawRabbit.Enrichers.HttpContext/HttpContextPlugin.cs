using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Instantiation;

namespace RawRabbit
{
	public static class HttpContextPlugin
	{
		public static IClientBuilder UseHttpContext(this IClientBuilder builder)
		{
			builder.Register(
				p => p.Use<AspNetCoreHttpContextMiddleware>(),
				p => p.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>()
			);
			return builder;
		}
	}
}
