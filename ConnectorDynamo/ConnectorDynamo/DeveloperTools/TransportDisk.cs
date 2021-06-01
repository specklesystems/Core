using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Disk Transport")]
  [NodeCategory("Speckle 2.Developer Tools.Transport.Actions")]
  [NodeDescription("Creates a Disk Transport.")]
  [InPortNames("basePath")]
  [InPortTypes("string")]
  [InPortDescriptions("The root folder where you want the data to be stored. Defaults to `%appdata%/Speckle/DiskTransportFiles`.")]
  [OutPortNames("transport")]
  [OutPortTypes("object")]
  [OutPortDescriptions("The Disk Transport you have created.")]
  [NodeSearchTags("speckle", "developer", "transport", "disk")]
  [IsDesignScriptCompatible]
  public class TransportDisk : NodeModel
  {
    public TransportDisk()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private TransportDisk(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

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
        new Func<string, object>(Functions.Advanced.Transport.DiskTransport),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
