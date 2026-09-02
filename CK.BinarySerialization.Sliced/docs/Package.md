Makes any type binary-serializable through a single marker interface, ICKSlicedSerializable.

Inheritance is the point: each level of a hierarchy writes and reads its own slice, so a base class and
its specializations evolve their serialized shape independently, each with its own SerializationVersion.

A destroyed instance is written in a shortened form - only the root constructor runs on the way back,
specialized ones are skipped.
