using AAXClean;

namespace ConsoleApp1;

class Program
{
    static async Task Main(string[] args)
    {
        var filePath = @"C:\Users\JKamsker\Downloads\DasSchiffTachyon2_ep7.aax";

        var outputFilePath = Path.ChangeExtension(filePath, ".mp4");
        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
        }

        {
            using var inputStream = File.OpenRead(filePath);
            using var outputStream = File.OpenWrite(filePath + ".mp4");

            var aaxFile = new AaxFile(inputStream);
            var activationBytes = "9f786605";
            aaxFile.SetDecryptionKey(activationBytes);

            var convert = aaxFile.ConvertToMp4aAsync(outputStream);
            convert.ConversionProgressUpdate += (sender, args) =>
            {
                // Update the progress based on FractionCompleted
                // ConversionProgress = args.FractionCompleted * 100;
                // StateHasChanged();
                Console.WriteLine($"Progress: {args.FractionCompleted * 100}%");
            };

            await convert;
        }
    }
}
