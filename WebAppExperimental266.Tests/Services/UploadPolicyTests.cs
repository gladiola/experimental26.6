using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Text;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class UploadPolicyTests
    {
        [Fact]
        public async Task ValidateAndReadJsonUploadAsync_RejectsInvalidJson()
        {
            var bytes = Encoding.UTF8.GetBytes("not-json");
            using var stream = new MemoryStream(bytes);
            IFormFile formFile = new FormFile(stream, 0, bytes.Length, "uploadFile", "payload.json")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/html"
            };

            var result = await UploadPolicy.ValidateAndReadJsonUploadAsync(formFile);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("valid JSON");
        }

        [Fact]
        public async Task ValidateAndReadJsonUploadAsync_NormalizesJsonUploadMetadata()
        {
            var bytes = Encoding.UTF8.GetBytes("{\"ok\":true}");
            using var stream = new MemoryStream(bytes);
            IFormFile formFile = new FormFile(stream, 0, bytes.Length, "uploadFile", "../payload.json")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/html"
            };

            var result = await UploadPolicy.ValidateAndReadJsonUploadAsync(formFile);

            result.IsValid.Should().BeTrue();
            result.UploadedContentType.Should().Be(UploadPolicy.JsonContentType);
            result.UploadedFileName.Should().Be("payload.json");
            result.UploadedBytes.Should().Equal(bytes);
        }

        [Theory]
        [InlineData(null, "upload.json")]
        [InlineData("", "upload.json")]
        [InlineData("report", "report.json")]
        [InlineData("../report.json", "report.json")]
        [InlineData("report.json\r\nbad", "report.jsonbad.json")]
        public void GetSafeDownloadFileName_ReturnsSanitizedJsonName(string? input, string expected)
        {
            UploadPolicy.GetSafeDownloadFileName(input).Should().Be(expected);
        }

        [Fact]
        public void GetSafeDownloadContentType_AlwaysReturnsJson()
        {
            UploadPolicy.GetSafeDownloadContentType("text/html").Should().Be(UploadPolicy.JsonContentType);
        }
    }
}
