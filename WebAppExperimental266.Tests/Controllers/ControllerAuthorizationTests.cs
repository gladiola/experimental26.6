using Microsoft.AspNetCore.Authorization;
using WebAppExperimental266.Controllers;

namespace WebAppExperimental266.Tests.Controllers
{
    public class ControllerAuthorizationTests
    {
        [Fact]
        public void RecordsController_HasAuthorizeAttribute()
        {
            var attribute = typeof(RecordsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            attribute.Should().NotBeNull();
            attribute!.Policy.Should().BeNull();
        }

        [Fact]
        public void AdminRecordsController_UsesAdminCertificatePolicy()
        {
            var attribute = typeof(AdminRecordsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            attribute.Should().NotBeNull();
            attribute!.Policy.Should().Be("AdminCertificate");
        }
    }
}
