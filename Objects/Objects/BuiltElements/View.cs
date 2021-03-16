﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class View : Base
  {
    public string name { get; set; }

    public View() { }
  }

  public class View3D : View
  {
    public Point origin { get; set; }
    public Vector upDirection { get; set; }
    public Vector forwardDirection { get; set; }
    //public double zoom { get; set; }
    public bool isOrthogonal { get; set; } = false;

    public View3D() { }
  }

  public class View2D : View
  {
    //public Point topLeft { get; set; }
    //public Point bottomRight { get; set; }

    public View2D() { }
  }
}



//namespace Objects.BuiltElements.Revit
//{

//  public class RevitLevel : Level
//  {
//    public bool createView { get; set; }

//    public List<Parameter> parameters { get; set; }

//    public string elementId { get; set; }

//    public bool referenceOnly { get; set; }

//    public RevitLevel() { }

//    [SchemaInfo("Create level", "Creates a new Revit level unless one with the same elevation already exists")]
//    public RevitLevel(
//      [SchemaParamInfo("Level name. NOTE: updating level name is not supported")] string name,
//      [SchemaParamInfo("Level elevation. NOTE: updating level elevation is not supported, a new one will be created unless another level at the new elevation already exists.")] double elevation,
//      [SchemaParamInfo("If true, it creates an associated view in Revit. NOTE: only used when creating a level for the first time")] bool createView,
//      List<Parameter> parameters = null)
//    {
//      this.name = name;
//      this.elevation = elevation;
//      this.createView = createView;
//      this.parameters = parameters;
//      this.referenceOnly = false;
//    }

//    [SchemaInfo("Level by name", "Gets an existing Revit level by name")]
//    public RevitLevel(string name)
//    {
//      this.name = name;
//      this.referenceOnly = true;
//    }
//  }
//}
