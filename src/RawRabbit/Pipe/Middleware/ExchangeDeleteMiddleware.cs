using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeleteOptions
	{
		public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
		public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
		public Func<IPipeContext, bool> IfUsedFunc { get; set; }
	}

	public class ExchangeDeleteMiddleware : Middleware
	{
		protected Func<IPipeContext, IChannel> ChannelFunc;
		protected Func<IPipeContext, string> ExchangeNameFunc;
		protected Func<IPipeContext, bool> IfUsedFunc;

		public ExchangeDeleteMiddleware(ExchangeDeleteOptions options)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (context => string.Empty);
			IfUsedFunc = options?.IfUsedFunc ?? (context => false);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var channel = GetChannel(context);
			var exchangeName = GetExchangeName(context);
			var ifUsed = GetIfUsed(context);
			DeleteEchange(channel, exchangeName, ifUsed);
			await Next.InvokeAsync(context, token);
		}

		protected virtual void DeleteEchange(IChannel channel, string exchangeName, bool ifUsed)
		{
			channel.ExchangeDeleteAsync(exchangeName, ifUsed);
		}

		protected virtual IChannel GetChannel(IPipeContext context)
		{
			return ChannelFunc?.Invoke(context);
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			return ExchangeNameFunc?.Invoke(context);
		}

		protected virtual bool GetIfUsed(IPipeContext context)
		{
			return IfUsedFunc.Invoke(context);
		}
	}
}

