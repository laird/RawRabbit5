using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer;

namespace RawRabbit.Subscription
{
	public interface ISubscription : IDisposable
	{
		string QueueName { get; }
		string ConsumerTag { get; }
		bool Active { get;  }
	}

	public class Subscription : ISubscription
	{
		public string QueueName { get; }
		public string ConsumerTag { get; private set; }
		public bool Active { get; set; }

		private readonly IBasicConsumer _consumer;
		private volatile bool _consumerTagSet = false;

		public Subscription(IBasicConsumer consumer, string queueName)
		{
			Active = true;
			_consumer = consumer;
			QueueName = queueName;
			ConsumerTag = string.Empty; // Will be populated when consumer starts

			// In RabbitMQ.Client 6.x+, ConsumerTag is set during BasicConsume, not on consumer creation
			// Subscribe to Registered event to capture the consumer tag when available
			if (consumer is EventingBasicConsumer eventingConsumer)
			{
				eventingConsumer.Registered += (sender, args) =>
				{
					if (args.ConsumerTags.Length > 0)
					{
						ConsumerTag = args.ConsumerTags[0];
						_consumerTagSet = true;
					}
				};
			}
		}

		public void Dispose()
		{
			if (!Active)
			{
				return;
			}

			Active = false;

			try
			{
				// Check if channel is still open before attempting to cancel
				if (_consumer?.Model != null && _consumer.Model.IsOpen)
				{
					// Use the stored ConsumerTag if available
					if (_consumerTagSet && !string.IsNullOrEmpty(ConsumerTag))
					{
						_consumer.Model.BasicCancel(ConsumerTag);
					}
				}
			}
			catch (Exception)
			{
				// Ignore exceptions during disposal - channel may already be closed
			}
		}
	}
}
