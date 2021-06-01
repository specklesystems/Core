using System;
using System.Collections.Generic;
using System.Linq;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Accounts
{
  /// <summary>
  /// Displays the details of a given account
  /// </summary>
  [NodeName("Account Details")]
  [NodeCategory("Speckle 2.Accounts.Query")]
  [NodeDescription("Displays the details of the given Speckle account(s).")]
  [InPortNames("account")]
  [InPortTypes("Speckle.Core.Credentials.Account")]
  [InPortDescriptions("The Speckle account(s) to retrieve details from. Use the List Accounts node to see which accounts are available.")]
  [OutPortNames("details")]
  [OutPortTypes("System.String[]")]
  [OutPortDescriptions("Details of the given account(s).")]
  [NodeSearchTags("account", "details", "speckle", "info")]
  [IsDesignScriptCompatible]
  public class AccountDetails : NodeModel
  {
    /// <summary>
    /// JSON constructor, called on file open
    /// </summary>
    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
    [JsonConstructor]
    private AccountDetails(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (outPorts.Count() == 0)
        AddOutputs();
      if (inPorts.Count() == 0)
        AddInputs();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    /// <summary>
    /// Normal constructor, called when adding node to canvas
    /// </summary>
    public AccountDetails()
    {
      Tracker.TrackPageview(Tracker.ACCOUNT_DETAILS);
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    private void AddInputs()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("account", "The Speckle accounts(s) to retrieve details from. Use the List Accounts node to see which accounts are available.")));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("details", "Details of the given account(s).")));
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
