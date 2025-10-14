using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware
	{
		public TransientChannelMiddleware(IChannelFactory factory)
			: base(factory) { }

		protected override Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken token)
		{
			var pipeline = context.GetPolicy(PolicyKeys.ChannelCreate);
			if (pipeline == null)
			{
				return base.CreateChannelAsync(context, token);
			}

			return pipeline.ExecuteAsync(
				async ct => await base.CreateChannelAsync(context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
