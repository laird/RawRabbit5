using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Enrichers.Polly.Services
{
	public class ChannelFactory : Channel.ChannelFactory
	{
		protected ResiliencePipeline CreateChannelPipeline;
		protected ResiliencePipeline ConnectPipeline;
		protected ResiliencePipeline GetConnectionPipeline;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies? policies = null)
			: base(connectionFactory, config)
		{
			CreateChannelPipeline = policies?.CreateChannel ?? ResiliencePipeline.Empty;
			ConnectPipeline = policies?.Connect ?? ResiliencePipeline.Empty;
			GetConnectionPipeline = policies?.GetConnection ?? ResiliencePipeline.Empty;
		}

		public override Task ConnectAsync(CancellationToken token = default)
		{
			return ConnectPipeline.ExecuteAsync(
				async ct => await base.ConnectAsync(ct),
				cancellationToken: token
			).AsTask();
		}

		protected override Task<IConnection> GetConnectionAsync(CancellationToken token = default)
		{
			return GetConnectionPipeline.ExecuteAsync(
				async ct => await base.GetConnectionAsync(ct),
				cancellationToken: token
			).AsTask();
		}

		public override Task<IModel> CreateChannelAsync(CancellationToken token = default)
		{
			return CreateChannelPipeline.ExecuteAsync(
				async ct => await base.CreateChannelAsync(ct),
				cancellationToken: token
			).AsTask();
		}
	}

	public class ConnectionPolicies
	{
		/// <summary>
		/// Used whenever 'CreateChannelAsync' is called.
		/// Expects a resilience pipeline.
		/// </summary>
		public ResiliencePipeline? CreateChannel { get; set; }

		/// <summary>
		/// Used whenever an existing connection is retrieved.
		/// </summary>
		public ResiliencePipeline? GetConnection { get; set; }

		/// <summary>
		/// Used when establishing the initial connection
		/// </summary>
		public ResiliencePipeline? Connect { get; set; }
	}
}
