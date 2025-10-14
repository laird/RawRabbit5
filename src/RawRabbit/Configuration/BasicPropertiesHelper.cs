using RabbitMQ.Client;

namespace RawRabbit.Configuration
{
	/// <summary>
	/// Helper class for creating BasicProperties in RabbitMQ.Client 6.x+
	/// where the BasicProperties constructor is protected.
	/// </summary>
	public static class BasicPropertiesHelper
	{
		/// <summary>
		/// Creates a new IBasicProperties instance using the channel's factory method.
		/// This is the recommended approach for RabbitMQ.Client 6.x and later.
		/// </summary>
		/// <param name="channel">The channel to create properties for</param>
		/// <returns>A new IBasicProperties instance</returns>
		public static IBasicProperties CreateBasicProperties(IModel channel)
		{
			return channel.CreateBasicProperties();
		}
	}
}
