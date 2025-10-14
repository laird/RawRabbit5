using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class BasicPublishMiddleware : Pipe.Middleware.BasicPublishMiddleware
	{
		public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions? options = null)
			: base(exclusive, options) { }

		protected override void BasicPublish(
				IModel channel,
				string exchange,
				string routingKey,
				bool mandatory,
				IBasicProperties basicProps,
				byte[] body,
				IPipeContext context)
		{
			var pipeline = context.GetPolicy(PolicyKeys.BasicPublish);
			if (pipeline == null)
			{
				base.BasicPublish(channel, exchange, routingKey, mandatory, basicProps, body, context);
				return;
			}

			var policyTask = pipeline.ExecuteAsync(
				async ct =>
				{
					base.BasicPublish(channel, exchange, routingKey, mandatory, basicProps, body, context);
					await Task.CompletedTask;
				},
				CancellationToken.None);
			policyTask.ConfigureAwait(false);
			policyTask.GetAwaiter().GetResult();
		}
	}
}
