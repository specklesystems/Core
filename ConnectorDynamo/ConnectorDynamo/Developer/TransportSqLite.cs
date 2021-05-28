using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Developer
{
  [NodeName("SQLite Transport")]
  [NodeCategory("Speckle 2.Developer.Transport.Actions")]
  [NodeDescription("Creates an SQLite Transport.")]
  [InPortNames("basePath", "applicationName", "scope")]
  [InPortTypes("string, string, string")]
  [InPortDescriptions
  (
    "The root folder where you want the sqlite db to be stored.Defaults to `%appdata%`",
    "The subfolder you want the sqlite db to be stored. Defaults to `Speckle`",
    "The name of the actual database file. Defaults to `UserLocalDefaultDb`"
  )]
  [OutPortNames("")]
  [OutPortTypes("")]
  [OutPortDescriptions("")]
  [NodeSearchTags("speckle", "developer", "transport", "sqlite")]
  [IsDesignScriptCompatible]
  
  public class TransportSqLite : NodeModel
  {
    public TransportSqLite()
    {
      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    [JsonConstructor]
    private TransportSqLite(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }

    public static object SQLiteTransport(string basePath = "", string applicationName = "Speckle", string scope = "UserLocalDefaultDb")
    {
      if (string.IsNullOrEmpty(basePath))
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      if (string.IsNullOrEmpty(applicationName))
        applicationName = "Speckle";
      if (string.IsNullOrEmpty(scope))
        scope = "UserLocalDefaultDb";

      Tracker.TrackPageview("transports", "server");
      return new SQLiteTransport(basePath, applicationName, scope);
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
        new Func<string, string, string, object>(SQLiteTransport),
        new List<AssociativeNode> { inputAstNodes[0] }
      );

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall));
      return associativeNodes;
    }
  }
}
