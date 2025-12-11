using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RawRabbit.Channel
{
	public class DynamicChannelPool : StaticChannelPool
	{
		public DynamicChannelPool()
			: this(Enumerable.Empty<IChannel>()) { }

		public DynamicChannelPool(IEnumerable<IChannel> seed)
			: base(seed) { }

		public void Add(params IChannel[] channels)
		{
			Add(channels.ToList());
		}

		public void Add(IEnumerable<IChannel> channels)
		{
			foreach (var channel in channels)
			{
				ConfigureRecovery(channel);
				if (Pool.Contains(channel))
				{
					continue;
				}
				Pool.AddLast(channel);
			}
		}

		public void Remove(int numberOfChannels = 1)
		{
			var toRemove = Pool
				.Take(numberOfChannels)
				.ToList();
			Remove(toRemove);
		}

		public void Remove(params IChannel[] channels)
		{
			Remove(channels.ToList());
		}

		public void Remove(IEnumerable<IChannel> channels)
		{
			foreach (var channel in channels)
			{
				Pool.Remove(channel);
				Recoverables.Remove(channel as IRecoverable);
			}
		}
	}
}
