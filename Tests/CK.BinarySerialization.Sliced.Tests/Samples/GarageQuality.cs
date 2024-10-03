using System;
using System.Collections.Generic;
using System.Text;
using CK.Core;

namespace CK.BinarySerialization.Tests.Samples;

/// <summary>
/// The underlying type is an int. 
/// In V2 it will be downgraded to a byte.
/// This will work ONLY because we use <see cref="IBinarySerializer.WriteValue{T}(in T)"/> and <see cref="IBinaryDeserializer.ReadValue{T}"/>.
/// Using the <see cref="ICKBinaryWriter.WriteEnum{T}(T)"/> and <see cref="ICKBinaryReader.ReadEnum{T}"/> CANNOT handle such migrations.
/// </summary>
public enum GarageQuality
{
    Awful,
    Noob,
    CanDoBetter,
    Correct,
    TopQuality
}
