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
  [NodeName("List Streams")]
  [NodeCategory("Speckle 2.Streams.Create")]
  [NodeDescription("Lists all the streams associated to the given account. If no account is given, all streams belonging to the default Speckle Manager account are retrieved.")]
  [InPortNames("account", "limit")]
  [InPortTypes("Speckle.Core.Credentials.Account", "System.Int32")]
  [InPortDescriptions("A Speckle account. Use the Select Account node to retrieve Speckle accounts.", "Optional: Limit the maximum number of streams to retrieve.")]
  [OutPortNames("streamUrls")]
  [OutPortTypes("string[]")]
  [OutPortDescriptions("The URL of one (or more) Speckle stream(s).")]
  [NodeSearchTags("speckle", "stream", "streams", "list", "get", "retrieve")]
  [IsDesignScriptCompatible]
  public class ListStreams: NodeModel
  {
    public ListStreams()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private ListStreams(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (inPorts.Count() == 2)
      {
      }
      else
      {
        InPorts.Clear();
        AddInputs();
      }
      if(outPorts.Count() == 0) AddOutputs();
    }

    private void AddInputs()
    {
      var defaultValue = new IntNode(10);
      
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("account", "A Speckle account. Use the Select Account node to retrieve Speckle accounts.")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("limit", "Optional: Limit the maximum number of streams to retrieve.", defaultValue)
      {
        DefaultValue = defaultValue
      }));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("streamUrls", "The URL of one (or more) Speckle stream(s).")));
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
        new Func<Core.Credentials.Account, int, object>(Functions.Stream.List),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
