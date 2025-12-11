using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware
{
	public class BasicGetOptions
	{
		public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> AutoAckFunc { get; internal set; }
		public Action<IPipeContext, BasicGetResult> PostExecutionAction { get; set; }
		public Func<IPipeContext, string> QueueNameFunc { get; internal set; }
	}

	public class BasicGetMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, IChannel> ChannelFunc;
		protected  Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, bool> AutoAckFunc;
		protected Action<IPipeContext, BasicGetResult> PostExecutionAction;

		public BasicGetMiddleware(BasicGetOptions options = null)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetGetConfiguration()?.QueueName);
			AutoAckFunc = options?.AutoAckFunc ?? (context => context.GetGetConfiguration()?.AutoAck ?? false);
			PostExecutionAction = options?.PostExecutionAction;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = GetChannel(context);
			var queueNamme = GetQueueName(context);
			var autoAck = GetAutoAck(context);
			var getResult = await PerformBasicGetAsync(channel, queueNamme, autoAck);
			context.Properties.TryAdd(GetPipeExtensions.BasicGetResult, getResult);
			PostExecutionAction?.Invoke(context, getResult);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Task<BasicGetResult> PerformBasicGetAsync(IChannel channel, string queueName, bool autoAck)
		{
			return channel.BasicGetAsync(queueName, autoAck);
		}

		protected virtual bool GetAutoAck(IPipeContext context)
		{
			return AutoAckFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return QueueNameFunc(context);
		}

		protected virtual IChannel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}
	}
}
