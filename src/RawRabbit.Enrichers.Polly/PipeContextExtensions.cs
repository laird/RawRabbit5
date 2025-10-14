using Polly;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly
{
	public static class PipeContextExtensions
	{
		public static ResiliencePipeline? GetPolicy(this IPipeContext context, string? policyName = null)
		{
			var fallback = context.Get<ResiliencePipeline>(PolicyKeys.DefaultPolicy);
			return context.Get(policyName, fallback);
		}

		public static TPipeContext UsePolicy<TPipeContext>(this TPipeContext context, ResiliencePipeline policy, string? policyName = null) where TPipeContext : IPipeContext
		{
			policyName = policyName ?? PolicyKeys.DefaultPolicy;
			context.Properties.TryAdd(policyName, policy);
			return context;
		}
	}
}
