using System;
using System.Collections.Generic;
using System.Linq;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Streams
{
  [NodeName("Stream Details")]
  [NodeCategory("Speckle 2.Streams.Query")]
  [NodeDescription("Extracts the details of a given stream, or list of streams. Use is limited to max 20 streams.")]
  [InPortNames("streamUrl")]
  [InPortTypes("System.String")]
  [InPortDescriptions("URL of the Speckle stream(s) to retrieve details from. Can be the URL of a stream, branch or commit or object.")]
  [OutPortNames("details")]
  [OutPortTypes("System.String[]")]
  [OutPortDescriptions("Details from the stream(s) that were given.")]
  [NodeSearchTags("speckle", "stream", "details", "info")]
  [IsDesignScriptCompatible]
  public class StreamDetails : NodeModel
  {
    public StreamDetails()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private StreamDetails(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (inPorts.Count() == 1) {}
      else
      {
        InPorts.Clear();
        AddInputs();
      }
      if (outPorts.Count() == 0) AddOutputs();
    }

    private void AddInputs()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("streamUrl", "URL of the Speckle stream(s) to retrieve details from. Can be the URL of a stream, branch or commit or object.")));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("details", "Details from the stream(s) that were given.")));
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
        new Func<string, object>(Functions.Stream.Details),
        new List<AssociativeNode> { inputAstNodes[0] }
      );;
      
      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
