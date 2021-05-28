using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Api;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Deserialize from JSON")]
  [NodeCategory("Speckle 2.Developer.Serialize.Actions")]
  [NodeDescription("Deserializes an object from JSON.")]
  [InPortNames("json")]
  [InPortTypes("string")]
  [InPortDescriptions("A JSON representation of an object(s).")]
  [OutPortNames("object")]
  [OutPortTypes("object")]
  [OutPortDescriptions("The deserialized object")]
  [NodeSearchTags("speckle", "developer", "deserialize", "JSON")]
  [IsDesignScriptCompatible]
  public class JsonDeserialize : NodeModel
  {
    public JsonDeserialize()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private JsonDeserialize(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    public static object Deserialize(string json)
    {
      Tracker.TrackPageview(Tracker.DESERIALIZE);
      return Operations.Deserialize(json);
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
        new Func<string, object>(Deserialize),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
