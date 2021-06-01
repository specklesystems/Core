using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Serialize to JSON")]
  [NodeCategory("Speckle 2.Developer Tools.Serialize.Actions")]
  [NodeDescription("Serializes a Speckle Base object to JSON")]
  [InPortNames("object")]
  [InPortTypes("object")]
  [InPortDescriptions("Speckle base objects to serialize.")]
  [OutPortNames("json")]
  [OutPortTypes("string")]
  [OutPortDescriptions("The given object(s) serialized to JSON.")]
  [NodeSearchTags("speckle", "developer", "serialize", "JSON")]
  [IsDesignScriptCompatible]
  public class JsonSerialize : NodeModel
  {
    public JsonSerialize()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private JsonSerialize(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }
    
    public static string Serialize(Base @base)
    {
      Tracker.TrackPageview(Tracker.SERIALIZE);
      return Operations.Serialize(@base);
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
        new Func<Base, object>(Serialize),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
