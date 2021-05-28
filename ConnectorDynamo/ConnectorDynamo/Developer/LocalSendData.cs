using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Api;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Send Local Data")]
  [NodeCategory("Speckle 2.Developer.Local.Actions")]
  [NodeDescription("Sends data locally, without requiring a Speckle Server.")]
  [InPortNames("data")]
  [InPortTypes("var")]
  [InPortDescriptions("The data to be sent locally.")]
  [OutPortNames("localDataId")]
  [OutPortTypes("string")]
  [OutPortDescriptions("Id of the data that was sent locally.")]
  [NodeSearchTags("speckle", "developer", "local", "send")]
  [IsDesignScriptCompatible]
  public class LocalSendData : NodeModel
  {
    public LocalSendData()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private LocalSendData(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    public static string Send([ArbitraryDimensionArrayImport] object data)
    {
      Tracker.TrackPageview(Tracker.SEND_LOCAL);

      var converter = new BatchConverter();
      var @base = converter.ConvertRecursivelyToSpeckle(data);
      var objectId = Task.Run(async () => await Operations.Send(@base)).Result;

      return objectId;
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
        new Func<string, object>(Send),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
