using System.Security.Cryptography;

static class Receive
{
    public static async Task ReceiveFile(string url, string? key, string? output, bool force)
    {
        Console.Error.WriteLine($"Downloading from {url}...");

        var responseData = await Rubis.Receive(url);
        var payload = TryDecodeBase64(responseData);
        var (data, compressed, encrypted, filename, nonce, salt) = Payload.Decode(payload);

        data = DecryptIfNeeded(data, encrypted, key, nonce, salt);
        data = DecompressIfNeeded(data, compressed);

        var outputFile = GetOutputFilename(filename, output);
        await SaveFile(data, outputFile, force);

        Console.Error.WriteLine($"Saved to {outputFile}");
    }

    static byte[] TryDecodeBase64(byte[] data)
    {
        try
        {
            return Payload.FromBase64(System.Text.Encoding.UTF8.GetString(data));
        }
        catch
        {
            return data;
        }
    }

    static byte[] DecryptIfNeeded(byte[] data, bool encrypted, string? key, byte[]? nonce, byte[]? salt)
    {
        if (!encrypted)
            return data;

        if (key == null)
        {
            Console.Error.WriteLine("Error: Payload is encrypted but no key was provided. Use --key to decrypt.");
            Environment.Exit(1);
        }

        if (nonce == null || salt == null)
        {
            Console.Error.WriteLine("Error: Encrypted payload missing nonce or salt");
            Environment.Exit(1);
        }

        try
        {
            var decrypted = Encryption.Decrypt(data, key, nonce, salt);
            Console.Error.WriteLine("Decrypted successfully");
            return decrypted;
        }
        catch (CryptographicException)
        {
            Console.Error.WriteLine("Error: Decryption failed. Wrong key or corrupted data.");
            Environment.Exit(1);
            return data;
        }
    }

    static byte[] DecompressIfNeeded(byte[] data, bool compressed)
    {
        if (!compressed)
            return data;

        var decompressed = Compression.Decompress(data);
        Console.Error.WriteLine("Decompressed successfully");
        return decompressed;
    }

    static string GetOutputFilename(string? payloadFilename, string? output)
    {
        if (!string.IsNullOrEmpty(output))
            return output;

        if (!string.IsNullOrEmpty(payloadFilename))
            return payloadFilename;

        return "received";
    }

    static async Task SaveFile(byte[] data, string outputFile, bool force)
    {
        if (File.Exists(outputFile) && !force)
        {
            Console.Error.WriteLine($"Error: Output file already exists: {outputFile}");
            Environment.Exit(1);
        }

        var tempFile = outputFile + ".tmp";
        await File.WriteAllBytesAsync(tempFile, data);
        File.Move(tempFile, outputFile, overwrite: true);
    }
}
