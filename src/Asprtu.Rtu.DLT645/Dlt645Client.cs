using Asprtu.Rtu.Attributes;
using Asprtu.Rtu.Contracts.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asprtu.Rtu.DLT645;

[LibraryCapacities]
public sealed class Dlt645Client : Channel, IDlt645Client
{
    public Dlt645Client() : base(new("default"), NullLoggerFactory.Instance)
    {
    }
    public Dlt645Client(ChannelOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
    { }



    public Task<bool> TrySendAsync(byte data, out byte[] message)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryWriteAsync(byte[] bytes)
    {
        throw new NotImplementedException();
    }



    public Task<bool> TrySendAsync<T>(T data)
        where T : AbstractMessage, new()
    {
        throw new NotImplementedException();
    }


    public IEnumerable<byte[]> TryReadAddressAsync()
    {
        // ¹ã²¥µØÖ· 6 ×Ö½Ú AA AA AA AA AA AA
        MessageHeader messageHeader = new(
           address: [.. Enumerable.Repeat((byte)0xAA, 6)],
           control: ((byte)Command.ControlCode.ReadAddress),
           bytes: []
        );

        messageHeader.ToBytes(out var a);
        throw new NotImplementedException();
    }

    public Task TryExecuteAsync()
    {
        throw new NotImplementedException();
    }
}