namespace Server
{
    /// <summary>
    /// A proxy for a socket connection.
    /// </summary>
    /// <remarks>
    /// This interface exists to make <see cref="SocketStreamReader"/>
    /// testable.
    /// </remarks>
    public interface ISocketConnectionProxy
    {
        int Receive(byte[] buffer, int offset, int size);
    }
}
