using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public class ChannelFactory : IChannelFactory
	{
		private readonly ILog _logger = LogProvider.For<ChannelFactory>();
		protected readonly IConnectionFactory ConnectionFactory;
		protected readonly RawRabbitConfiguration ClientConfig;
		protected readonly ConcurrentBag<IChannel> Channels;
		protected IConnection Connection;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config)
		{
			ConnectionFactory = connectionFactory;
			ClientConfig = config;
			Channels = new ConcurrentBag<IChannel>();
		}

		public virtual async Task ConnectAsync(CancellationToken token = default(CancellationToken))
		{
			try
			{
				_logger.Debug("Creating a new connection for {hostNameCount} hosts.", ClientConfig.Hostnames.Count);
				Connection = await ConnectionFactory.CreateConnectionAsync(ClientConfig.Hostnames, ClientConfig.ClientProvidedName);
				Connection.ConnectionShutdownAsync += (sender, args) =>
                {
					_logger.Warn("Connection was shutdown by {Initiator}. ReplyText {ReplyText}", args.Initiator, args.ReplyText);
                    return Task.CompletedTask;
                };
			}
			catch (BrokerUnreachableException e)
			{
				_logger.Info("Unable to connect to broker", e);
				throw;
			}
		}

		public virtual async Task<IChannel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			var connection = await GetConnectionAsync(token);
			token.ThrowIfCancellationRequested();
			var channel = await connection.CreateChannelAsync();
			Channels.Add(channel);
			return channel;
		}

		protected virtual async Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
		{
			token.ThrowIfCancellationRequested();
			if (Connection == null)
			{
				await ConnectAsync(token);
			}
			if (Connection.IsOpen)
			{
				_logger.Debug("Existing connection is open and will be used.");
				return Connection;
			}
			_logger.Info("The existing connection is not open.");

			if (Connection.CloseReason != null &&Connection.CloseReason.Initiator == ShutdownInitiator.Application)
			{
				_logger.Info("Connection is closed with Application as initiator. It will not be recovered.");
				Connection.Dispose();
				throw new ChannelAvailabilityException("Closed connection initiated by the Application. A new connection will not be created, and no channel can be created.");
			}

			if (!(Connection is IRecoverable recoverable))
			{
				_logger.Info("Connection is not recoverable");
				Connection.Dispose();
				throw new ChannelAvailabilityException("The non recoverable connection is closed. A channel can not be created.");
			}

			_logger.Debug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
			var recoverTcs = new TaskCompletionSource<IConnection>();
			token.Register(() => recoverTcs.TrySetCanceled());

			AsyncEventHandler<AsyncEventArgs> completeTask = null;
			completeTask = (sender, args) =>
			{
				if (recoverTcs.Task.IsCanceled)
				{
					return Task.CompletedTask;
				}
				_logger.Info("Connection has been recovered!");
				recoverTcs.TrySetResult(recoverable as IConnection);
				recoverable.RecoveryAsync -= completeTask;
                return Task.CompletedTask;
			};

			recoverable.RecoveryAsync += completeTask;
			return await recoverTcs.Task;
		}

		public void Dispose()
		{
			foreach (var channel in Channels)
			{
				channel?.Dispose();
			}
			Connection?.Dispose();
		}
	}
}

