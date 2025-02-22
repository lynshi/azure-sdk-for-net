﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.TestFramework;
using Azure.Identity.Tests.Mock;
using NUnit.Framework;

namespace Azure.Identity.Tests
{
    public class ManagedIdentityCredentialTests : ClientTestBase
    {
        public ManagedIdentityCredentialTests(bool isAsync) : base(isAsync)
        {
        }

        private const string ExpectedToken = "mock-msi-access-token";

        [NonParallelizable]
        [Test]
        public async Task VerifyImdsRequestWithClientIdMockAsync()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };
            var pipeline = CredentialPipeline.GetInstance(options);

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential("mock-client-id", pipeline));

            AccessToken actualToken = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.Requests[0];

            string query = request.Uri.Query;

            Assert.AreEqual(request.Uri.Host, "169.254.169.254");
            Assert.AreEqual(request.Uri.Path, "/metadata/identity/oauth2/token");
            Assert.IsTrue(query.Contains("api-version=2018-02-01"));
            Assert.IsTrue(query.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            Assert.IsTrue(request.Headers.TryGetValue("Metadata", out string metadataValue));
            Assert.IsTrue(query.Contains($"client_id=mock-client-id"));
            Assert.AreEqual("true", metadataValue);
        }

        [NonParallelizable]
        [Test]
        public void VerifyImdsRequestFailurePopulatesExceptionMessage()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var expectedMessage = "No MSI found for specified ClientId/ResourceId.";
            var response = CreateErrorMockResponse(400, expectedMessage);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };
            var pipeline = CredentialPipeline.GetInstance(options);

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential("mock-client-id", pipeline));

            var ex = Assert.ThrowsAsync<CredentialUnavailableException>(async () => await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default)));
            Assert.That(ex.Message, Does.Contain(expectedMessage));
        }

        [NonParallelizable]
        [Test]
        [TestCase(400, ImdsManagedIdentitySource.IdentityUnavailableError)]
        [TestCase(502, ImdsManagedIdentitySource.GatewayError)]
        public void VerifyImdsRequestHandlesFailedRequestWithCredentialUnavailableExceptionMockAsync(int responseCode, string expectedMessage)
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(responseCode, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };
            var pipeline = CredentialPipeline.GetInstance(options);

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential("mock-client-id", pipeline));

            var ex = Assert.ThrowsAsync<CredentialUnavailableException>(async () => await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default)));

            Assert.That(ex.Message, Does.Contain(expectedMessage));
        }

        [NonParallelizable]
        [Test]
        [TestCase(null)]
        [TestCase("mock-client-id")]
        public async Task VerifyIMDSRequestWithPodIdentityEnvVarMockAsync(string clientId)
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", "https://mock.podid.endpoint/" } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };
            var pipeline = CredentialPipeline.GetInstance(options);

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential(clientId, pipeline));

            AccessToken actualToken = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.SingleRequest;

            Assert.IsTrue(request.Uri.ToString().StartsWith("https://mock.podid.endpoint" + ImdsManagedIdentitySource.imddsTokenPath));

            string query = request.Uri.Query;

            Assert.That(query, Does.Contain("api-version=2018-02-01"));
            Assert.That(query, Does.Contain($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            if (clientId != null)
            {
                Assert.That(query, Does.Contain($"client_id=mock-client-id"));
            }
            Assert.IsTrue(request.Headers.TryGetValue("Metadata", out string actMetadataValue));
            Assert.AreEqual("true", actMetadataValue);
        }

        [NonParallelizable]
        [Test]
        [TestCase(null)]
        [TestCase("mock-client-id")]
        public async Task VerifyAppService2017RequestWithClientIdMockAsync(string clientId)
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", "https://mock.msi.endpoint/" }, { "MSI_SECRET", "mock-msi-secret" }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential(clientId, options));

            AccessToken actualToken = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.SingleRequest;

            Assert.IsTrue(request.Uri.ToString().StartsWith("https://mock.msi.endpoint/"));

            string query = request.Uri.Query;

            Assert.IsTrue(query.Contains("api-version=2017-09-01"));
            Assert.IsTrue(query.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            if (clientId != null)
            {
                Assert.IsTrue(query.Contains($"clientid=mock-client-id"));
            }
            Assert.IsTrue(request.Headers.TryGetValue("secret", out string actSecretValue));
            Assert.AreEqual("mock-msi-secret", actSecretValue);
        }

        [NonParallelizable]
        [Ignore("This test is disabled as we have disabled AppService MI version 2019-08-01 due to issue https://github.com/Azure/azure-sdk-for-net/issues/16278. The test should be re-enabled if and when support for this api version is added back")]
        [Test]
        public async Task VerifyAppService2019RequestMockAsync()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", "https://identity.endpoint/" }, { "IDENTITY_HEADER", "mock-identity-header" }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions { Transport = mockTransport };

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential(options: options));

            AccessToken token = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, token.Token);

            MockRequest request = mockTransport.Requests[0];
            Assert.IsTrue(request.Uri.ToString().StartsWith(EnvironmentVariables.IdentityEndpoint));

            string query = request.Uri.Query;
            Assert.IsTrue(query.Contains("api-version=2019-08-01"));
            Assert.IsTrue(query.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            Assert.IsTrue(request.Headers.TryGetValue("X-IDENTITY-HEADER", out string identityHeader));

            Assert.AreEqual(EnvironmentVariables.IdentityHeader, identityHeader);
        }

        [NonParallelizable]
        [Test]
        // This test has been added to ensure as we have disabled AppService MI version 2019-08-01 due to issue https://github.com/Azure/azure-sdk-for-net/issues/16278.
        // The test should be removed if and when support for this api version is added back
        public async Task VerifyAppService2017RequestWith2019EnvVarsMockAsync()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", "https://mock.msi.endpoint/" }, { "MSI_SECRET", "mock-msi-secret" }, { "IDENTITY_ENDPOINT", "https://identity.endpoint/" }, { "IDENTITY_HEADER", "mock-identity-header" }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential(options: options));

            AccessToken actualToken = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.Requests[0];

            Assert.IsTrue(request.Uri.ToString().StartsWith("https://mock.msi.endpoint/"));

            string query = request.Uri.Query;

            Assert.IsTrue(query.Contains("api-version=2017-09-01"));
            Assert.IsTrue(query.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            Assert.IsTrue(request.Headers.TryGetValue("secret", out string actSecretValue));
            Assert.AreEqual("mock-msi-secret", actSecretValue);
        }

        [NonParallelizable]
        [Ignore("This test is disabled as we have disabled AppService MI version 2019-08-01 due to issue https://github.com/Azure/azure-sdk-for-net/issues/16278. The test should be re-enabled if and when support for this api version is added back")]
        [Test]
        public async Task VerifyAppService2019RequestWithClientIdMockAsync()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", "https://identity.endpoint/" }, { "IDENTITY_HEADER", "mock-identity-header" }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions { Transport = mockTransport };

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential("mock-client-id", options));
            AccessToken actualToken = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.SingleRequest;
            Assert.IsTrue(request.Uri.ToString().StartsWith(EnvironmentVariables.IdentityEndpoint));

            string query = request.Uri.Query;
            Assert.IsTrue(query.Contains("api-version=2019-08-01"));
            Assert.IsTrue(query.Contains("client_id=mock-client-id"));
            Assert.IsTrue(query.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            Assert.IsTrue(request.Headers.TryGetValue("X-IDENTITY-HEADER", out string identityHeader));
            Assert.AreEqual(EnvironmentVariables.IdentityHeader, identityHeader);
        }

        [NonParallelizable]
        [Test]
        public async Task VerifyCloudShellMsiRequestMockAsync()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", "https://mock.msi.endpoint/" }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };

            ManagedIdentityCredential credential = InstrumentClient(new ManagedIdentityCredential(options: options));
            AccessToken actualToken = await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.Requests[0];

            Assert.IsTrue(request.Uri.ToString().StartsWith("https://mock.msi.endpoint/"));
            Assert.IsTrue(request.Content.TryComputeLength(out long contentLen));

            var content = new byte[contentLen];
            MemoryStream contentBuff = new MemoryStream(content);
            request.Content.WriteTo(contentBuff, default);
            string body = Encoding.UTF8.GetString(content);

            Assert.IsTrue(body.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            Assert.IsTrue(request.Headers.TryGetValue("Metadata", out string actMetadata));
            Assert.AreEqual("true", actMetadata);
        }

        [NonParallelizable]
        [Test]
        [TestCase(null)]
        [TestCase("mock-client-id")]
        public async Task VerifyCloudShellMsiRequestWithClientIdMockAsync(string clientId)
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", "https://mock.msi.endpoint/" }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var response = CreateMockResponse(200, ExpectedToken);
            var mockTransport = new MockTransport(response);
            var options = new TokenCredentialOptions() { Transport = mockTransport };

            ManagedIdentityCredential client = InstrumentClient(new ManagedIdentityCredential(clientId, options));

            AccessToken actualToken = await client.GetTokenAsync(new TokenRequestContext(MockScopes.Default));

            Assert.AreEqual(ExpectedToken, actualToken.Token);

            MockRequest request = mockTransport.Requests[0];

            Assert.IsTrue(request.Uri.ToString().StartsWith("https://mock.msi.endpoint/"));
            Assert.IsTrue(request.Content.TryComputeLength(out long contentLen));

            var content = new byte[contentLen];
            MemoryStream contentBuff = new MemoryStream(content);
            request.Content.WriteTo(contentBuff, default);
            string body = Encoding.UTF8.GetString(content);

            Assert.IsTrue(body.Contains($"resource={Uri.EscapeDataString(ScopeUtilities.ScopesToResource(MockScopes.Default))}"));
            if (clientId != null)
            {
                Assert.IsTrue(body.Contains($"client_id=mock-client-id"));
            }
            Assert.IsTrue(request.Headers.TryGetValue("Metadata", out string actMetadata));
            Assert.AreEqual("true", actMetadata);
        }

        [NonParallelizable]
        [Test]
        public async Task VerifyMsiUnavailableOnIMDSAggregateExcpetion()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", "http://169.254.169.001/" } });

            // setting the delay to 1ms and retry mode to fixed to speed up test
            var options = new TokenCredentialOptions() { Retry = { Delay = TimeSpan.FromMilliseconds(1), Mode = RetryMode.Fixed } };

            var credential = InstrumentClient(new ManagedIdentityCredential(options: options));

            var ex = Assert.ThrowsAsync<CredentialUnavailableException>(async () => await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default)));

            Assert.That(ex.Message, Does.Contain(ImdsManagedIdentitySource.AggregateError));

            await Task.CompletedTask;
        }

        [NonParallelizable]
        [Test]
        public async Task VerifyMsiUnavailableOnIMDSRequestFailedExcpetion()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", "http://169.254.169.001/" } });

            var options = new TokenCredentialOptions() { Retry = { MaxRetries = 0 } };

            var credential = InstrumentClient(new ManagedIdentityCredential(options: options));

            var ex = Assert.ThrowsAsync<CredentialUnavailableException>(async () => await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default)));

            Assert.That(ex.Message, Does.Contain(ImdsManagedIdentitySource.NoResponseError));

            await Task.CompletedTask;
        }

        [Test]
        public async Task VerifyClientAuthenticateThrows()
        {
            using var environment = new TestEnvVar(new() { { "MSI_ENDPOINT", null }, { "MSI_SECRET", null }, { "IDENTITY_ENDPOINT", null }, { "IDENTITY_HEADER", null }, { "AZURE_POD_IDENTITY_AUTHORITY_HOST", null } });

            var mockClient = new MockManagedIdentityClient(CredentialPipeline.GetInstance(null)) { TokenFactory = () => throw new MockClientException("message") };

            var credential = InstrumentClient(new ManagedIdentityCredential(mockClient));

            var ex = Assert.ThrowsAsync<AuthenticationFailedException>(async () => await credential.GetTokenAsync(new TokenRequestContext(MockScopes.Default)));

            Assert.IsInstanceOf(typeof(MockClientException), ex.InnerException);

            await Task.CompletedTask;
        }

        private MockResponse CreateMockResponse(int responseCode, string token)
        {
            var response = new MockResponse(responseCode);
            response.SetContent($"{{ \"access_token\": \"{token}\", \"expires_on\": \"3600\" }}");
            return response;
        }

        private MockResponse CreateErrorMockResponse(int responseCode, string message)
        {
            var response = new MockResponse(responseCode);
            response.SetContent($"{{\"StatusCode\":400,\"Message\":\"{message}\",\"CorrelationId\":\"f3c9aec0-7fa2-4184-ad0f-0c68ce5fc748\"}}");
            return response;
        }
    }
}
