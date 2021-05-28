using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Api;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Receive Local Data")]
  [NodeCategory("Speckle 2.Developer.Local.Actions")]
  [NodeDescription("Receives data locally, without requiring a Speckle Server.\nNOTE: Updates will not be received automatically.")]
  [InPortNames("localDataId")]
  [InPortTypes("string")]
  [InPortDescriptions("ID of the local data to receive.")]
  [OutPortNames("data")]
  [OutPortTypes("var")]
  [OutPortDescriptions("The received data.")]
  [NodeSearchTags("speckle", "developer", "local", "receive")]
  [IsDesignScriptCompatible]
  public class LocalReceiveData : NodeModel
  {
    public LocalReceiveData()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private LocalReceiveData(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    public static object Receive(string localDataId)
    {
      Tracker.TrackPageview(Tracker.RECEIVE_LOCAL);

      var @base = Task.Run(async () => await Operations.Receive(localDataId)).Result;
      var converter = new BatchConverter();
      var data = converter.ConvertRecursivelyToNative(@base);
      return data;
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
        new Func<string, object>(Receive),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
