using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    public class AllSettingsModelsTests
    {
        public class AzureADSettingsTests
        {
            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new AzureADSettings
                {
                    Instance = "https://login.microsoftonline.com/",
                    Domain = "test.onmicrosoft.com",
                    TenantId = "test-tenant",
                    ClientId = "test-client",
                    CallbackPath = "/signin-oidc",
                    SignedOutCallbackPath = "/signout-callback-oidc",
                    Authority = "https://login.microsoftonline.com/test-tenant",
                    ClientCredentials = new List<ClientCredential>()
                };

                // Assert
                settings.Instance.Should().Be("https://login.microsoftonline.com/");
                settings.Domain.Should().Be("test.onmicrosoft.com");
                settings.TenantId.Should().Be("test-tenant");
            }

            [Fact]
            public void ClientCredentials_CanBeEmpty()
            {
                // Act
                var settings = new AzureADSettings
                {
                    Authority = "https://login.microsoftonline.com/test-tenant",
                    Instance = "https://login.microsoftonline.com/",
                    Domain = "test.onmicrosoft.com",
                    TenantId = "test-tenant",
                    ClientId = "test-client",
                    ClientCredentials = new List<ClientCredential>()
                };

                // Assert
                settings.ClientCredentials.Should().NotBeNull();
                settings.ClientCredentials.Should().BeEmpty();
            }
        }

        public class ClientCredentialTests
        {
            [Fact]
            public void Properties_CanBeSet()
            {
                // Act
                var credential = new ClientCredential
                {
                    SourceType = "ClientSecret",
                    ClientSecret = "test-secret"
                };

                // Assert
                credential.SourceType.Should().Be("ClientSecret");
                credential.ClientSecret.Should().Be("test-secret");
            }
        }

        public class BlobSettingsTests
        {
            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new BlobSettings
                {
                    BlobConnectionString = "test-connection",
                    MaxAttachments = 10
                };

                // Assert
                settings.BlobConnectionString.Should().Be("test-connection");
                settings.MaxAttachments.Should().Be(10);
            }

            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            [InlineData(100)]
            public void MaxAttachments_AcceptsValidValues(int maxAttachments)
            {
                // Act
                var settings = new BlobSettings { MaxAttachments = maxAttachments };

                // Assert
                settings.MaxAttachments.Should().Be(maxAttachments);
            }
        }

        public class CosmosDbSettingsTests
        {
            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new CosmosDbSettings
                {
                    AccountEndpoint = "https://test.documents.azure.com:443/",
                    AccountKey = "test-key",
                    DatabaseName = "TestDb",
                    ContainerName = "TestContainer",
                    CosmosConnectionString = "AccountEndpoint=test",
                    CommonPartitionKey = "test-partition"
                };

                // Assert
                settings.AccountEndpoint.Should().Be("https://test.documents.azure.com:443/");
                settings.DatabaseName.Should().Be("TestDb");
                settings.ContainerName.Should().Be("TestContainer");
            }
        }

        public class KeyVaultSettingsTests
        {
            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new KeyVaultSettings
                {
                    KeyVaultURL = "https://test-vault.vault.azure.net/",
                    KeyVaultSecret = "test-secret",
                    KeyVaultPassName = "test-cert"
                };

                // Assert
                settings.KeyVaultURL.Should().Be("https://test-vault.vault.azure.net/");
                settings.KeyVaultSecret.Should().Be("test-secret");
                settings.KeyVaultPassName.Should().Be("test-cert");
            }
        }

        public class NonceEncryptionSettingsTests
        {
            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new NonceEncryptionSettings
                {
                    Key = "test-key",
                    IV = "test-iv",
                    KeyVaultURL = "https://test-vault.vault.azure.net/",
                    IVSecret = "iv-secret",
                    NonceKeySecret = "nonce-key-secret"
                };

                // Assert
                settings.Key.Should().Be("test-key");
                settings.IV.Should().Be("test-iv");
                settings.KeyVaultURL.Should().Be("https://test-vault.vault.azure.net/");
            }
        }

        public class CSPScriptHashSettingsTests
        {
            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new CSPScriptHashSettings
                {
                    ManuallyCalculatedInlineHash1 = "sha256-abc123",
                    ManuallyCalculatedInlineHash2 = "sha256-def456"
                };

                // Assert
                settings.ManuallyCalculatedInlineHash1.Should().Be("sha256-abc123");
                settings.ManuallyCalculatedInlineHash2.Should().Be("sha256-def456");
            }

            [Fact]
            public void Hashes_CanBeNull()
            {
                // Act
                var settings = new CSPScriptHashSettings();

                // Assert
                settings.ManuallyCalculatedInlineHash1.Should().BeNull();
                settings.ManuallyCalculatedInlineHash2.Should().BeNull();
            }
        }

        public class MtlsSettingsTests
        {
            [Fact]
            public void DefaultValues_AreSetCorrectly()
            {
                // Act
                var settings = new MtlsSettings();

                // Assert
                settings.RequireClientCertificate.Should().BeTrue();
                settings.AllowCertificateChains.Should().BeTrue();
                settings.AllowSelfSignedCertificates.Should().BeFalse();
                settings.CheckCertificateRevocation.Should().BeTrue();
                settings.ValidateClientCertificateIssuer.Should().BeTrue();
                settings.ClientCertificateName.Should().BeNull();
            }

            [Fact]
            public void AllProperties_CanBeSet()
            {
                // Act
                var settings = new MtlsSettings
                {
                    RequireClientCertificate = false,
                    AllowCertificateChains = false,
                    AllowSelfSignedCertificates = true,
                    CheckCertificateRevocation = true,
                    ClientCertificateName = "my-client-cert",
                    ValidateClientCertificateIssuer = false
                };

                // Assert
                settings.RequireClientCertificate.Should().BeFalse();
                settings.AllowCertificateChains.Should().BeFalse();
                settings.AllowSelfSignedCertificates.Should().BeTrue();
                settings.CheckCertificateRevocation.Should().BeTrue();
                settings.ClientCertificateName.Should().Be("my-client-cert");
                settings.ValidateClientCertificateIssuer.Should().BeFalse();
            }

            [Fact]
            public void ClientCertificateName_CanBeNull()
            {
                // Act
                var settings = new MtlsSettings
                {
                    ClientCertificateName = null
                };

                // Assert
                settings.ClientCertificateName.Should().BeNull();
            }
        }

        public class AwsCognitoSettingsTests
        {
            [Fact]
            public void AllRequiredProperties_CanBeSet()
            {
                var settings = new AwsCognitoSettings
                {
                    Region = "us-east-1",
                    UserPoolId = "us-east-1_AbCdEfGhI",
                    AppClientId = "1abc2def3ghi4jkl5mno6pqr7",
                    AppClientSecret = "secret123",
                    Domain = "my-app.auth.us-east-1.amazoncognito.com"
                };

                settings.Region.Should().Be("us-east-1");
                settings.UserPoolId.Should().Be("us-east-1_AbCdEfGhI");
                settings.AppClientId.Should().Be("1abc2def3ghi4jkl5mno6pqr7");
                settings.AppClientSecret.Should().Be("secret123");
                settings.Domain.Should().Be("my-app.auth.us-east-1.amazoncognito.com");
            }

            [Fact]
            public void DefaultCallbackPath_ShouldBeSigninAwsCognito()
            {
                var settings = new AwsCognitoSettings
                {
                    Region = "us-east-1",
                    UserPoolId = "us-east-1_Pool",
                    AppClientId = "client-id",
                    AppClientSecret = "secret",
                    Domain = "app.auth.us-east-1.amazoncognito.com"
                };

                settings.CallbackPath.Should().Be("/signin-aws-cognito");
                settings.SignedOutCallbackPath.Should().Be("/signout-aws-cognito");
            }

            [Fact]
            public void CallbackPath_CanBeOverridden()
            {
                var settings = new AwsCognitoSettings
                {
                    Region = "us-east-1",
                    UserPoolId = "us-east-1_Pool",
                    AppClientId = "client-id",
                    AppClientSecret = "secret",
                    Domain = "app.auth.us-east-1.amazoncognito.com",
                    CallbackPath = "/custom-callback",
                    SignedOutCallbackPath = "/custom-signout"
                };

                settings.CallbackPath.Should().Be("/custom-callback");
                settings.SignedOutCallbackPath.Should().Be("/custom-signout");
            }
        }

        public class GcpIdentitySettingsTests
        {
            [Fact]
            public void AllRequiredProperties_CanBeSet()
            {
                var settings = new GcpIdentitySettings
                {
                    ClientId = "123456789-abcdefghij.apps.googleusercontent.com",
                    ClientSecret = "gcp-secret"
                };

                settings.ClientId.Should().Be("123456789-abcdefghij.apps.googleusercontent.com");
                settings.ClientSecret.Should().Be("gcp-secret");
            }

            [Fact]
            public void DefaultCallbackPath_ShouldBeSigninGcp()
            {
                var settings = new GcpIdentitySettings
                {
                    ClientId = "client-id",
                    ClientSecret = "secret"
                };

                settings.CallbackPath.Should().Be("/signin-gcp");
                settings.SignedOutCallbackPath.Should().Be("/signout-gcp");
            }

            [Fact]
            public void ProjectId_DefaultsToEmpty()
            {
                var settings = new GcpIdentitySettings
                {
                    ClientId = "client-id",
                    ClientSecret = "secret"
                };

                settings.ProjectId.Should().BeEmpty();
            }

            [Fact]
            public void ProjectId_CanBeSet()
            {
                var settings = new GcpIdentitySettings
                {
                    ClientId = "client-id",
                    ClientSecret = "secret",
                    ProjectId = "my-project-123456"
                };

                settings.ProjectId.Should().Be("my-project-123456");
            }

            [Fact]
            public void CallbackPath_CanBeOverridden()
            {
                var settings = new GcpIdentitySettings
                {
                    ClientId = "client-id",
                    ClientSecret = "secret",
                    CallbackPath = "/my-gcp-callback",
                    SignedOutCallbackPath = "/my-gcp-signout"
                };

                settings.CallbackPath.Should().Be("/my-gcp-callback");
                settings.SignedOutCallbackPath.Should().Be("/my-gcp-signout");
            }
        }
    }
}
