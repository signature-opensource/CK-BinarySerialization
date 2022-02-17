using System;

namespace CK.BinarySerialization
{
    sealed class SlicedReferenceSerializer<T> : ReferenceTypeSerializer<T> where T : class
    {
        public SlicedReferenceSerializer( int version )
        {
            SerializationVersion = version;
        }

        public override string DriverName => "Sliced";

        public override int SerializationVersion { get; }

        protected override void Write( IBinarySerializer w, in T o )
        {
            throw new NotImplementedException();
        }
    }
}
