﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Duct : Base
  {
    public Line baseLine { get; set; }
    public double width { get; set; }
    public double height { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }
    public double velocity { get; set; }

    public Duct() { }

    [SchemaInfo("Duct", "Creates a Speckle duct")]
    public Duct(Line baseLine, double width, double height, double diameter, double velocity = 0, string units = Units.Meters)
    {
      this.baseLine = baseLine;
      this.width = width;
      this.height = height;
      this.diameter = diameter;
      this.velocity = velocity;
      this.units = units;
    }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitDuct : Duct
  {
    public string family { get; set; }
    public string type { get; set; }
    public string systemName { get; set; }
    public string systemType { get; set; }
    public Level level { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public RevitDuct() { }

    [SchemaInfo("RevitDuct", "Creates a Revit duct")]
    public RevitDuct(string family, string type, Line baseLine, 
      string systemName, string systemType, Level level, 
      double width, double height, double diameter, 
      double velocity = 0, List<Parameter> parameters = null, string units = Units.Meters)
    {
      this.baseLine = baseLine;
      this.family = family;
      this.type = type;
      this.width = width;
      this.height = height;
      this.diameter = diameter;
      this.velocity = velocity;
      this.systemName = systemName;
      this.systemType = systemType;
      this.parameters = parameters;
      this.level = level;
      this.units = units;
    }
  }

}
