package LanChat.Network.Datagrams;

import java.nio.charset.StandardCharsets;
import java.security.*;
import java.util.*;
import javax.crypto.*;
import javax.crypto.spec.*;
import LanChat.Network.Json;

public final class PrivateMessageDatagram extends MessageDatagram {
    public String nonce = "";
    public String tag = "";
    public String getType() { return "PrivateMessage"; }

    public static byte[] deriveKey(String passphrase) throws GeneralSecurityException {
        PBEKeySpec spec = new PBEKeySpec(passphrase.toCharArray(),
                "LanChat/v1/shared-key".getBytes(StandardCharsets.UTF_8), 600_000, 256);
        try {
            return SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).getEncoded();
        } finally {
            spec.clearPassword();
        }
    }

    public void encrypt(String plaintext, byte[] key) throws GeneralSecurityException {
        byte[] iv = new byte[12];
        new SecureRandom().nextBytes(iv);
        Cipher aes = cipher(Cipher.ENCRYPT_MODE, key, iv);
        byte[] encrypted = aes.doFinal(plaintext.getBytes(StandardCharsets.UTF_8));
        nonce = Base64.getEncoder().encodeToString(iv);
        tag = Base64.getEncoder().encodeToString(Arrays.copyOfRange(encrypted, encrypted.length - 16, encrypted.length));
        message = Base64.getEncoder().encodeToString(Arrays.copyOf(encrypted, encrypted.length - 16));
    }

    public String decrypt(byte[] key) throws GeneralSecurityException {
        byte[] iv = Base64.getDecoder().decode(nonce);
        byte[] authTag = Base64.getDecoder().decode(tag);
        byte[] ciphertext = Base64.getDecoder().decode(message);
        if (iv.length != 12 || authTag.length != 16) throw new GeneralSecurityException("Invalid nonce or tag.");
        byte[] encrypted = Arrays.copyOf(ciphertext, ciphertext.length + 16);
        System.arraycopy(authTag, 0, encrypted, ciphertext.length, 16);
        return new String(cipher(Cipher.DECRYPT_MODE, key, iv).doFinal(encrypted), StandardCharsets.UTF_8);
    }

    private Cipher cipher(int mode, byte[] key, byte[] iv) throws GeneralSecurityException {
        Cipher aes = Cipher.getInstance("AES/GCM/NoPadding");
        aes.init(mode, new SecretKeySpec(key, "AES"), new GCMParameterSpec(128, iv));
        // System.Text.Json's DateTimeOffset converter writes the offset's plus sign literally.
        String associatedData = Json.stringify(identity()).replace(
                "\"RegisteredAt\":\"" + registeredAt.replace("+", "\\u002B") + "\"",
                "\"RegisteredAt\":\"" + registeredAt + "\"");
        aes.updateAAD(associatedData.getBytes(StandardCharsets.UTF_8));
        return aes;
    }
}
