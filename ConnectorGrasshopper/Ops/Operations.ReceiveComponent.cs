﻿using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ConnectorGrasshopper.Ops
{
  public class ReceiveComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid => new Guid("{3D07C1AC-2D05-42DF-A297-F861CCEEFBC7}");

    protected override Bitmap Icon => Properties.Resources.Receiver;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public bool AutoReceive { get; set; } = false;

    public string CurrentComponentState { get; set; } = "needs_input";

    public double OverallProgress { get; set; } = 0;

    public bool JustPastedIn { get; set; }

    public string LastInfoMessage { get; set; }

    public string LastCommitDate { get; set; }

    public string ReceivedObjectId { get; set; }

    public string InputType { get; set; }

    public StreamWrapper StreamWrapper { get; set; }

    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;

    private Client ApiClient { get; set; }

    public ReceiveComponent() : base("Receive", "Receive", "Receives Speckle data.", "Speckle 2", "   Send/Receive")
    {
      BaseWorker = new ReceiveComponentWorker(this);
      Attributes = new ReceiveComponentAttributes(this);
      SetDefaultKitAndConverter();
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("AutoReceive", AutoReceive);
      writer.SetString("CurrentComponentState", CurrentComponentState);
      writer.SetString("LastInfoMessage", LastInfoMessage);
      writer.SetString("LastCommitDate", LastCommitDate);
      writer.SetString("ReceivedObjectId", ReceivedObjectId);
      writer.SetString("KitName", Kit.Name);
      var streamUrl = StreamWrapper != null ? StreamWrapper.ToString() : "";
      writer.SetString("StreamWrapper", streamUrl);
      
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      AutoReceive = reader.GetBoolean("AutoReceive");
      CurrentComponentState = reader.GetString("CurrentComponentState");
      LastInfoMessage = reader.GetString("LastInfoMessage");
      LastCommitDate = reader.GetString("LastCommitDate");
      ReceivedObjectId = reader.GetString("ReceivedObjectId");

      var swString = reader.GetString("StreamWrapper");
      if (!string.IsNullOrEmpty(swString))
      {
        StreamWrapper = new StreamWrapper(swString);
      }

      JustPastedIn = true;

      var kitName = "";
      reader.TryGetString("KitName", ref kitName);

      if (kitName != "")
      {
        try
        {
          SetConverterFromKit(kitName);
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not find the {kitName} kit on this machine. Do you have it installed? \n Will fallback to the default one.");
          SetDefaultKitAndConverter();
        }
      }
      else
      {
        SetDefaultKitAndConverter();
      }

      return base.Read(reader);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S", "The Speckle Stream you want to receive data from. You can also input the Stream ID or it's URL as text.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "The data.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Info", "I", "Commit information.", GH_ParamAccess.item);
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }
      Menu_AppendSeparator(menu);

      if (InputType == "Stream" || InputType == "Branch")
      {
        var autoReceiveMi = Menu_AppendItem(menu, $"Receive automatically", (s, e) =>
        {
          AutoReceive = !AutoReceive;
          Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
          {
            OnDisplayExpired(true);
          });
        }, true, AutoReceive);
        autoReceiveMi.ToolTipText = "Toggle automatic receiving. If set, any upstream change will be pulled instantly. This only is applicable when receiving a stream or a branch.";
      }
      else
      {
        var autoReceiveMi = Menu_AppendItem(menu, "Automatic receiving is disabled because you have specified a direct commit.");
        autoReceiveMi.ToolTipText = "To enable automatic receiving, you need to input a stream rather than a specific commit.";
      }

      base.AppendAdditionalComponentMenuItems(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    private void SetDefaultKitAndConverter()
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
        var x = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      // We need to call this always in here to be able to react and set events :/
      ParseInput(DA);

      if ((AutoReceive || CurrentComponentState == "primed_to_receive" || CurrentComponentState == "receiving") && !JustPastedIn)
      {
        CurrentComponentState = "receiving";

        // Delegate control to parent async component.
        base.SolveInstance(DA);
        return;
      }
      else if (!JustPastedIn)
      {
        CurrentComponentState = "expired";
        Message = "Expired";
        OnDisplayExpired(true);
      }

      // Set output data in a "first run" event. Note: we are not persisting the actual "sent" object as it can be very big.
      if (JustPastedIn)
      {
        DA.SetData(1, LastInfoMessage);
        // This ensures that we actually do a run. The worker will check and determine if it needs to pull an existing object or not.
        base.SolveInstance(DA);
      }
    }

    public override void DisplayProgress(object sender, ElapsedEventArgs e)
    {
      if (Workers.Count == 0)
      {
        return;
      }

      Message = "";
      var total = 0.0;
      foreach (var kvp in ProgressReports)
      {
        Message += $"{kvp.Key}: {kvp.Value:0.00%}\n";
        total += kvp.Value;
      }

      OverallProgress = total / ProgressReports.Keys.Count();

      Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
      {
        OnDisplayExpired(true);
      });
    }

    public void ParseInput(IGH_DataAccess DA)
    {
      DA.GetDataTree(0, out GH_Structure<IGH_Goo> DataInput);

      var ghGoo = DataInput.get_DataItem(0);
      if (ghGoo == null)
      {
        return;
      }
      var input = ghGoo.GetType().GetProperty("Value")?.GetValue(ghGoo);

      var inputType = "Stream";
      StreamWrapper newWrapper = null;

      if (input is StreamWrapper)
      {
        newWrapper = input as StreamWrapper;
      }
      else if (input is string s)
      {
        newWrapper = new StreamWrapper(s);
      }

      if (newWrapper?.CommitId != null)
      {
        inputType = "Commit";
      }

      Message = inputType;
      HandleInputType(inputType, newWrapper);
    }

    public void HandleInputType(string inputType, StreamWrapper wrapper)
    {
      if (inputType != InputType)
      {
      }

      InputType = inputType;

      if (inputType == "Commit")
      {
        AutoReceive = false;
        StreamWrapper = wrapper;
        return;
      }

      if (StreamWrapper != null && wrapper.StreamId == StreamWrapper.StreamId && !JustPastedIn)
      {
        return;
      }

      StreamWrapper = wrapper;

      ApiClient = new Client(wrapper.GetAccount());
      ApiClient.SubscribeCommitCreated(StreamWrapper.StreamId);

      ApiClient.OnCommitCreated += ApiClient_OnCommitCreated;
    }

    private void ApiClient_OnCommitCreated(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo e)
    {
      Message = "Expired";
      CurrentComponentState = "expired";
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"There is a newer commit available for this {InputType}");

      Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
      {
        if (AutoReceive)
        {
          ExpireSolution(true);
        }
        else
        {
          OnDisplayExpired(true);
        }
      });
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview("receive", AutoReceive ? "auto" : "manual");
      base.BeforeSolveInstance();
    }
  }

  public class ReceiveComponentWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> DataInput;
    StreamWrapper InputWrapper { get; set; }

    Action<ConcurrentDictionary<string, int>> InternalProgressAction;
    Action<string, Exception> ErrorAction;

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public int TotalObjectCount { get; set; } = 1;

    public Base ReceivedObject { get; set; }

    public Commit ReceivedCommit { get; set; }

    public ReceiveComponentWorker(GH_Component p) : base(p) { }

    public override WorkerInstance Duplicate() => new ReceiveComponentWorker(Parent);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      InputWrapper = ((ReceiveComponent)Parent).StreamWrapper;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      InternalProgressAction = (dict) =>
      {
        foreach (var kvp in dict)
        {
          ReportProgress(kvp.Key, (double)kvp.Value / TotalObjectCount);
        }
      };

      ErrorAction = (transportName, exception) =>
      {
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"{transportName}: {exception.Message}"));
      };

      var client = new Client(InputWrapper?.GetAccount());
      var remoteTransport = new ServerTransport(InputWrapper?.GetAccount(), InputWrapper?.StreamId);
      remoteTransport.TransportName = "R";

      if (((ReceiveComponent)Parent).JustPastedIn && ((ReceiveComponent)Parent).ReceivedObjectId != null)
      {
        Task.Run(async () =>
        {
          ReceivedObject = await Operations.Receive(
          objectId: ((ReceiveComponent)Parent).ReceivedObjectId,
          cancellationToken: CancellationToken,
          remoteTransport: remoteTransport,
          localTransport: new SQLiteTransport() { TransportName = "LC" }, // Local cache!
          onProgressAction: InternalProgressAction,
          onErrorAction: ErrorAction,
          onTotalChildrenCountKnown: (count) => TotalObjectCount = count
          );

          Done();
        });
        return;
      }

      // Means it's a copy paste of an empty non-init component; set the record and exit fast.
      if (((ReceiveComponent)Parent).JustPastedIn)
      {
        ((ReceiveComponent)Parent).JustPastedIn = false;
        return;
      }

      Task.Run(async () =>
      {
        Commit myCommit = null;
        if (InputWrapper.CommitId != null)
        {
          try
          {
            myCommit = await client.CommitGet(CancellationToken, InputWrapper.StreamId, InputWrapper.CommitId);
          }
          catch (Exception e)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, e.Message));
            Done();
            return;
          }
        }
        else
        {
          try
          {
            var stream = await client.StreamGet(InputWrapper.StreamId);
            var mainBranch = stream.branches.items.FirstOrDefault(b => b.name == (InputWrapper.BranchName ?? "main"));
            myCommit = mainBranch.commits.items[0];
          }
          catch (Exception e)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Could not get any commits from the stream's \"main\" branch."));
            Done();
            return;
          }
        }

        ReceivedCommit = myCommit;

        if (CancellationToken.IsCancellationRequested)
        {
          return;
        }

        ReceivedObject = await Operations.Receive(
          objectId: myCommit.referencedObject,
          cancellationToken: CancellationToken,
          remoteTransport: remoteTransport,
          localTransport: new SQLiteTransport() { TransportName = "LC" }, // Local cache!
          onProgressAction: InternalProgressAction,
          onErrorAction: ErrorAction,
          onTotalChildrenCountKnown: (count) => TotalObjectCount = count
          );

        if (CancellationToken.IsCancellationRequested)
        {
          return;
        }

        Done();
      });
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return;
      }

      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }

      ((ReceiveComponent)Parent).CurrentComponentState = "up_to_date";

      if (ReceivedCommit != null)
      {
        ((ReceiveComponent)Parent).LastInfoMessage = $"{ReceivedCommit.authorName} @ {ReceivedCommit.createdAt}: { ReceivedCommit.message} (id:{ReceivedCommit.id})";
      }

      ((ReceiveComponent)Parent).JustPastedIn = false;

      DA.SetData(1, ((ReceiveComponent)Parent).LastInfoMessage);

      if (ReceivedObject == null)
      {
        return;
      }

      ((ReceiveComponent)Parent).ReceivedObjectId = ReceivedObject.id;

      var dataList = ReceivedObject["@data"] as List<object>;
      var dataDictionary = ReceivedObject["@data"] as Dictionary<string, object>;

      if (dataList != null)
      {
        //DA.SetDataList(0, dataList.Select(item => new GH_SpeckleBase { Value = item as Base }));

        var list = dataList.Select(item => new GH_ObjectWrapper() { Value = Extras.Utilities.TryConvertItemToNative(item, ((ReceiveComponent)Parent).Converter) });
        DA.SetDataList(0, list);
        return;
      }
      else if (dataDictionary != null && dataDictionary.Values.First() is List<object>)
      {
        var tree = new GH_Structure<GH_ObjectWrapper>();
        var borkage = false;
        foreach (var kvp in dataDictionary)
        {
          if (kvp.Value is List<object>)
          {
            var pathObjects = kvp.Value as List<object>;
            var pathPieces = kvp.Key.Trim(new char[] { '{', '}' }).Split(';').Select(x => Int32.Parse(x)).ToArray();
            var path = new GH_Path(pathPieces);
            tree.AppendRange(pathObjects.Select(o => new GH_ObjectWrapper() { Value = Extras.Utilities.TryConvertItemToNative(o, ((ReceiveComponent)Parent).Converter) }), path);
          }
          else
          {
            borkage = true;
          }
        }
        if (!borkage)
        {
          DA.SetDataTree(0, tree);
          return;
        }
      }

      // Last attempt: just set the object out as received, and the user can unpack it via the other components.
      if (((ReceiveComponent)Parent).Converter.CanConvertToNative(ReceivedObject))
      {
        DA.SetData(0, new GH_ObjectWrapper() { Value = Extras.Utilities.TryConvertItemToNative(ReceivedObject, ((ReceiveComponent)Parent).Converter) });
      }
      else
      {
        DA.SetData(0, new GH_SpeckleBase() { Value = ReceivedObject });
      }
    }
  }

  public class ReceiveComponentAttributes : GH_ComponentAttributes
  {
    Rectangle ButtonBounds { get; set; }

    public ReceiveComponentAttributes(GH_Component owner) : base(owner) { }

    protected override void Layout()
    {
      base.Layout();

      var baseRec = GH_Convert.ToRectangle(Bounds);
      baseRec.Height += 26;

      var btnRec = baseRec;
      btnRec.Y = btnRec.Bottom - 26;
      btnRec.Height = 26;
      btnRec.Inflate(-2, -2);

      Bounds = baseRec;
      ButtonBounds = btnRec;
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);

      var state = ((ReceiveComponent)Owner).CurrentComponentState;

      if (channel == GH_CanvasChannel.Objects)
      {
        if (((ReceiveComponent)Owner).AutoReceive)
        {
          var autoSendButton = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Blue, "Auto Receive", 2, 0);

          autoSendButton.Render(graphics, Selected, Owner.Locked, false);
          autoSendButton.Dispose();
        }
        else
        {
          var palette = state == "expired" ? GH_Palette.Black : GH_Palette.Transparent;
          var text = state == "receiving" ? $"{((ReceiveComponent)Owner).OverallProgress:0.00%}" : "Receive";

          var button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, palette, text, 2, state == "expired" ? 10 : 0);
          button.Render(graphics, Selected, Owner.Locked, false);
          button.Dispose();
        }
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button == MouseButtons.Left)
      {
        if (((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
        {
          if (((ReceiveComponent)Owner).AutoReceive || ((ReceiveComponent)Owner).CurrentComponentState != "expired")
          {
            return GH_ObjectResponse.Handled;
          }
          ((ReceiveComponent)Owner).CurrentComponentState = "primed_to_receive";
          Owner.ExpireSolution(true);
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown(sender, e);
    }

    public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      // Double clicking the send button, even if the state is up to date, will do a "force receive"
      if (e.Button == MouseButtons.Left)
      {
        if (((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
        {
          if (((ReceiveComponent)Owner).CurrentComponentState == "receiving")
          {
            return GH_ObjectResponse.Handled;
          }

          if (((ReceiveComponent)Owner).AutoReceive)
          {
            ((ReceiveComponent)Owner).AutoReceive = false;
            Owner.OnDisplayExpired(true);
            return GH_ObjectResponse.Handled;
          }

          ((ReceiveComponent)Owner).CurrentComponentState = "primed_to_receive";
          Owner.ExpireSolution(true);
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown(sender, e);
    }
  }

}
