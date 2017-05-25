namespace Server
{
    public interface ISocketConnectionProxy
    {
        int Receive(byte[] buffer, int offset, int size);
    }
}
