using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Server Transport")]
  [NodeCategory("Speckle 2.Developer.Transport.Actions")]
  [NodeDescription("Creates a server transport.")]
  [InPortNames("stream")]
  [InPortTypes("object")]
  [InPortDescriptions("The Stream you want to send data to.")]
  [OutPortNames("transport")]
  [OutPortTypes("object")]
  [OutPortDescriptions("The Server Transport you have created.")]
  [NodeSearchTags("speckle", "developer", "transport", "disk")]
  [IsDesignScriptCompatible]
  public class TransportServer : NodeModel
  {
    public TransportServer()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private TransportServer(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    public static object ServerTransport(StreamWrapper stream)
    {
      Tracker.TrackPageview("transports", "server");
      var userId = stream.UserId;
      Core.Credentials.Account account;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId);
      Exception error = null;
      if (account == null)
      {
        // Get the default account
        account = AccountManager.GetAccounts(stream.ServerUrl).FirstOrDefault();
        error = new WarningException(
          "Original account not found. Please make sure you have permissions to access this stream!");
        if (account == null)
        {
          // No default
          error = new WarningException(
            $"No account found for {stream.ServerUrl}.");
        }
      }

      if (error != null) throw error;

      return new ServerTransport(account, stream.StreamId);
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected)
      {
        return OutPorts.Enumerate().Select(output =>
          AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
      }

      var associativeNodes = new List<AssociativeNode>();

      AssociativeNode functionCall = AstFactory.BuildFunctionCall
      (
        new Func<StreamWrapper, object>(ServerTransport),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
