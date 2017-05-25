namespace Server
{
    public interface ISocketConnectionHandler
    {
        int Receive(byte[] buffer, int offset, int size);
    }
}
