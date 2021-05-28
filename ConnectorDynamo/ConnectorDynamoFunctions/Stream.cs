using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Functions
{
  public static class Stream
  {
    [IsVisibleInDynamoLibrary(false)]
    public static object GetStream([ArbitraryDimensionArrayImport] object streamUrl, [DefaultArgument("null")] Core.Credentials.Account account)
    {
      Tracker.TrackPageview(Tracker.STREAM_GET);

      var streams = Utils.InputToStream(streamUrl);
      if (!streams.Any())
      {
        throw new SpeckleException("Please provide one or more Stream URLs.");
      }
      else if (streams.Count > 20)
      {
        throw new SpeckleException("Please provide fewer than 20 Stream URLs.");
      }

      try
      {

        foreach (var s in streams)
        {
          //lets ppl override the account for the specified stream
          Core.Credentials.Account accountToUse = null;
          if (account != null)
            accountToUse = account;
          else
            accountToUse = Task.Run(async () => await s.GetAccount()).Result;

          var client = new Client(accountToUse);

          //Exists?
          Core.Api.Stream res = Task.Run(async () => await client.StreamGet(s.StreamId)).Result;
          s.UserId = accountToUse.userInfo.id;
        }
      }
      catch (Exception ex)
      {
        Utils.HandleApiException(ex);
      }

      if (streams.Count() == 1)
        return streams[0];

      return streams;
    }

    [IsVisibleInDynamoLibrary(false)]
    public static StreamWrapper UpdateStream(object streamUrl, [DefaultArgument("null")] string name, [DefaultArgument("null")] string description, [DefaultArgument("null")] bool? isPublic)
    {
      Tracker.TrackPageview(Tracker.STREAM_UPDATE);

      if (streamUrl == null)
      {
        return null;
      }

      var wrapper = Utils.ParseWrapper(streamUrl);

      if (wrapper == null)
      {
        throw new SpeckleException("Invalid stream.");
      }

      if (name == null && description == null && isPublic == null)
        return null;

      var account = Task.Run(async () => await wrapper.GetAccount()).Result;

      var client = new Client(account);

      var input = new StreamUpdateInput { id = wrapper.StreamId };

      if (name != null)
        input.name = name;

      if (description != null)
        input.description = description;

      if (isPublic != null)
        input.isPublic = (bool)isPublic;

      try
      {
        var res = Task.Run(async () => await client.StreamUpdate(input)).Result;

        if (res)
          return wrapper;
      }
      catch (Exception ex)
      {
        Utils.HandleApiException(ex);
      }

      return null;
    }

    /// <summary>
    /// Extracts the details of a given stream, use is limited to max 20 streams 
    /// </summary>
    /// <param name="stream">Stream object</param>
    [IsVisibleInDynamoLibrary(false)]
    public static object Details([ArbitraryDimensionArrayImport] object stream)
    {
      Tracker.TrackPageview(Tracker.STREAM_DETAILS);

      var streams = Utils.InputToStream(stream);

      if (!streams.Any())
        throw new SpeckleException("Please provide one or more Streams.");

      if (streams.Count > 20)
        throw new SpeckleException("Please provide fewer than 20 Streams.");

      var details = new List<Dictionary<string, object>>();

      foreach (var streamWrapper in streams)
      {
        var account = Task.Run(async () => await streamWrapper.GetAccount()).Result;

        var client = new Client(account);

        try
        {
          Core.Api.Stream res = Task.Run(async () => await client.StreamGet(streamWrapper.StreamId)).Result;

          details.Add(new Dictionary<string, object>
          {
            { "id", res.id },
            { "name", res.name },
            { "description", res.description },
            { "createdAt", res.createdAt },
            { "updatedAt", res.updatedAt },
            { "isPublic", res.isPublic },
            { "collaborators", res.collaborators },
            { "branches", res.branches?.items }
          });
        }
        catch (Exception ex)
        {
          Utils.HandleApiException(ex);
          return details;
        }

      }

      if (details.Count() == 1) return details[0];
      return details;
    }

    [IsVisibleInDynamoLibrary(false)]
    [NodeCategory("Query")]
    public static List<StreamWrapper> List([DefaultArgument("null")] Core.Credentials.Account account = null, [DefaultArgument("10")] int limit = 10)
    {
      Tracker.TrackPageview(Tracker.STREAM_LIST);

      if (account == null)
        account = AccountManager.GetDefaultAccount();

      if (account == null)
      {
        Utils.HandleApiException(new Exception("No accounts found. Please use the Speckle Manager to manage your accounts on this computer."));
      }

      var client = new Client(account);
      var streamWrappers = new List<StreamWrapper>();

      try
      {
        var res = Task.Run(async () => await client.StreamsGet(limit)).Result;
        res.ForEach(x => { streamWrappers.Add(new StreamWrapper(x.id, account.userInfo.id, account.serverInfo.url)); });
      }
      catch (Exception ex)
      {
        Utils.HandleApiException(ex);
      }

      return streamWrappers;
    }
  }
}
