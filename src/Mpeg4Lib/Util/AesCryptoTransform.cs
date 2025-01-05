using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Mpeg4Lib.Util;

public delegate ValueTask<int> TransformDelegate(ReadOnlyMemory<byte> input, Memory<byte> output);

public interface IAesCryptoTransform
{
    //TransformDelegate Transform { get; }
    TransformDelegate TransformFinal { get; }

    void Dispose();
}

public class AesCryptoTransform : IDisposable, IAesCryptoTransform
{
    public TransformDelegate Transform { get; }
    public TransformDelegate TransformFinal { get; }

    private delegate int TransformDelegateInternal(ReadOnlySpan<byte> input, Span<byte> output);

    private Aes Aes { get; }
    private ICryptoTransform AesTransform { get; }

    public AesCryptoTransform(byte[] key, byte[] iv)
    {
        Aes = Aes.Create();
        Aes.Mode = CipherMode.CBC;
        Aes.Padding = PaddingMode.None;
        AesTransform = Aes.CreateDecryptor(key, iv);

        object basicSymmetricCipherBCrypt =
            AesTransform
            .GetType()
            .GetProperty("BasicSymmetricCipher", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(AesTransform);

        Type bCryptType = basicSymmetricCipherBCrypt.GetType();
        Type[] methodSig = new Type[] { typeof(ReadOnlySpan<byte>), typeof(Span<byte>) };

        var transform =
            bCryptType
            .GetMethod("Transform", methodSig)
            .CreateDelegate<TransformDelegateInternal>(basicSymmetricCipherBCrypt);

        var transformFinal =
            bCryptType
            .GetMethod("TransformFinal", methodSig)
            .CreateDelegate<TransformDelegateInternal>(basicSymmetricCipherBCrypt);

        Transform = (input, output) =>
        {
            var debugData = new DebugData
            {
                Key = key.ToArray(),
                Iv = iv.ToArray(),
                EncryptedData = input.Span.ToArray()
            };

            int result = transform(input.Span, output.Span);
            debugData.DecryptedData = output.ToArray();

            debugData.SaveTo("DebugData\\Transform-" + debugData.GetHashCode().ToString() + ".json");

            return new ValueTask<int>(result);
        };

        TransformFinal = (input, output) =>
        {

            var debugData = new DebugData
            {
                Key = key.ToArray(),
                Iv = iv.ToArray(),
                EncryptedData = input.Span.Slice(Math.Min(input.Length, output.Length)).ToArray()
            };


            int result = transformFinal(input.Span, output.Span);

            debugData.DecryptedData = output.Slice(0, Math.Min(input.Length, output.Length)).ToArray();

            debugData.SaveTo("DebugData\\TransformFinal-" + debugData.GetHashCode().ToString() + ".json");

            return new ValueTask<int>(result);
        };

    }

    public void Dispose()
    {
        Aes.Dispose();
        AesTransform.Dispose();
    }
}


public class AesCryptoTransformFactory
{
    public static Func<byte[], byte[], IAesCryptoTransform> Create { get; set; } 
        = (key, iv) => new AesCryptoTransform(key, iv);
}
