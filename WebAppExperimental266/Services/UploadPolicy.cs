using Microsoft.AspNetCore.Http;

namespace WebAppExperimental266.Services
{
    public static class UploadPolicy
    {
        public const long MaxUploadBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> CardTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "MIFARE Classic",
            "HITAG",
            "iCLASS",
            "T55x7"
        };

        public static string[] SupportedCardTypes => CardTypes.ToArray();

        public static bool IsSupportedCardType(string? cardType)
        {
            return !string.IsNullOrWhiteSpace(cardType) && CardTypes.Contains(cardType);
        }

        public static async Task<byte[]> ReadFileBytesAsync(IFormFile uploadFile, CancellationToken cancellationToken = default)
        {
            var length = (int)uploadFile.Length;
            var buffer = new byte[length];
            await using var stream = uploadFile.OpenReadStream();
            var totalRead = 0;
            while (totalRead < length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, length - totalRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            if (totalRead == length)
            {
                return buffer;
            }

            return buffer[..totalRead];
        }
    }
}
