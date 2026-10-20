class Payload
{
    const byte Version = 1;
    const byte FlagCompressed = 1 << 0;
    const byte FlagEncrypted = 1 << 1;

    public static byte[] Encode(byte[] data, bool compressed, bool encrypted, string? filename = null, byte[]? nonce = null, byte[]? salt = null)
    {
        var ms = new MemoryStream();
        ms.WriteByte(Version);

        byte flags = 0;
        if (compressed) flags |= FlagCompressed;
        if (encrypted) flags |= FlagEncrypted;
        ms.WriteByte(flags);

        var filenameBytes = filename != null ? System.Text.Encoding.UTF8.GetBytes(filename) : Array.Empty<byte>();
        WriteLengthPrefixed(ms, filenameBytes);

        if (encrypted)
        {
            if (nonce == null || salt == null)
                throw new ArgumentException("Nonce and salt required for encrypted payloads");

            WriteLengthPrefixed(ms, nonce);
            WriteLengthPrefixed(ms, salt);
        }

        ms.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    public static (byte[] data, bool compressed, bool encrypted, string? filename, byte[]? nonce, byte[]? salt) Decode(byte[] payload)
    {
        if (payload.Length < 2)
            throw new InvalidDataException("Payload too short");

        int pos = 0;
        byte version = payload[pos++];
        if (version != Version)
            throw new InvalidDataException($"Unsupported payload version: {version}");

        byte flags = payload[pos++];
        bool compressed = (flags & FlagCompressed) != 0;
        bool encrypted = (flags & FlagEncrypted) != 0;

        (byte[] filenameBytes, pos) = ReadLengthPrefixed(payload, pos);
        string? filename = filenameBytes.Length > 0 ? System.Text.Encoding.UTF8.GetString(filenameBytes) : null;

        byte[]? nonce = null;
        byte[]? salt = null;

        if (encrypted)
        {
            (nonce, pos) = ReadLengthPrefixed(payload, pos);
            (salt, pos) = ReadLengthPrefixed(payload, pos);
        }

        byte[] data = new byte[payload.Length - pos];
        Array.Copy(payload, pos, data, 0, data.Length);

        return (data, compressed, encrypted, filename, nonce, salt);
    }

    static void WriteLengthPrefixed(MemoryStream ms, byte[] data)
    {
        ms.WriteByte((byte)(data.Length >> 8));
        ms.WriteByte((byte)data.Length);
        ms.Write(data, 0, data.Length);
    }

    static (byte[] data, int newPos) ReadLengthPrefixed(byte[] payload, int pos)
    {
        if (pos + 2 > payload.Length)
            throw new InvalidDataException("Invalid length prefix");

        int length = (payload[pos] << 8) | payload[pos + 1];
        pos += 2;

        if (pos + length > payload.Length)
            throw new InvalidDataException("Length prefix exceeds payload size");

        byte[] data = new byte[length];
        Array.Copy(payload, pos, data, 0, length);
        return (data, pos + length);
    }

    public static string ToBase64(byte[] data)
    {
        return Convert.ToBase64String(data);
    }

    public static byte[] FromBase64(string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Invalid Base64 data", ex);
        }
    }
}
