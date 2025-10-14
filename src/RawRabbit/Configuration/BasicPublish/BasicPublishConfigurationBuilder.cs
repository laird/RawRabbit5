using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfigurationBuilder : IBasicPublishConfigurationBuilder
	{
		public BasicPublishConfiguration Configuration { get; }

		public BasicPublishConfigurationBuilder(BasicPublishConfiguration initial)
		{
			Configuration = initial;
		}

		public IBasicPublishConfigurationBuilder OnExchange(string exchange)
		{
			Configuration.ExchangeName = exchange;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
		{
			Configuration.Mandatory = mandatory;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction)
		{
			// Store the property modification action for later execution
			// BasicProperties will be created by BasicPropertiesMiddleware with proper channel
			var existingAction = Configuration.PropertyModifier;
			Configuration.PropertyModifier = props =>
			{
				existingAction?.Invoke(props);
				propAction?.Invoke(props);
			};
			return this;
		}
	}
}