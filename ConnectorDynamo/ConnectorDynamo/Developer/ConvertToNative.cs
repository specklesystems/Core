using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Convert To Native")]
  [NodeCategory("Speckle 2.Developer.Conversion.Actions")]
  [NodeDescription("Converts an object from its Speckle representation to the native application's object model.")]
  [InPortNames("base")]
  [InPortTypes("Speckle.Core.Models.Base")]
  [InPortDescriptions("An object deriving from Speckle's base object.")]
  [OutPortNames("nativeObject")]
  [OutPortTypes("object")]
  [OutPortDescriptions("The given object in the application's native object model.")]
  [NodeSearchTags("speckle", "developer", "convert", "native")]
  [IsDesignScriptCompatible]
  public class ConvertToNative : NodeModel
  {
    public ConvertToNative()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private ConvertToNative(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }
    private static object ToNative(Base @base)
    {
      Tracker.TrackPageview(Tracker.CONVERT_TONATIVE);
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToNative(@base);
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
        new Func<Base, object>(ToNative),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
