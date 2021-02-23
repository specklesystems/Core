using Objects.Primitive;
using Speckle.Core.Models;

namespace Objects.Geometry
{
  public class CylindricalSurface: Base, ISurface
  {
    public CylindricalSurface(){}
    public CylindricalSurface(Point origin, Vector axis, Vector radiusVectorX, Vector radiusVectorY)
    {
      this.origin = origin;
      this.axis = axis;
      this.radiusVectorX = radiusVectorX;
      this.radiusVectorY = radiusVectorY;
      this.domainU = new Interval(0, 1);
      this.domainV = new Interval(0, 1);
    }

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