# CK-CommChannel

Simple abstraction based on PipeReader/Writer API of communication channels.
Communication channels are low-level component typically used by the CK-DeviceModel and their conception
follows the same pattern:

- Configuration objects ([CommunicationChannelConfiguration](CK.CommChannel/CommunicationChannelConfiguration.cs)) drive the 
instantiation of the actual [CommunicationChannel](CK.CommChannel/CommunicationChannel.cs).
- Dynamic reconfiguration of channels can be supported (when possible).
- Configuration objects are basically serializable (thanks to CK.Core.ICKSimpleBinarySerializable like the device's 
configuration) and hence support deep cloning.

However, since there should be relatively few channel types, the model is not as polished as the one of the device:

- There is no strong typing (via generics) of Configuration from inside Channel code (downcasting is required).
- The Channel factory is basic and throw as soon as something goes wrong.
- There is no protection of the configuration instance via deep clones: this should be done in the caller code (the device model does this).

## Visibilities

The configuration object must obviously be public, but the channel implementation should be internal: 
the [ICommunicationChannel](CK.CommChannel/ICommunicationChannel.cs) interface generalizes all channels and
this should be enough.

## Dynamic reconfiguration

Dynamic reconfiguration relies on a 3-states check of the current and the new configuration, the
`CommunicationChannelConfiguration.CanReconfigure` returns a nullable boolean:

- The current and new configuration are exactly the same: reconfiguration is useless, `null` is returned.
- The current and new configuration are not exactly the same:
  - Changes are minor and can be applied without disposing the channel and recreating a new one: `true` is returned.
  - A brand new channel should be used since configurations are really different: `false` is returned.

Typical channel implementations don't support truly dynamic reconfiguration, they simply require the current and future
configuration to be equal: `CanReconfigure` is a kind of `IsEqual` that returns null (no change at all: the actual
reconfiguration is a no-op) or false (a new channel is required).
 
The only example of a non-typical channel regarding configuration is the [MemoryChannel](CK.CommChannel/Memory/MemoryChannel.cs).
It is configured with an endpoint name and a connection timeout and this timeout is used only when the channel is created.

The `MemoryChannel.CanReconfigureChannel` uses the 3-states possible value:

```csharp
  /// <summary>
  /// When <see cref="EndPointName"/> differs, there cannot be any reconfiguration: this returns false.
  /// When the same EndPoint is targeted, if <see cref="ConnectionTimeout"/> are the same then null is returned
  /// (identical configuration, there's nothing to do), and if connection timeout differs, this returns true
  /// because even if the configuration actually changed, an already opened channel doesn't care anymore
  /// and we can accept the no-op reconfiguration.
  /// </summary>
  /// </summary>
  /// <param name="configuration">The new configuration to apply.</param>
  /// <returns>Null if it's the exact same endpoint and connection timeout, true if endpoint are the same, false otherwise.</returns>
  protected override bool? CanReconfigureChannel( CommunicationChannelConfiguration configuration )
  {
      if( configuration is not MemoryChannelConfiguration o ) throw new ArgumentException( "Must be a MemoryChannelConfiguration.", nameof( configuration ) );
      return EndPointName == o.EndPointName
              ? (ConnectionTimeout == o.ConnectionTimeout
                  ? null
                  : true)
              : false;
  }
```

Actual reconfiguration must be implemented by the channel's `protected abstract ValueTask DoReconfigureAsync( CommunicationChannelConfiguration configuration )`
and a typical implementation (described above) has nothing to do (since both configurations are the same when this is called).
Even the MemoryChannel has nothing to do: being called because nothing at all changed or because the configuration timeout changed
has no impact.

## TcpChannel implementation

Below is the full TCP channel implementation:

```csharp
sealed class TcpChannel : CommunicationChannel
{
    readonly TcpClient _client;

    TcpChannel( TcpClient client, TcpChannelConfiguration configuration )
        : base( client.GetStream(), configuration )
    {
        _client = client;
    }

    protected override ValueTask DoReconfigureAsync( CommunicationChannelConfiguration configuration ) => default;
        
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _client.Dispose();
    }

    public static async ValueTask<CommunicationChannel> CreateAsync( IActivityMonitor monitor,
                                                                     TcpChannelConfiguration configuration )
    {
        var client = new TcpClient();
        await client.ConnectAsync( configuration.Host, configuration.Port );
        return new TcpChannel( client, configuration );
    }
}
```


