using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfigurationBuilder : IPublisherConfigurationBuilder
	{
		public PublisherConfiguration Config { get; }

		public PublisherConfigurationBuilder(PublisherConfiguration initial)
		{
			Config = initial;
		}

		public IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeDeclarationBuilder> exchange)
		{
			var builder = new ExchangeDeclarationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Declaration;
			Config.ExchangeName = builder.Declaration.Name;
			return this;
		}

		public IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback)
		{
			Config.ReturnCallback = Config.ReturnCallback ?? ((sender, args) =>{}) ;
			Config.ReturnCallback += (sender, args) => callback(args);
			Config.Mandatory = true;
			return this;
		}

		public IBasicPublishConfigurationBuilder OnExchange(string exchange)
		{
			Config.Exchange = null;
			Truncator.Truncate(ref exchange);
			Config.ExchangeName = exchange;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Config.RoutingKey = routingKey;
			return this;
		}

		public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
		{
			Config.Mandatory = mandatory;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction)
		{
			// Store the property modification action for later execution
			// BasicProperties will be created by BasicPropertiesMiddleware with proper channel
			var existingAction = Config.PropertyModifier;
			Config.PropertyModifier = props =>
			{
				existingAction?.Invoke(props);
				propAction?.Invoke(props);
			};
			return this;
		}
	}
}
