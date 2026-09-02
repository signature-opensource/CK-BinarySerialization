Teaches the binary serializer to read and write any IPoco.

Nothing to implement: no marker interface, no attribute, and no registration either - referencing the
package is enough, the resolvers are picked up reflectively. What is required is a PocoDirectory in the
serialization context's services.

A Poco is written as a length-prefixed JSON payload under the driver name "IPocoJson", without the usual
type envelope, and recorded under its primary interface rather than the generated class. The export
option used carries the "AllSerializable" type filter rather than the default "AllExchangeable".
