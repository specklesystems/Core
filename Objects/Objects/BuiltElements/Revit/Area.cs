using Objects.Geometry;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
    public class Area : Base, IHasArea, IDisplayMesh
    {
        public string name { get; set; }
        public string number { get; set; }
        public double area { get; set; }
        public Level level { get; set; }
        public Point center { get; set; }
        public List<ICurve> voids { get; set; } = new List<ICurve>();
        public ICurve outline { get; set; }

        [DetachProperty]
        public Mesh displayMesh { get; set; }
        public Polyline displayValue { get; set; }

        public Area() { }
    }
}