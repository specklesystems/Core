using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Streams
{
  [NodeName("Update Stream")]
  [NodeCategory("Speckle 2.Streams.Actions")]
  [NodeDescription("Update a Stream's details, such as its name or description. Can only be used with 1 stream at a time.")]
  [InPortNames("streamUrl", "name", "description", "isPublic")]
  [InPortTypes("string", "string", "string", "bool")]
  [InPortDescriptions
    (
    "URL of the Speckle stream(s) to retrieve details from. Can be the URL of a stream, branch or commit or object.",
    "Optional: The new name you want this stream to have.",
    "Optional: The new description you want the stream to have.",
    "Optional: Whether the stream should be publicly accessible."
    )
  ]
  [OutPortNames("stream")]
  [OutPortTypes("Speckle.Core.Credentials.StreamWrapper")]
  [OutPortDescriptions("The updated Speckle stream.")]
  [NodeSearchTags("speckle", "stream", "update")]
  [IsDesignScriptCompatible]
  public class UpdateStream : NodeModel
  {
    public UpdateStream()
    {
      Tracker.TrackPageview(Tracker.STREAM_UPDATE);
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private UpdateStream(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
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
        new Func<object, string, string, bool?, object>(Functions.Stream.UpdateStream),
        new List<AssociativeNode> { inputAstNodes[0] }
      ); ;

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
