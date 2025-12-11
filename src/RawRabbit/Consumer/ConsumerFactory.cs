using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Consume;
using RawRabbit.Logging;

namespace RawRabbit.Consumer
{
	public class ConsumerFactory : IConsumerFactory
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ConcurrentDictionary<string, Lazy<Task<IAsyncBasicConsumer>>> _consumerCache;
		private readonly ILog _logger = LogProvider.For<ConsumerFactory>();

		public ConsumerFactory(IChannelFactory channelFactory)
		{
			_consumerCache = new ConcurrentDictionary<string, Lazy<Task<IAsyncBasicConsumer>>>();
			_channelFactory = channelFactory;
		}

		public Task<IAsyncBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg, IChannel channel = null, CancellationToken token = default(CancellationToken))
		{
			var consumerKey = CreateConsumerKey(cfg);
			var lazyConsumerTask = _consumerCache.GetOrAdd(consumerKey, routingKey =>
			{
				return new Lazy<Task<IAsyncBasicConsumer>>(async () =>
				{
					var consumer = await CreateConsumerAsync(channel, token);
					return consumer;
				});
			});
			return lazyConsumerTask.Value;
		}

		public Task<IAsyncBasicConsumer> GetConfiguredConsumerAsync(ConsumeConfiguration cfg, IChannel channel = null, CancellationToken token = default(CancellationToken))
		{
			var consumerKey = CreateConsumerKey(cfg);
			var lazyConsumerTask = _consumerCache.GetOrAdd(consumerKey, routingKey =>
			{
				return new Lazy<Task<IAsyncBasicConsumer>>(async () =>
				{
					var consumer = await CreateConsumerAsync(channel, token);
					ConfigureConsume(consumer, cfg);
					return consumer;
				});
			});
			if (lazyConsumerTask.Value.IsCompleted && !((RawRabbitConsumer)lazyConsumerTask.Value.Result).Channel.IsOpen)
			{
				_consumerCache.TryRemove(consumerKey, out _);
				return GetConsumerAsync(cfg, channel, token);
			}
			return lazyConsumerTask.Value;
		}

		public async Task<IAsyncBasicConsumer> CreateConsumerAsync(IChannel channel = null, CancellationToken token = default(CancellationToken))
		{
			if (channel == null)
			{
				channel = await GetOrCreateChannelAsync(token);
			}
			return new RawRabbitConsumer(channel);
		}

		public IAsyncBasicConsumer ConfigureConsume(IAsyncBasicConsumer consumer, ConsumeConfiguration cfg)
		{
			CheckPropertyValues(cfg);

			if (cfg.PrefetchCount > 0)
			{
				_logger.Info("Setting Prefetch Count to {prefetchCount}.", cfg.PrefetchCount);
				((AsyncEventingBasicConsumer)consumer).Channel.BasicQosAsync(
					prefetchSize: 0,
					prefetchCount: cfg.PrefetchCount,
					global: false
				).GetAwaiter().GetResult();
			}

			_logger.Info("Preparing to consume message from queue '{queueName}'.", cfg.QueueName);

			var tag = ((RawRabbitConsumer)consumer).Channel.BasicConsumeAsync(
				queue: cfg.QueueName,
				autoAck: cfg.AutoAck,
				consumerTag: cfg.ConsumerTag,
				noLocal: cfg.NoLocal,
				exclusive: cfg.Exclusive,
				arguments: cfg.Arguments,
				consumer: consumer).GetAwaiter().GetResult();
            
            ((RawRabbitConsumer)consumer).ConsumerTag = tag;
			return consumer;
		}

		protected virtual void CheckPropertyValues(ConsumeConfiguration cfg)
		{
			if (cfg == null)
			{
				throw new ArgumentException("Unable to create consumer. The provided configuration is null");
			}
			if (string.IsNullOrEmpty(cfg.QueueName))
			{
				throw new ArgumentException("Unable to create consume. No queue name provided.");
			}
			if (string.IsNullOrEmpty(cfg.ConsumerTag))
			{
				throw new ArgumentException("Unable to create consume. Consumer tag cannot be undefined.");
			}
		}

		protected virtual Task<IChannel> GetOrCreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			_logger.Info("Creating a dedicated channel for consumer.");
			return _channelFactory.CreateChannelAsync(token);
		}

		protected string CreateConsumerKey(ConsumeConfiguration cfg)
		{
			return $"{cfg.QueueName}:{cfg.RoutingKey}:{cfg.AutoAck}";
		}
	}

	public static class ConsumerExtensions
	{
		public static Task<string> CancelAsync(this IAsyncBasicConsumer consumer, CancellationToken token = default(CancellationToken))
		{
			var eventConsumer = consumer as RawRabbitConsumer;
			if (eventConsumer == null)
			{
				throw new NotSupportedException("Only supported for RawRabbitConsumer");
			}
			var cancelTcs = new TaskCompletionSource<string>();
			token.Register(() => cancelTcs.TrySetCanceled());
			var tag = eventConsumer.ConsumerTag;
			eventConsumer.ConsumerCancelledAsync += (sender, args) =>
			{
				if (args.ConsumerTags.Contains(tag))
				{
				    cancelTcs.TrySetResult(tag);
				}
                return Task.CompletedTask;
			};
			((RawRabbitConsumer)consumer).Channel.BasicCancelAsync(eventConsumer.ConsumerTag);
			return cancelTcs.Task;
		}

		public static void OnMessage(this IAsyncBasicConsumer consumer, EventHandler<BasicDeliverEventArgs> onMessage, Predicate<BasicDeliverEventArgs> abort = null)
		{
			var eventConsumer = consumer as RawRabbitConsumer;
			if (eventConsumer == null)
			{
				throw new NotSupportedException("Only supported for RawRabbitConsumer");
			}
			AsyncEventHandler<BasicDeliverEventArgs> handler = null;
            handler = (sender, args) => 
            {
                onMessage(sender, args);
                if (abort != null && abort(args))
                {
                    eventConsumer.ReceivedAsync -= handler;
                }
                				return Task.CompletedTask;
            };
            eventConsumer.ReceivedAsync += handler;
		}
	}

	public class RawRabbitConsumer : AsyncEventingBasicConsumer
	{
		public RawRabbitConsumer(IChannel channel) : base(channel)
		{
		}

		public string ConsumerTag { get; set; }
        
        public event AsyncEventHandler<ConsumerEventArgs> ConsumerCancelledAsync;

        protected override async Task OnCancelAsync(string[] consumerTags, CancellationToken cancellationToken)
        {
            await base.OnCancelAsync(consumerTags, cancellationToken);
            if (ConsumerCancelledAsync != null) 
            {
                 await ConsumerCancelledAsync(this, new ConsumerEventArgs(consumerTags));
            }
        }
	}
}
