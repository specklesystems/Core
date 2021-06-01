using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Api;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Deserialize from JSON")]
  [NodeCategory("Speckle 2.Developer Tools.Serialize.Actions")]
  [NodeDescription("Deserializes a JSON string to a Speckle Base object.")]
  [InPortNames("json")]
  [InPortTypes("string")]
  [InPortDescriptions("Serialized Base objects in JSON format.")]
  [OutPortNames("object")]
  [OutPortTypes("object")]
  [OutPortDescriptions("Deserialized Speckle Base objects.")]
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
