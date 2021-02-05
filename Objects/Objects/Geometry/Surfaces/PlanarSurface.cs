using System.Reactive;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry
{
  public class PlanarSurface : Base, ISurface
  {
    public Plane origin { get; set; }
    public Interval domainU { get; set; }
    public Interval domainV { get; set; }
    public Box bbox { get; }
    public double area { get; set; }
    
    public PlanarSurface()
    {
      this.units = Units.Meters;
      this.applicationId = null;
    }

    public PlanarSurface(Plane origin, Interval uInterval, Interval vInterval, string units = Units.Meters, string applicationId = null)
    {
      this.origin = origin;
      this.domainU = uInterval;
      this.domainV = vInterval;
      this.units = units;
      this.applicationId = applicationId;
    }
    
  }
}