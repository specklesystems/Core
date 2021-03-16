﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class AdaptiveComponent : Base
  {
    public string type { get; set; }
    public string family { get; set; }
    public List<Point> basePoints { get; set; }
    public bool flipped { get; set; }
    public string elementId { get; set; }
    public List<Parameter> parameters { get; set; }

    public AdaptiveComponent() { }

    [SchemaInfo("AdaptiveComponent", "Creates a Revit adaptive component by points")]
    public AdaptiveComponent(string type, string family, List<Point> basePoints, bool flipped = false, List<Parameter> parameters = null)
    {
      this.type = type;
      this.family = family;
      this.basePoints = basePoints;
      this.flipped = flipped;
      this.parameters = parameters;
    }
  }
}