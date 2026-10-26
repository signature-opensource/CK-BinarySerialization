Binary serialization built for performance, immutability and type mutation: a type can be renamed, moved or
turned from a struct into a class and still be read back.
Entry points are BinarySerializer.Create and BinaryDeserializer.Deserialize, with their BinarySerializerContext and
BinaryDeserializerContext and the shared contexts that hold drivers, resolvers and deserialization hooks.
