using System.Runtime.InteropServices;

namespace Irc.Helpers;

public static class SerializationExtensions
{
    public static byte[] Serialize<T>(this object serializableObject)
    {
        var ptrMessageSize = Marshal.SizeOf<T>();
        var serialBytes = new byte[ptrMessageSize];
        var pBuf = Marshal.AllocHGlobal(ptrMessageSize);
        try
        {
            Marshal.StructureToPtr(serializableObject, pBuf, false);
            for (var i = 0; i < ptrMessageSize; i++) serialBytes[i] = Marshal.ReadByte(pBuf, i);

            return serialBytes;
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
        finally
        {
            Marshal.FreeHGlobal(pBuf);
        }
    }

    public static T Deserialize<T>(this byte[] bytes)
    {
        var size = Marshal.SizeOf<T>();
        var pBuf = IntPtr.Zero;
        try
        {
            pBuf = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, pBuf, size);
            return Marshal.PtrToStructure<T>(pBuf);
        }
        catch (Exception)
        {
            return default;
        }
        finally
        {
            if (pBuf != IntPtr.Zero) Marshal.FreeHGlobal(pBuf);
        }
    }
}