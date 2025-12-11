using System;
using System.Linq;
using System.Reflection;
using MessagePack;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack
{
	internal class MessagePackSerializerWorker : ISerializer
	{
		public string ContentType => "application/x-messagepack";
		private readonly MessagePackSerializerOptions _options;

		public MessagePackSerializerWorker(MessagePackFormat format)
		{
			if (format == MessagePackFormat.LZ4Compression)
			{
				_options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
			}
			else
			{
				_options = MessagePackSerializerOptions.Standard;
			}
		}

		public byte[] Serialize(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException();

			return MessagePackSerializer.Serialize(obj.GetType(), obj, _options);
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			return MessagePackSerializer.Deserialize(type, bytes, _options);
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			return MessagePackSerializer.Deserialize<TType>(bytes, _options);
		}
	}
}
