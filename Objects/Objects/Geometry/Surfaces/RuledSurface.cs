using System.Collections.Generic;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry
{
  public class RuledSurface : Base, ISurface
  {
    public List<ICurve> curves { get; set; }
    public Point pointA { get; set; }
    public Point pointB { get; set; }
    public bool isExtruded { get; set; }

    public RuledSurface()
    {
      
    }
    public RuledSurface(string units = Units.Meters, string applicationId = null)
    {
      this.units = units;
      this.applicationId = applicationId;
    }

    public Box bbox { get; set; }
    public double area { get; set; }
    public Interval domainU { get; set; }
    public Interval domainV { get; set; }
  }
}