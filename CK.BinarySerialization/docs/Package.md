Binary serialization built for performance, immutability and type mutation.

Writing and reading are deliberately asymmetric: the reader is given the type information that was
written, so a type can be renamed, moved to another namespace or assembly, turned from a struct into a
class, and still be read back. A deserialization hook can rewrite that type information before a driver
is chosen.

Drivers are the serializers and deserializers themselves; resolvers find them, and both live in shared
contexts that honour each driver's declared cache level. The two lighter CK.Core contracts,
ICKSimpleBinarySerializable and ICKVersionedBinarySerializable, are handled out of the box.
