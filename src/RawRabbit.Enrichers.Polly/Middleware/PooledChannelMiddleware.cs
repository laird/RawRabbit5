using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class PooledChannelMiddleware : Pipe.Middleware.PooledChannelMiddleware
	{
		public PooledChannelMiddleware(IChannelPoolFactory poolFactory, PooledChannelOptions? options = null)
			: base(poolFactory, options) { }

		protected override Task<IModel> GetChannelAsync(IPipeContext context, CancellationToken token)
		{
			var pipeline = context.GetPolicy(PolicyKeys.ChannelCreate);
			if (pipeline == null)
			{
				return base.GetChannelAsync(context, token);
			}

			return pipeline.ExecuteAsync(
				async ct => await base.GetChannelAsync(context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
