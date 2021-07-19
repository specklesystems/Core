﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Alignment : Base
  {
    public ICurve baseCurve { get; set; }

    public string name { get; set; }

    public double startStation { get; set; }

    public double endStation { get; set; }

    /// <summary>
    /// Station equation arrays should contain doubles indicating raw station back, station back, and station ahead
    /// </summary>
    public List<double[]> stationEquations { get; set; }

    /// <summary>
    /// Station equation direction for the corresponding station equation should be true for increasing or false for decreasing
    /// </summary>
    public List<bool> stationEquationDirections { get; set; }

    public Alignment() { }

  }
}
