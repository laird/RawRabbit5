using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RawRabbit.Operations.Get.Model
{
	public class Ackable<TType> : IDisposable
	{
		public TType Content { get; set; }
		public bool Acknowledged { get; private set; }
		public IEnumerable<ulong> DeliveryTags => DeliveryTagFunc(Content);
		internal readonly IChannel Channel;
		internal readonly Func<TType, ulong[]> DeliveryTagFunc;

		public Ackable(TType content, IChannel channel, params ulong[] deliveryTag) : this(content, channel, type => deliveryTag)
		{ }

		public Ackable(TType content, IChannel channel, Func<TType, ulong[]> deliveryTagFunc)
		{
			Content = content;
			Channel = channel;
			DeliveryTagFunc = deliveryTagFunc;
		}

		public async Task AckAsync()
		{
			foreach (var deliveryTag in DeliveryTagFunc(Content))
			{
				await Channel.BasicAckAsync(deliveryTag, false);
			}
			Acknowledged = true;
		}

		public async Task NackAsync(bool requeue = true)
		{
			foreach (var deliveryTag in DeliveryTagFunc(Content))
			{
				await Channel.BasicNackAsync(deliveryTag, false, requeue);
			}
			Acknowledged = true;
		}

		public async Task RejectAsync(bool requeue = true)
		{
			foreach (var deliveryTag in DeliveryTagFunc(Content))
			{
				await Channel.BasicRejectAsync(deliveryTag, requeue);
			}
			Acknowledged = true;
		}

		public void Dispose()
		{
			Channel?.Dispose();
		}
	}
}
