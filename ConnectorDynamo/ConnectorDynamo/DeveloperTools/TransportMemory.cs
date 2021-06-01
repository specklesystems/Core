using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Memory Transport")]
  [NodeCategory("Speckle 2.Developer Tools.Transport.Actions")]
  [NodeDescription("Creates an Memory Transport.")]
  [InPortNames("name")]
  [InPortTypes("string")]
  [InPortDescriptions("The name of this memory transport.")]
  [OutPortNames("transport")]
  [OutPortTypes("object")]
  [OutPortDescriptions("The transport you have created.")]
  [NodeSearchTags("speckle", "developer", "transport", "disk")]
  [IsDesignScriptCompatible]
  public class TransportMemory : NodeModel
  {
    public TransportMemory()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private TransportMemory(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    public static object MemoryTransport(string name = "Memory")
    {
      Tracker.TrackPageview("transports", "memory");
      return new MemoryTransport { TransportName = name };
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
        new Func<string, object>(MemoryTransport),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
