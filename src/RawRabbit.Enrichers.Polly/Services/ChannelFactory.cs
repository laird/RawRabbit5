using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Enrichers.Polly.Services
{
	public class ChannelFactory : Channel.ChannelFactory
	{
		protected AsyncPolicy CreateChannelPolicy;
		protected AsyncPolicy ConnectPolicy;
		protected AsyncPolicy GetConnectionPolicy;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies policies = null)
			: base(connectionFactory, config)
		{
			CreateChannelPolicy = policies?.CreateChannel ?? Policy.NoOpAsync();
			ConnectPolicy = policies?.Connect ?? Policy.NoOpAsync();
			GetConnectionPolicy = policies?.GetConnection ?? Policy.NoOpAsync();
		}
// ... (omitting body for brevity, only replacing fields/constructor)
// Wait, I should do the whole file or chunks.
// Multi replacement is safer.


		public override Task ConnectAsync(CancellationToken token = default(CancellationToken))
		{
			return ConnectPolicy.ExecuteAsync(
				action: (ctx, ct) => base.ConnectAsync(ct),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.ConnectionFactory] = ConnectionFactory,
					[RetryKey.ClientConfiguration] = ClientConfig
				},
				cancellationToken: token
			);
		}

		protected override Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
		{
			return GetConnectionPolicy.ExecuteAsync(
				action: (ctx, ct) => base.GetConnectionAsync(ct),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.ConnectionFactory] = ConnectionFactory,
					[RetryKey.ClientConfiguration] = ClientConfig
				},
				cancellationToken: token
			);
		}

		public override Task<IChannel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			return CreateChannelPolicy.ExecuteAsync(
				action: (ctx, ct) => base.CreateChannelAsync(ct),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.ConnectionFactory] = ConnectionFactory,
					[RetryKey.ClientConfiguration] = ClientConfig
				},
				cancellationToken: token
			);
		}
	}

	public class ConnectionPolicies
	{
		/// <summary>
		/// Used whenever 'CreateChannelAsync' is called.
		/// Expects an async policy.
		/// </summary>
		public AsyncPolicy CreateChannel { get; set; }

		/// <summary>
		/// Used whenever an existing connection is retrieved.
		/// </summary>
		public AsyncPolicy GetConnection { get; set; }

		/// <summary>
		/// Used when establishing the initial connection
		/// </summary>
		public AsyncPolicy Connect { get; set; }
	}
}
