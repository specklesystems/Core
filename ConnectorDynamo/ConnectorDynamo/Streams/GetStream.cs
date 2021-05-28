using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;

namespace Speckle.ConnectorDynamo.Streams
{
  [NodeName("Get Stream")]
  [NodeCategory("Speckle 2.Streams.Create")]
  [NodeDescription("Gets an existing Stream from its URL. This node is used for gaining access to a stream via a specific Speckle account.")]
  [InPortNames("streamUrl", "account")]
  [InPortTypes("System.String", "Speckle.Core.Credentials.Account")]
  [InPortDescriptions("URL of the Speckle stream(s) to retrieve details from. Can be the URL of a stream, branch or commit or object.", "A Speckle account.")]
  [OutPortNames("stream")]
  [OutPortTypes("object")]
  [OutPortDescriptions("One or more Speckle stream(s).")]
  [NodeSearchTags("speckle", "stream", "get", "retrieve")]
  [IsDesignScriptCompatible]
  public class GetStream : NodeModel
  {
    public GetStream()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private GetStream(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
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
        new Func<string, Core.Credentials.Account, object>(Functions.Stream.GetStream),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
