using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Serialization;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfigurationFactory : IBasicPublishConfigurationFactory
	{
		private readonly INamingConventions _conventions;
		private readonly ISerializer _serializer;
		private readonly RawRabbitConfiguration _config;

		public BasicPublishConfigurationFactory(INamingConventions conventions, ISerializer serializer, RawRabbitConfiguration config)
		{
			_conventions = conventions;
			_serializer = serializer;
			_config = config;
		}

		public virtual BasicPublishConfiguration Create(object message)
		{
			if (message == null)
			{
				return Create();
			}
			var cfg = Create(message.GetType());
			cfg.Body = GetBody(message);
			return cfg;
		}

		public virtual BasicPublishConfiguration Create(Type type)
		{
			return new BasicPublishConfiguration
			{
				RoutingKey = GetRoutingKey(type),
				BasicProperties = GetBasicProperties(type),
				ExchangeName = GetExchangeName(type),
				Mandatory = GetMandatory(type),
				PropertyModifier = GetPropertyModifier(type)
			};
		}

		public virtual BasicPublishConfiguration Create()
		{
			return new BasicPublishConfiguration
			{
				BasicProperties = null // Will be created by middleware with proper channel
			};
		}

		protected  virtual string GetRoutingKey(Type type)
		{
			return _conventions.RoutingKeyConvention(type);
		}

		protected virtual bool GetMandatory(Type type)
		{
			return false;
		}

		protected virtual string GetExchangeName(Type type)
		{
			return _conventions.ExchangeNamingConvention(type);
		}

		protected virtual IBasicProperties GetBasicProperties(Type type)
		{
			// Return null - BasicPropertiesMiddleware will create it with proper channel
			return null;
		}

		protected virtual Action<IBasicProperties> GetPropertyModifier(Type type)
		{
			return props =>
			{
				props.Type = type.GetUserFriendlyName();
				props.MessageId = Guid.NewGuid().ToString();
				props.DeliveryMode = _config.PersistentDeliveryMode ? Convert.ToByte(2) : Convert.ToByte(1);
				props.ContentType = _serializer.ContentType;
				props.ContentEncoding = "UTF-8";
				props.UserId = _config.Username;
				if (props.Headers == null)
				{
					props.Headers = new Dictionary<string, object>();
				}
			};
		}

		protected virtual byte[] GetBody(object message)
		{
			return _serializer.Serialize(message);
		}
	}
}
