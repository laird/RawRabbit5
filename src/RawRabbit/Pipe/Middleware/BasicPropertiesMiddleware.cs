using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPropertiesOptions
	{
		public Action<IPipeContext, IBasicProperties> PropertyModier { get; set; }
		public Func<IPipeContext, IBasicProperties> GetOrCreatePropsFunc { get; set; }
		public Action<IPipeContext, IBasicProperties> PostCreateAction { get; set; }
	}

	public class BasicPropertiesMiddleware : Middleware
	{
		protected ISerializer Serializer;
		protected Func<IPipeContext, IBasicProperties> GetOrCreatePropsFunc;
		protected Action<IPipeContext, IBasicProperties> PropertyModifier;
		protected Action<IPipeContext, IBasicProperties> PostCreateAction;

		public BasicPropertiesMiddleware(ISerializer serializer, BasicPropertiesOptions options = null)
		{
			Serializer = serializer;
			PropertyModifier = options?.PropertyModier ?? ((ctx, props) =>
			{
				// Apply modifier from PipeKey first
				ctx.Get<Action<IBasicProperties>>(PipeKey.BasicPropertyModifier)?.Invoke(props);

				// Apply modifier from configuration if present
				var basicPublishConfig = ctx.GetBasicPublishConfiguration();
				basicPublishConfig?.PropertyModifier?.Invoke(props);
			});
			PostCreateAction = options?.PostCreateAction;
			GetOrCreatePropsFunc = options?.GetOrCreatePropsFunc ?? (ctx =>
			{
				var existingProps = ctx.GetBasicProperties();
				if (existingProps != null)
				{
					return existingProps;
				}

				// Get channel to create properties
				var channel = ctx.GetTransientChannel() ?? ctx.GetChannel();
				if (channel == null)
				{
					throw new InvalidOperationException("Cannot create BasicProperties without a channel. Ensure channel middleware runs before BasicPropertiesMiddleware.");
				}

				var props = channel.CreateBasicProperties();
				props.MessageId = Guid.NewGuid().ToString();
				props.Headers = new Dictionary<string, object>();
				props.Persistent = ctx.GetClientConfiguration().PersistentDeliveryMode;
				props.ContentType = Serializer.ContentType;
				return props;
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var props = GetOrCreateBasicProperties(context);
			ModifyBasicProperties(context, props);
			InvokePostCreateAction(context, props);
			context.Properties.TryAdd(PipeKey.BasicProperties, props);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void ModifyBasicProperties(IPipeContext context, IBasicProperties props)
		{
			PropertyModifier?.Invoke(context, props);
		}

		protected virtual void InvokePostCreateAction(IPipeContext context, IBasicProperties props)
		{
			PostCreateAction?.Invoke(context, props);
		}

		protected virtual IBasicProperties GetOrCreateBasicProperties(IPipeContext context)
		{
			return GetOrCreatePropsFunc(context);
		}
	}
}
