using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("Convert To Speckle")]
  [NodeCategory("Speckle 2.Developer Tools.Conversion.Actions")]
  [NodeDescription("Converts an object from its native representation to Speckle's object model.")]
  [InPortDescriptions("An object from the application's object model.")]
  [InPortNames("nativeObject")]
  [InPortTypes("object")]
  [OutPortNames("base")]
  [OutPortTypes("Speckle.Core.Models.Base")]
  [OutPortDescriptions("The given object in Speckle's object model.")]
  [NodeSearchTags("speckle", "developer", "convert")]
  [IsDesignScriptCompatible]
  public class ConvertToSpeckle : NodeModel
  {
    public ConvertToSpeckle()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private ConvertToSpeckle(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    private static Base ToSpeckle([ArbitraryDimensionArrayImport] object data)
    {
      Tracker.TrackPageview(Tracker.CONVERT_TOSPECKLE);
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToSpeckle(data);
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
        new Func<object, object>(ToSpeckle),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
