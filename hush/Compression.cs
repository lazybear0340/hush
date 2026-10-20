using Zstandard.Net;
using System.IO.Compression;

static class Compression
{
    public static byte[] CompressIfUseful(byte[] data)
    {
        if (data.Length == 0)
            return data;

        var compressed = Compress(data);
        return compressed.Length < data.Length ? compressed : data;
    }

    public static byte[] Compress(byte[] data)
    {
        var output = new MemoryStream();
        using (var compressor = new ZstandardStream(output, CompressionMode.Compress))
        {
            compressor.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var decompressor = new ZstandardStream(input, CompressionMode.Decompress);
        var output = new MemoryStream();
        decompressor.CopyTo(output);
        return output.ToArray();
    }
}

