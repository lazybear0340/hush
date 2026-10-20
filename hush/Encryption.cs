using System.Security.Cryptography;
using Konscious.Security.Cryptography;

static class Encryption
{
    const int KeySize = 32;
    const int NonceSize = 12;
    const int TagSize = 16;
    const int SaltSize = 16;

    const int ArgonMemory = 16384;
    const int ArgonIterations = 3;
    const int ArgonParallelism = 1;

    public static (byte[] encrypted, byte[] nonce, byte[] salt) Encrypt(byte[] data, string passphrase)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(passphrase, salt);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var encrypted = EncryptWithAes(data, key, nonce);
        return (encrypted, nonce, salt);
    }

    public static byte[] Decrypt(byte[] encrypted, string passphrase, byte[] nonce, byte[] salt)
    {
        var key = DeriveKey(passphrase, salt);
        return DecryptWithAes(encrypted, key, nonce);
    }

    static byte[] EncryptWithAes(byte[] data, byte[] key, byte[] nonce)
    {
        using var aes = new AesGcm(key);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        aes.Encrypt(nonce, data, ciphertext, tag);

        var result = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
        return result;
    }

    static byte[] DecryptWithAes(byte[] encrypted, byte[] key, byte[] nonce)
    {
        var ciphertextLength = encrypted.Length - TagSize;

        if (ciphertextLength <= 0)
            throw new CryptographicException("Encrypted data too short");

        var ciphertext = new byte[ciphertextLength];
        var tag = new byte[TagSize];

        Buffer.BlockCopy(encrypted, 0, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(encrypted, ciphertextLength, tag, 0, TagSize);

        var plaintext = new byte[ciphertextLength];

        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    static byte[] DeriveKey(string passphrase, byte[] salt)
    {
        var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(passphrase))
        {
            Salt = salt,
            DegreeOfParallelism = ArgonParallelism,
            MemorySize = ArgonMemory,
            Iterations = ArgonIterations
        };

        return argon2.GetBytes(KeySize);
    }
}
