using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Channel
{
	public class ConcurrentChannelQueue
	{
		private readonly ConcurrentQueue<TaskCompletionSource<IChannel>> _queue;

		public EventHandler Queued;

		public ConcurrentChannelQueue()
		{
			_queue = new ConcurrentQueue<TaskCompletionSource<IChannel>>();
		}

		public TaskCompletionSource<IChannel> Enqueue()
		{
			var modelTsc = new TaskCompletionSource<IChannel>();
			var raiseEvent = _queue.IsEmpty;
			_queue.Enqueue(modelTsc);
			if (raiseEvent)
			{
				Queued?.Invoke(this, EventArgs.Empty);
			}

			return modelTsc;
		}

		public bool TryDequeue(out TaskCompletionSource<IChannel> channel)
		{
			return _queue.TryDequeue(out channel);
		}

		public bool IsEmpty => _queue.IsEmpty;

		public int Count => _queue.Count;
	}
}
