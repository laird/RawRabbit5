using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class HandlerInvocationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware
	{
		public HandlerInvocationMiddleware(HandlerInvocationOptions? options = null)
			: base(options) { }

		protected override Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var pipeline = context.GetPolicy(PolicyKeys.HandlerInvocation);
			if (pipeline == null)
			{
				return base.InvokeMessageHandler(context, token);
			}

			return pipeline.ExecuteAsync(
				async ct => await base.InvokeMessageHandler(context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
