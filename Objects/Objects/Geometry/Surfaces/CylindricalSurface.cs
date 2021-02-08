using Objects.Primitive;
using Speckle.Core.Models;

namespace Objects.Geometry.Surfaces
{
  public class CylindricalSurface: Base, ISurface
  {
    public Point origin { get; set; }
    public Vector axis { get; set; }
    public Vector radiusVectorX { get; set; }
    public Vector radiusVectorY { get; set; }
    
    public Box bbox { get; }
    public double area { get; set; }
    public Interval domainU { get; set; }
    public Interval domainV { get; set; }
  }
}