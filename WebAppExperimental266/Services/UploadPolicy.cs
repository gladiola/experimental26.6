using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace WebAppExperimental266.Services
{
    public static class UploadPolicy
    {
        public const long MaxUploadBytes = 10 * 1024 * 1024;
        public const string JsonContentType = "application/json";

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

        public static async Task<UploadValidationResult> ValidateAndReadJsonUploadAsync(
            IFormFile? uploadFile,
            CancellationToken cancellationToken = default)
        {
            if (uploadFile is null)
            {
                return UploadValidationResult.Invalid("Please choose a JSON file to upload.");
            }

            if (!string.Equals(Path.GetExtension(uploadFile.FileName), ".json", StringComparison.OrdinalIgnoreCase))
            {
                return UploadValidationResult.Invalid("Only .json files are supported.");
            }

            if (uploadFile.Length <= 0)
            {
                return UploadValidationResult.Invalid("The selected file is empty.");
            }

            if (uploadFile.Length > MaxUploadBytes)
            {
                return UploadValidationResult.Invalid($"Files larger than {MaxUploadBytes / (1024 * 1024)} MB are not allowed.");
            }

            var uploadedBytes = await ReadFileBytesAsync(uploadFile, cancellationToken);
            try
            {
                using var jsonDocument = JsonDocument.Parse(uploadedBytes);
            }
            catch (JsonException)
            {
                return UploadValidationResult.Invalid("The selected file must contain valid JSON.");
            }

            return UploadValidationResult.Valid(
                uploadedBytes,
                GetSafeDownloadFileName(uploadFile.FileName),
                JsonContentType,
                uploadFile.Length);
        }

        public static string GetSafeDownloadContentType(string? uploadedContentType)
        {
            return JsonContentType;
        }

        public static string GetSafeDownloadFileName(string? uploadedFileName)
        {
            var fileName = string.IsNullOrWhiteSpace(uploadedFileName)
                ? "upload.json"
                : Path.GetFileName(uploadedFileName)
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Trim();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "upload.json";
            }

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            return fileName;
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

    public sealed class UploadValidationResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }
        public byte[]? UploadedBytes { get; init; }
        public string? UploadedFileName { get; init; }
        public string UploadedContentType { get; init; } = UploadPolicy.JsonContentType;
        public long? UploadedFileSize { get; init; }

        public static UploadValidationResult Invalid(string errorMessage)
        {
            return new UploadValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }

        public static UploadValidationResult Valid(byte[] uploadedBytes, string uploadedFileName, string uploadedContentType, long uploadedFileSize)
        {
            return new UploadValidationResult
            {
                IsValid = true,
                UploadedBytes = uploadedBytes,
                UploadedFileName = uploadedFileName,
                UploadedContentType = uploadedContentType,
                UploadedFileSize = uploadedFileSize
            };
        }
    }
}
