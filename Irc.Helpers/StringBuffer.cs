using System.Text;

namespace Irc.Helpers;

public class StringBuffer
{
    private readonly StringBuilder _buffer;
    private readonly int buffSize;

    public List<ArraySegment<byte>> bytesOut;
    private int cursor;

    //needs to be converted to a Queue later
    public List<string> DataIn;
    public List<string> DataOut;

    //e.g.                  1024      512

    public StringBuffer(int size)

    {
        _buffer = new StringBuilder(new string('\0', size));
        buffSize = size;
        cursor = 0;

        DataIn = new List<string>();
        DataOut = new List<string>();
        bytesOut = new List<ArraySegment<byte>>();
    }

    public int Capacity => _buffer.Capacity;

    public void Digest(byte[] data, int bytes)
    {
        for (var dataPos = 0; dataPos < bytes; dataPos++)
            if (cursor < buffSize) //ensure the current cursor is < 512 bytes
            {
                if (data[dataPos] != '\r' && data[dataPos] != '\n')
                {
                    _buffer[cursor++] = (char)data[dataPos]; //copy in
                }
                else
                {
                    if (cursor > 0) //to stop problems with joined crlf
                    {
                        var message = new StringBuilder(_buffer.ToString().Substring(0, cursor));
                        cursor = 0;
                        DataIn.Add(message.ToString());
                    }
                }
            }
            else
            {
                var message = new StringBuilder(_buffer.ToString().Substring(0, cursor));
                cursor = 0;
                DataIn.Add(message.ToString());
            }

        if (cursor == buffSize)
        {
            var message = new StringBuilder(_buffer.ToString().Substring(0, cursor));
            cursor = 0;
            DataIn.Add(message.ToString());
        }
    }
}