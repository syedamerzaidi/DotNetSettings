using System.Security.Cryptography;

namespace DotNetSettings.Encryption;

/// <summary>
/// Encrypts and decrypts setting values using AES-256-GCM.
/// Wire up via <c>options.UseEncryption(base64Key)</c> in <c>AddDotNetSettings()</c>.
/// </summary>
public sealed class SettingsEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public SettingsEncryptor(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes (256 bits).", nameof(key));
        _key = key;
    }

    /// <summary>Encrypts <paramref name="plaintext"/> and returns a base64-encoded blob.</summary>
    public string Encrypt(string plaintext)
    {
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

#if NET8_0_OR_GREATER
        using var aes = new AesGcm(_key, TagSize);
#else
        using var aes = new AesGcm(_key);
#endif
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var blob = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(blob, 0);
        ciphertext.CopyTo(blob, NonceSize);
        tag.CopyTo(blob, NonceSize + ciphertext.Length);

        return Convert.ToBase64String(blob);
    }

    /// <summary>Decrypts a base64-encoded blob produced by <see cref="Encrypt"/> and returns plaintext.</summary>
    public string Decrypt(string ciphertextBase64)
    {
        var blob = Convert.FromBase64String(ciphertextBase64);
        if (blob.Length < NonceSize + TagSize)
            throw new CryptographicException("Invalid ciphertext blob.");

        var nonce = blob[..NonceSize];
        var tag = blob[^TagSize..];
        var ciphertext = blob[NonceSize..^TagSize];
        var plaintext = new byte[ciphertext.Length];

#if NET8_0_OR_GREATER
        using var aes = new AesGcm(_key, TagSize);
#else
        using var aes = new AesGcm(_key);
#endif
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }
}
