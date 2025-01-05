using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mpeg4Lib.Util
{
    public class DebugData
    {
        public byte[] Key { get; set; }
        public byte[] Iv { get; set; }

        public byte[] DecryptedData { get; set; }
        public byte[] EncryptedData { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Iv, DecryptedData, EncryptedData);
        }

        public void SaveTo(string path)
        {
            var json = System.Text.Json.JsonSerializer.Serialize
            (
                this, 
                options: new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
            System.IO.File.WriteAllText(path, json);
        }
    }
}
