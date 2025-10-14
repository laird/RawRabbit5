using System;
using MessagePack;
using MessagePack.Resolvers;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack
{
	internal class MessagePackSerializerWorker : ISerializer
	{
		public string ContentType => "application/x-messagepack";
		private readonly MessagePackSerializerOptions _options;

		public MessagePackSerializerWorker(MessagePackFormat format)
		{
			// MessagePack v2.x uses MessagePackSerializerOptions instead of separate serializer types
			_options = format == MessagePackFormat.LZ4Compression
				? MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray)
				: MessagePackSerializerOptions.Standard;
		}

		public byte[] Serialize(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			// MessagePack v2.x uses MessagePackSerializer.Serialize with options
			return MessagePackSerializer.Typeless.Serialize(obj, _options);
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			// MessagePack v2.x uses MessagePackSerializer.Deserialize with type parameter
			return MessagePackSerializer.Deserialize(type, bytes, _options)
				?? throw new InvalidOperationException($"Failed to deserialize object of type {type}");
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			return MessagePackSerializer.Deserialize<TType>(bytes, _options);
		}
	}
}
