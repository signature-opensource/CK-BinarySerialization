Makes any type binary-serializable through a single marker interface, ICKSlicedSerializable: each level of a
class hierarchy writes and reads its own slice, with its own SerializationVersion.
A non sealed class also exposes an empty constructor taking the Sliced marker, called by its specializations.
