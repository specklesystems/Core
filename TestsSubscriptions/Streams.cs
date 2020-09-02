﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;

namespace TestsSubscriptions
{
  public class Tests
  {
    public Client client;
    public ServerTransport myServerTransport;

    private StreamInfo StreamAddedInfo;
    private StreamInfo StreamUpdatedInfo;
    private StreamInfo StreamRemovedInfo;
    string streamId;

    [OneTimeSetUp]
    public void Setup()
    {
      client = new Client(AccountManager.GetAccounts().First());
      myServerTransport = new ServerTransport(AccountManager.GetDefaultAccount(), null);
    }

    [Test, Order(0)]
    public async Task SubscribeStreamAdded()
    {
      client.SubscribeUserStreamAdded();
      client.OnUserStreamAdded += Client_OnUserStreamAdded;

      Thread.Sleep(100); //let server catch-up

      var streamInput = new StreamCreateInput
      {
        description = "Hello World",
        name = "Super Stream 01"
      };

      var res = await client.StreamCreate(streamInput);
      streamId = res;
      Assert.NotNull(res);

      await Task.Run(() => {
        Thread.Sleep(100); //let client catch-up
        Assert.NotNull(StreamAddedInfo);
        Assert.AreEqual(streamInput.name, StreamAddedInfo.name);
      });
    }

    private void Client_OnUserStreamAdded(object sender, StreamInfo e)
    {
      StreamAddedInfo = e;
    }

    [Test, Order(1)]
    public async Task SubscribeStreamUpdated()
    {
      client.SubscribeStreamUpdated(streamId);
      client.OnStreamUpdated += Client_OnStreamUpdated; ;

      Thread.Sleep(100); //let server catch-up

      var streamInput = new StreamUpdateInput
      {
        id = streamId,
        description = "Hello World",
        name = "Super Stream 01 EDITED"
      };

      var res = await client.StreamUpdate(streamInput);

      Assert.True(res);
      
      await Task.Run(() => {
        Thread.Sleep(100); //let client catch-up
        Assert.NotNull(StreamUpdatedInfo);
        Assert.AreEqual(streamInput.name, StreamUpdatedInfo.name);
      });

    }

    private void Client_OnStreamUpdated(object sender, StreamInfo e)
    {
      StreamUpdatedInfo = e;
    }

    [Test, Order(2)]
    public async Task SubscribeStreamRemoved()
    {
      client.SubscribeStreamRemoved(streamId);
      client.OnStreamRemoved += Client_OnStreamRemoved; ;

      Thread.Sleep(100); //let server catch-up

      var res = await client.StreamDelete(streamId);

      Assert.True(res);

      await Task.Run(() => {
        Thread.Sleep(100); //let client catch-up
        Assert.NotNull(StreamRemovedInfo);
        Assert.AreEqual(streamId, StreamRemovedInfo.id);
      });

    }

    private void Client_OnStreamRemoved(object sender, StreamInfo e)
    {
      StreamRemovedInfo = e;
    }
  }
}
