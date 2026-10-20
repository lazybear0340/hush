static class Upload
{
    public static async Task UploadFile(string filePath, string? key, string? keyEnv, string? url)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File not found: {filePath}");
            Environment.Exit(1);
        }

        var actualKey = GetKey(key, keyEnv);
        var fileData = await File.ReadAllBytesAsync(filePath);
        var filename = Path.GetFileName(filePath);

        Console.Error.WriteLine($"Read {fileData.Length} bytes from {filePath}");

        var (data, compressed, encrypted, nonce, salt) = ProcessData(fileData, actualKey);

        var payload = Payload.Encode(data, compressed, encrypted, filename, nonce, salt);
        var base64Payload = Payload.ToBase64(payload);

        Console.Error.WriteLine($"Uploading {base64Payload.Length} characters...");

        var shareUrl = url != null
            ? await Rubis.Upload(base64Payload, url)
            : await Rubis.Upload(base64Payload);

        Console.WriteLine(shareUrl);
    }

    static (byte[] data, bool compressed, bool encrypted, byte[]? nonce, byte[]? salt) ProcessData(byte[] fileData, string? key)
    {
        var data = Compression.CompressIfUseful(fileData);
        var compressed = data.Length < fileData.Length;

        if (compressed)
            Console.Error.WriteLine($"Compressed: {fileData.Length} -> {data.Length} bytes");

        if (key == null)
            return (data, compressed, false, null, null);

        var (encrypted, nonce, salt) = Encryption.Encrypt(data, key);
        Console.Error.WriteLine("Encrypted with provided key");
        return (encrypted, compressed, true, nonce, salt);
    }

    static string? GetKey(string? key, string? keyEnv)
    {
        if (key != null)
            return key;

        if (keyEnv != null)
        {
            var envValue = Environment.GetEnvironmentVariable(keyEnv);
            if (envValue == null)
            {
                Console.Error.WriteLine($"Error: Environment variable {keyEnv} not set");
                Environment.Exit(1);
            }
            return envValue;
        }

        return null;
    }
}

