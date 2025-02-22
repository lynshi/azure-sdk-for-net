﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.TestFramework;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network.Tests.Helpers;
using NUnit.Framework;

namespace Azure.ResourceManager.Network.Tests.Tests
{
    public class RouteTests : NetworkServiceClientTestBase
    {
        public RouteTests(bool isAsync) : base(isAsync)
        {
        }

        [SetUp]
        public void ClearChallengeCacheforRecord()
        {
            if (Mode == RecordedTestMode.Record || Mode == RecordedTestMode.Playback)
            {
                Initialize();
            }
        }

        [Test]
        [RecordedTest]
        public async Task RoutesApiTest()
        {
            string resourceGroupName = Recording.GenerateAssetName("csmrg");

            string location = TestEnvironment.Location;
            var resourceGroup = await CreateResourceGroup(resourceGroupName);
            string routeTableName = Recording.GenerateAssetName("azsmnet");
            string route1Name = Recording.GenerateAssetName("azsmnet");
            string route2Name = Recording.GenerateAssetName("azsmnet");
            var routeTable = new RouteTableData() { Location = location, };

            // Add a route
            var route1 = new RouteData()
            {
                AddressPrefix = "192.168.1.0/24",
                Name = route1Name,
                NextHopIpAddress = "23.108.1.1",
                NextHopType = RouteNextHopType.VirtualAppliance
            };

            routeTable.Routes.Add(route1);

            // Put RouteTable
            var routeTableContainer = resourceGroup.GetRouteTables();
            var putRouteTableResponseOperation = await routeTableContainer.CreateOrUpdateAsync(routeTableName, routeTable);
            Response<RouteTable> putRouteTableResponse = await putRouteTableResponseOperation.WaitForCompletionAsync();;
            Assert.AreEqual("Succeeded", putRouteTableResponse.Value.Data.ProvisioningState.ToString());

            // Get RouteTable
            Response<RouteTable> getRouteTableResponse = await routeTableContainer.GetAsync(routeTableName);
            Assert.AreEqual(routeTableName, getRouteTableResponse.Value.Data.Name);
            Assert.AreEqual(1, getRouteTableResponse.Value.Data.Routes.Count);
            Assert.AreEqual(route1Name, getRouteTableResponse.Value.Data.Routes[0].Name);
            Assert.AreEqual("192.168.1.0/24", getRouteTableResponse.Value.Data.Routes[0].AddressPrefix);
            Assert.AreEqual("23.108.1.1", getRouteTableResponse.Value.Data.Routes[0].NextHopIpAddress);
            Assert.AreEqual(RouteNextHopType.VirtualAppliance, getRouteTableResponse.Value.Data.Routes[0].NextHopType);

            // Get Route
            Response<Route> getRouteResponse = await getRouteTableResponse.Value.GetRoutes().GetAsync(route1Name);
            Assert.AreEqual(routeTableName, getRouteTableResponse.Value.Data.Name);
            Assert.AreEqual(1, getRouteTableResponse.Value.Data.Routes.Count);
            Assert.AreEqual(getRouteResponse.Value.Data.Name, getRouteTableResponse.Value.Data.Routes[0].Name);
            Assert.AreEqual(getRouteResponse.Value.Data.AddressPrefix, getRouteTableResponse.Value.Data.Routes[0].AddressPrefix);
            Assert.AreEqual(getRouteResponse.Value.Data.NextHopIpAddress, getRouteTableResponse.Value.Data.Routes[0].NextHopIpAddress);
            Assert.AreEqual(getRouteResponse.Value.Data.NextHopType, getRouteTableResponse.Value.Data.Routes[0].NextHopType);

            // Add another route
            var route2 = new RouteData()
            {
                AddressPrefix = "10.0.1.0/24",
                Name = route2Name,
                NextHopType = RouteNextHopType.VnetLocal
            };

            await getRouteTableResponse.Value.GetRoutes().CreateOrUpdateAsync(route2Name, route2);

            getRouteTableResponse = await routeTableContainer.GetAsync(routeTableName);
            Assert.AreEqual(routeTableName, getRouteTableResponse.Value.Data.Name);
            Assert.AreEqual(2, getRouteTableResponse.Value.Data.Routes.Count);
            Assert.AreEqual(route2Name, getRouteTableResponse.Value.Data.Routes[1].Name);
            Assert.AreEqual("10.0.1.0/24", getRouteTableResponse.Value.Data.Routes[1].AddressPrefix);
            Assert.True(string.IsNullOrEmpty(getRouteTableResponse.Value.Data.Routes[1].NextHopIpAddress));
            Assert.AreEqual(RouteNextHopType.VnetLocal, getRouteTableResponse.Value.Data.Routes[1].NextHopType);

            Response<Route> getRouteResponse2 = await getRouteTableResponse.Value.GetRoutes().GetAsync(route2Name);
            Assert.AreEqual(getRouteResponse2.Value.Data.Name, getRouteTableResponse.Value.Data.Routes[1].Name);
            Assert.AreEqual(getRouteResponse2.Value.Data.AddressPrefix, getRouteTableResponse.Value.Data.Routes[1].AddressPrefix);
            Assert.AreEqual(getRouteResponse2.Value.Data.NextHopIpAddress, getRouteTableResponse.Value.Data.Routes[1].NextHopIpAddress);
            Assert.AreEqual(getRouteResponse2.Value.Data.NextHopType, getRouteTableResponse.Value.Data.Routes[1].NextHopType);

            // list route
            AsyncPageable<Route> listRouteResponseAP = getRouteTableResponse.Value.GetRoutes().GetAllAsync();
            List<Route> listRouteResponse = await listRouteResponseAP.ToEnumerableAsync();
            Assert.AreEqual(2, listRouteResponse.Count());
            Assert.AreEqual(getRouteResponse.Value.Data.Name, listRouteResponse.First().Data.Name);
            Assert.AreEqual(getRouteResponse.Value.Data.AddressPrefix, listRouteResponse.First().Data.AddressPrefix);
            Assert.AreEqual(getRouteResponse.Value.Data.NextHopIpAddress, listRouteResponse.First().Data.NextHopIpAddress);
            Assert.AreEqual(getRouteResponse.Value.Data.NextHopType, listRouteResponse.First().Data.NextHopType);
            Assert.AreEqual(getRouteResponse2.Value.Data.Name, listRouteResponse.ElementAt(1).Data.Name);
            Assert.AreEqual(getRouteResponse2.Value.Data.AddressPrefix, listRouteResponse.ElementAt(1).Data.AddressPrefix);
            Assert.AreEqual(getRouteResponse2.Value.Data.NextHopIpAddress, listRouteResponse.ElementAt(1).Data.NextHopIpAddress);
            Assert.AreEqual(getRouteResponse2.Value.Data.NextHopType, listRouteResponse.ElementAt(1).Data.NextHopType);

            // Delete a route
            await getRouteResponse.Value.DeleteAsync();
            listRouteResponseAP = getRouteTableResponse.Value.GetRoutes().GetAllAsync();
            listRouteResponse = await listRouteResponseAP.ToEnumerableAsync();
            Has.One.EqualTo(listRouteResponse);
            Assert.AreEqual(getRouteResponse2.Value.Data.Name, listRouteResponse.First().Data.Name);
            Assert.AreEqual(getRouteResponse2.Value.Data.AddressPrefix, listRouteResponse.First().Data.AddressPrefix);
            Assert.AreEqual(getRouteResponse2.Value.Data.NextHopIpAddress, listRouteResponse.First().Data.NextHopIpAddress);
            Assert.AreEqual(getRouteResponse2.Value.Data.NextHopType, listRouteResponse.First().Data.NextHopType);

            // Delete route
            await getRouteResponse2.Value.DeleteAsync();

            listRouteResponseAP = getRouteTableResponse.Value.GetRoutes().GetAllAsync();
            listRouteResponse = await listRouteResponseAP.ToEnumerableAsync();
            Assert.IsEmpty(listRouteResponse);

            // Delete RouteTable
            await getRouteTableResponse.Value.DeleteAsync();

            // Verify delete
            AsyncPageable<RouteTable> listRouteTableResponseAP = routeTableContainer.GetAllAsync();
            List<RouteTable> listRouteTableResponse = await listRouteTableResponseAP.ToEnumerableAsync();
            Assert.IsEmpty(listRouteTableResponse);
        }

        [Test]
        [RecordedTest]
        public async Task RoutesHopTypeTest()
        {
            string resourceGroupName = Recording.GenerateAssetName("csmrg");

            string location = TestEnvironment.Location;
            var resourceGroup = await CreateResourceGroup(resourceGroupName);
            string routeTableName = Recording.GenerateAssetName("azsmnet");
            string route1Name = Recording.GenerateAssetName("azsmnet");
            string route2Name = Recording.GenerateAssetName("azsmnet");
            string route3Name = Recording.GenerateAssetName("azsmnet");
            string route4Name = Recording.GenerateAssetName("azsmnet");

            var routeTable = new RouteTableData() { Location = location, };

            // Add a route
            var route1 = new RouteData()
            {
                AddressPrefix = "192.168.1.0/24",
                Name = route1Name,
                NextHopIpAddress = "23.108.1.1",
                NextHopType = RouteNextHopType.VirtualAppliance
            };

            routeTable.Routes.Add(route1);

            // Put RouteTable
            var routeTableContainer = resourceGroup.GetRouteTables();
            var putRouteTableResponseOperation =
                await routeTableContainer.CreateOrUpdateAsync(routeTableName, routeTable);
            Response<RouteTable> putRouteTableResponse = await putRouteTableResponseOperation.WaitForCompletionAsync();;
            Assert.AreEqual("Succeeded", putRouteTableResponse.Value.Data.ProvisioningState.ToString());

            // Get RouteTable
            Response<RouteTable> getRouteTableResponse = await routeTableContainer.GetAsync(routeTableName);
            Assert.AreEqual(routeTableName, getRouteTableResponse.Value.Data.Name);
            Assert.AreEqual(1, getRouteTableResponse.Value.Data.Routes.Count);
            Assert.AreEqual(route1Name, getRouteTableResponse.Value.Data.Routes[0].Name);

            // Add another route
            var route2 = new RouteData()
            {
                AddressPrefix = "10.0.1.0/24",
                Name = route2Name,
                NextHopType = RouteNextHopType.VnetLocal
            };
            await getRouteTableResponse.Value.GetRoutes().CreateOrUpdateAsync(route2Name, route2);

            // Add another route
            var route3 = new RouteData()
            {
                AddressPrefix = "0.0.0.0/0",
                Name = route3Name,
                NextHopType = RouteNextHopType.Internet
            };
            await getRouteTableResponse.Value.GetRoutes().CreateOrUpdateAsync(route3Name, route3);

            // Add another route
            var route4 = new RouteData()
            {
                AddressPrefix = "10.0.2.0/24",
                Name = route4Name,
                NextHopType = RouteNextHopType.None
            };
            await getRouteTableResponse.Value.GetRoutes().CreateOrUpdateAsync(route4Name, route4);

            getRouteTableResponse = await routeTableContainer.GetAsync(routeTableName);
            Assert.AreEqual(routeTableName, getRouteTableResponse.Value.Data.Name);
            Assert.AreEqual(4, getRouteTableResponse.Value.Data.Routes.Count);
            Assert.AreEqual(route2Name, getRouteTableResponse.Value.Data.Routes[1].Name);
            Assert.AreEqual(RouteNextHopType.VirtualAppliance, getRouteTableResponse.Value.Data.Routes[0].NextHopType);
            Assert.AreEqual(RouteNextHopType.VnetLocal, getRouteTableResponse.Value.Data.Routes[1].NextHopType);
            Assert.AreEqual(RouteNextHopType.Internet, getRouteTableResponse.Value.Data.Routes[2].NextHopType);
            Assert.AreEqual(RouteNextHopType.None, getRouteTableResponse.Value.Data.Routes[3].NextHopType);

            // Delete RouteTable
            await getRouteTableResponse.Value.DeleteAsync();

            // Verify delete
            AsyncPageable<RouteTable> listRouteTableResponseAP = routeTableContainer.GetAllAsync();
            List<RouteTable> listRouteTableResponse = await listRouteTableResponseAP.ToEnumerableAsync();
            Assert.IsEmpty(listRouteTableResponse);
        }
    }
}
