﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Azure.Core.TestFramework;
using Azure.ResourceManager.Resources.Models;
using NUnit.Framework;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Azure.ResourceManager.Resources.Tests
{
    public class DeploymentExtendedOperationsTests : ResourcesTestBase
    {
        public DeploymentExtendedOperationsTests(bool isAsync)
            : base(isAsync)//, RecordedTestMode.Record)
        {
        }

        [TestCase]
        [RecordedTest]
        public async Task Delete()
        {
            string rgName = Recording.GenerateAssetName("testRg-4-");
            ResourceGroupData rgData = new ResourceGroupData(Location.WestUS2);
            var lro = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(rgName, rgData);
            ResourceGroup rg = lro.Value;
            string deployExName = Recording.GenerateAssetName("deployEx-D-");
            Deployment deploymentExtendedData = CreateDeploymentExtendedData(CreateDeploymentProperties());
            DeploymentExtended deploymentExtended = (await rg.GetDeploymentExtendeds().CreateOrUpdateAsync(deployExName, deploymentExtendedData)).Value;
            await deploymentExtended.DeleteAsync();
            var ex = Assert.ThrowsAsync<RequestFailedException>(async () => await deploymentExtended.GetAsync());
            Assert.AreEqual(404, ex.Status);
        }

        private static DeploymentProperties CreateDeploymentProperties()
        {
            DeploymentProperties tmpDeploymentProperties = new DeploymentProperties(DeploymentMode.Incremental);
            tmpDeploymentProperties.TemplateLink = new TemplateLink();
            tmpDeploymentProperties.TemplateLink.Uri = "https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/quickstarts/microsoft.storage/storage-account-create/azuredeploy.json";
            tmpDeploymentProperties.Parameters = new JsonObject()
            {
                {"storageAccountType", new JsonObject()
                    {
                        {"value", "Standard_GRS" }
                    }
                }
            };
            return tmpDeploymentProperties;
        }
        private static Deployment CreateDeploymentExtendedData(DeploymentProperties deploymentProperties) => new Deployment(deploymentProperties);
    }
}
