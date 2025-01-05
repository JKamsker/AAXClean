using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Mpeg4Lib.Util
{
    public interface ICryptoProvider
    {
        Task DecryptInPlace(byte[] key, byte[] iv, byte[] encryptedBlocks);
        Task<byte[]> Sha1(params (byte[] bytes, int start, int length)[] blocks);
    }

    public class DefaultCryptoProvider : ICryptoProvider
    {
        public static readonly ICryptoProvider Instance = new DefaultCryptoProvider();

        public Task DecryptInPlace(byte[] key, byte[] iv, byte[] encryptedBlocks)
        {
          

            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            
            

            using var cbcDecryptor = aes.CreateDecryptor(key, iv);
            
            //C:\Users\JKamsker\Documents\repos
            //C:\Users\JKamsker\repos\RiderProjects\AAXConverterWeb\AAXConverter.Consoletest\WasmTests.cs

            //var inputCount = encryptedBlocks.Length & 0x7ffffff0;
            var length = encryptedBlocks.Length;
            var inputCount = length - (length % 16); // nearest multiple of 16

            var debugData = new DebugData
            {
                Key = key.ToArray(),
                Iv = iv.ToArray(),
                EncryptedData = encryptedBlocks
                    .Take(inputCount)
                    .ToArray()
            };
            
            cbcDecryptor.TransformBlock
            (
                 inputBuffer: encryptedBlocks,
                 inputOffset: 0,
                 inputCount: inputCount,
                 outputBuffer: encryptedBlocks,
                 outputOffset: 0
            );

            debugData.DecryptedData = encryptedBlocks
                .Take(inputCount)
                .ToArray();

            Directory.CreateDirectory("DebugData");
            debugData.SaveTo("DebugData\\DecryptInPlace-" + debugData.GetHashCode().ToString() + ".json");


            return Task.CompletedTask;
        }


        public Task<byte[]> Sha1(params (byte[] bytes, int start, int length)[] blocks)
        {
            using SHA1 sha = SHA1.Create();
            int i = 0;
            for (; i < blocks.Length - 1; i++)
            {
                sha.TransformBlock(blocks[i].bytes, blocks[i].start, blocks[i].length, null, 0);
            }
            sha.TransformFinalBlock(blocks[i].bytes, blocks[i].start, blocks[i].length);
            return Task.FromResult(sha.Hash);
        }
    }

    public class Crypto
    {
        public static ICryptoProvider Provider { get; set; } = DefaultCryptoProvider.Instance;

        public static Task DecryptInPlace(byte[] key, byte[] iv, byte[] encryptedBlocks)
            => Provider.DecryptInPlace(key, iv, encryptedBlocks);

        public static Task<byte[]> Sha1(params (byte[] bytes, int start, int length)[] blocks)
            => Provider.Sha1(blocks);
    }
}
