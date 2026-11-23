using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LanChat.Network.Datagrams;

public sealed class PrivateMessageDatagram : MessageDatagram
{
    public override string Type => "PrivateMessage";
    public string Nonce { get; set; } = "";
    public string Tag { get; set; } = "";

    public static byte[] DeriveKey(string passphrase) => Rfc2898DeriveBytes.Pbkdf2(
        passphrase, Encoding.UTF8.GetBytes("LanChat/v1/shared-key"),
        600_000, HashAlgorithmName.SHA256, 32);

    public void Encrypt(string plaintext, byte[] key)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] bytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[bytes.Length];
        byte[] tag = new byte[16];
        using AesGcm aes = new(key, tag.Length);
        aes.Encrypt(nonce, bytes, ciphertext, tag, AssociatedData());
        Nonce = Convert.ToBase64String(nonce);
        Tag = Convert.ToBase64String(tag);
        Message = Convert.ToBase64String(ciphertext);
    }

    public string Decrypt(byte[] key)
    {
        byte[] nonce = Convert.FromBase64String(Nonce);
        byte[] ciphertext = Convert.FromBase64String(Message);
        byte[] tag = Convert.FromBase64String(Tag);
        if (nonce.Length != 12 || tag.Length != 16)
            throw new CryptographicException("Invalid AES-GCM nonce or tag.");
        byte[] plaintext = new byte[ciphertext.Length];
        using AesGcm aes = new(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData());
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] AssociatedData() => JsonSerializer.SerializeToUtf8Bytes(
        new { Type, SenderId, Name, RegisteredAt });
}
