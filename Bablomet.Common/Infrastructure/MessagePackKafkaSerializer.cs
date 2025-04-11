using System;
using Confluent.Kafka;
using MessagePack;

namespace Bablomet.Common.Infrastructure;

public sealed class MessagePackKafkaSerializer<TObject> : ISerializer<TObject>, IDeserializer<TObject>
{
    public byte[] Serialize(TObject data, SerializationContext context)
    {
        if (data == null) return Array.Empty<byte>();

        return MessagePackSerializer.Serialize(data);
    }

    public TObject Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull) return default;

        return MessagePackSerializer.Deserialize<TObject>(data.ToArray());
    }
}