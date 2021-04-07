using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Utils
{
  public static class Api
  {
    /// <summary>
    /// Helper method to Receive from a Speckle Server.
    /// </summary>
    /// <param name="stream">Stream URL or Id to receive from. If the URL contains branchName, commitId or objectId those will be used, otherwise the latest commit from main will be received.</param>
    /// <param name="account">Account to use. If not provided the default account will be used.</param>
    /// <param name="onProgressAction">Action invoked on progress iterations.</param>
    /// <param name="onErrorAction">Action invoked on internal errors.</param>
    /// <param name="onTotalChildrenCountKnown">Action invoked once the total count of objects is known.</param>
    /// <returns></returns>
    public static async Task<Base> Receive(string stream, Account account = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null, Action<int> onTotalChildrenCountKnown = null)
    {
      var sw = new StreamWrapper(stream);

      var client = new Client(account ?? await sw.GetAccount());

      var transport = new ServerTransport(client.Account, sw.StreamId);

      Tracker.TrackPageview(Tracker.RECEIVE);

      //OBJECT URL
      if (!string.IsNullOrEmpty(sw.ObjectId))
      {
        return await Operations.Receive(
          sw.ObjectId,
          remoteTransport: transport,
          onErrorAction: onErrorAction,
          onProgressAction: onProgressAction,
          onTotalChildrenCountKnown: onTotalChildrenCountKnown
        );
      }


      //COMMIT URL
      if (!string.IsNullOrEmpty(sw.CommitId))
      {
        var commit = await client.CommitGet(sw.StreamId, sw.CommitId);
        return await Operations.Receive(
          sw.ObjectId,
          remoteTransport: transport,
          onErrorAction: onErrorAction,
          onProgressAction: onProgressAction,
          onTotalChildrenCountKnown: onTotalChildrenCountKnown
        );
      }

      //BRANCH URL OR MAIN
      var branchName = string.IsNullOrEmpty(sw.BranchName) ? "main" : sw.BranchName;

      var branch = await client.BranchGet(sw.StreamId, branchName, 1);
      if (!branch.commits.items.Any())
        throw new SpeckleException($"The selected branch has no commits.", level: SentryLevel.Error);

      var branchCommit = branch.commits.items[0];
      return await Operations.Receive(
        branchCommit.referencedObject,
        remoteTransport: transport,
        onErrorAction: onErrorAction,
        onProgressAction: onProgressAction,
        onTotalChildrenCountKnown: onTotalChildrenCountKnown
      );

    }

    /// <summary>
    /// Helper method to Send to a Speckle Server.
    /// </summary>
    /// <param name="stream">Stream URL or Id to receive from. If the URL contains branchName, commitId or objectId those will be used, otherwise the latest commit from main will be received.</param>
    /// <param name="data">Data to send</param>
    /// <param name="account">Account to use. If not provided the default account will be used.</param>
    /// <param name="useDefaultCache">Toggle for the default cache. If set to false, it will only send to the provided transports.</param>
    /// <param name="onProgressAction">Action invoked on progress iterations.</param>
    /// <param name="onErrorAction">Action invoked on internal errors.</param>
    /// <returns></returns>
    public static async Task<string> Send(string stream, Base data, Account account = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      var sw = new StreamWrapper(stream);

      var client = new Client(account ?? await sw.GetAccount());

      var transport = new ServerTransport(client.Account, sw.StreamId);
      var branchName = string.IsNullOrEmpty(sw.BranchName) ? "main" : sw.BranchName;

      var objectId = await Operations.Send(
        data,
        new List<ITransport> { transport },
        useDefaultCache,
        onProgressAction,
        onErrorAction);

      Tracker.TrackPageview(Tracker.SEND);
      return await client.CommitCreate(
            new CommitCreateInput
            {
              streamId = sw.StreamId,
              branchName = branchName,
              objectId = objectId,
              message = "Data from unity!"
            });

    }
  }
}
