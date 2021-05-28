using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Accounts
{
  [NodeName("Account Details")]
  [NodeCategory("Speckle 2.Accounts.Query")]
  [NodeDescription("Displays the details of the given Speckle account(s).")]
  [InPortNames("account")]
  [InPortTypes("Speckle.Core.Credentials.Account")]
  [InPortDescriptions("One or more Speckle account(s).")]
  [OutPortNames("details")]
  [OutPortTypes("System.String[]")]
  [OutPortDescriptions("Details from the account(s) that were given.")]
  [NodeSearchTags("account", "details", "speckle", "info")]
  [IsDesignScriptCompatible]
  public class AccountDetails : NodeModel
  {
    public AccountDetails()
    {
      Tracker.TrackPageview(Tracker.ACCOUNT_DETAILS);
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
    [JsonConstructor]
    private AccountDetails(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      
    }
    private void AddInputs()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("account", "The Speckle accounts(s) to retrieve details from.")));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("details", "The details of the given account(s).")));
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
        new Func<Core.Credentials.Account, object>(Functions.Account.Details),
        new List<AssociativeNode> { inputAstNodes[0] }
      );;

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
