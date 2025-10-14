using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ConsumerCreationMiddleware : Pipe.Middleware.ConsumerCreationMiddleware
	{
		public ConsumerCreationMiddleware(IConsumerFactory consumerFactory, ConsumerCreationOptions? options = null)
			: base(consumerFactory, options) { }

		protected override Task<IBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context, CancellationToken token)
		{
			var pipeline = context.GetPolicy(PolicyKeys.QueueDeclare);
			if (pipeline == null)
			{
				return base.GetOrCreateConsumerAsync(context, token);
			}

			return pipeline.ExecuteAsync(
				async ct => await base.GetOrCreateConsumerAsync(context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
