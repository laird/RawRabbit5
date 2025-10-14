using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExchangeDeclareMiddleware : Pipe.Middleware.ExchangeDeclareMiddleware
	{
		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions? options = null)
			: base(topologyProvider, options) { }

		protected override Task DeclareExchangeAsync(ExchangeDeclaration exchange, IPipeContext context, CancellationToken token)
		{
			var pipeline = context.GetPolicy(PolicyKeys.ExchangeDeclare);
			if (pipeline == null)
			{
				return base.DeclareExchangeAsync(exchange, context, token);
			}

			return pipeline.ExecuteAsync(
				async ct => await base.DeclareExchangeAsync(exchange, context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
