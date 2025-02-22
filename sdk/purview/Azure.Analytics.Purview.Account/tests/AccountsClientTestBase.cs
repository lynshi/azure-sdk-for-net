﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Azure.Core.Pipeline;
using Azure.Core.TestFramework;

namespace Azure.Analytics.Purview.Account.Tests
{
    public class AccountsClientTestBase : RecordedTestBase<PurviewAccountTestEnvironment>
    {
        public AccountsClientTestBase(bool isAsync) : base(isAsync)
        {
        }

        public AccountsClientTestBase(bool isAsync, RecordedTestMode mode) : base(isAsync, mode)
        {
        }

        public PurviewAccountClient GetAccountClient()
        {
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) =>
            {
                return true;
            };
            var options = new PurviewAccountClientOptions { Transport = new HttpClientTransport(httpHandler) };
            var client = InstrumentClient(
                new PurviewAccountClient(TestEnvironment.Endpoint, TestEnvironment.Credential, InstrumentClientOptions(options)));
            return client;
        }
    }
}
