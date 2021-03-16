using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using BrepEdge = Objects.Geometry.BrepEdge;
using BrepFace = Objects.Geometry.BrepFace;
using BrepLoop = Objects.Geometry.BrepLoop;
using BrepLoopType = Objects.Geometry.BrepLoopType;
using BrepTrim = Objects.Geometry.BrepTrim;
using BrepTrimType = Objects.Geometry.BrepTrimType;
using Circle = Objects.Geometry.Circle;
using ControlPoint = Objects.Geometry.ControlPoint;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Extrusion = Objects.Geometry.Extrusion;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;

using RH = Rhino.Geometry;

using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {

    // Convenience methods point:
    public double[ ] PointToArray(Point3d pt)
    {
      return new double[ ] { pt.X, pt.Y, pt.Z };
    }

    public double[ ] PointToArray(Point2d pt)
    {
      return new double[ ] { pt.X, pt.Y };
    }

    public double[ ] PointToArray(Point2f pt)
    {
      return new double[ ] { pt.X, pt.Y };
    }

    // Mass point converter
    public Point3d[ ] PointListToNative(IEnumerable<double> arr, string units)
    {
      var enumerable = arr.ToList();
      if (enumerable.Count % 3 != 0)throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");

      Point3d[ ] points = new Point3d[enumerable.Count / 3];
      var asArray = enumerable.ToArray();
      for (int i = 2, k = 0; i < enumerable.Count; i += 3)
        points[k++] = new Point3d(
          ScaleToNative(asArray[i - 2], units),
          ScaleToNative(asArray[i - 1], units),
          ScaleToNative(asArray[i], units));

      return points;
    }

    public double[ ] PointsToFlatArray(IEnumerable<Point3d> points)
    {
      return points.SelectMany(pt => PointToArray(pt)).ToArray();
    }

    public double[ ] PointsToFlatArray(IEnumerable<Point2f> points)
    {
      return points.SelectMany(pt => PointToArray(pt)).ToArray();
    }

    // Convenience methods vector:
    public double[ ] VectorToArray(Vector3d vc)
    {
      return new double[ ] { vc.X, vc.Y, vc.Z };
    }

    public Vector3d ArrayToVector(double[ ] arr)
    {
      return new Vector3d(arr[0], arr[1], arr[2]);
    }

    // Points
    // GhCapture?
    public Point PointToSpeckle(Point3d pt, string units = null)
    {
      return new Point(pt.X, pt.Y, pt.Z, units ?? ModelUnits);
    }

    // Rh Capture?
    public Rhino.Geometry.Point PointToNative(Point pt)
    {
      var myPoint = new Rhino.Geometry.Point(new Point3d(
        ScaleToNative(pt.x, pt.units),
        ScaleToNative(pt.y, pt.units),
        ScaleToNative(pt.z, pt.units)));

      return myPoint;
    }

    public Point PointToSpeckle(Rhino.Geometry.Point pt, string units = null)
    {
      return new Point(pt.Location.X, pt.Location.Y, pt.Location.Z, units ?? ModelUnits);
    }

    // Vectors
    public Vector VectorToSpeckle(Vector3d pt, string units = null)
    {
      return new Vector(pt.X, pt.Y, pt.Z, units ?? ModelUnits);
    }

    public Vector3d VectorToNative(Vector pt)
    {
      return new Vector3d(
        ScaleToNative(pt.x, pt.units),
        ScaleToNative(pt.y, pt.units),
        ScaleToNative(pt.z, pt.units));
    }

    // Interval
    public Interval IntervalToSpeckle(RH.Interval interval)
    {
      var speckleInterval = new Interval(interval.T0, interval.T1);
      return speckleInterval;
    }

    public RH.Interval IntervalToNative(Interval interval)
    {
      return new RH.Interval((double)interval.start, (double)interval.end);
    }

    // Interval2d
    public Interval2d Interval2dToSpeckle(UVInterval interval)
    {
      return new Interval2d(IntervalToSpeckle(interval.U), IntervalToSpeckle(interval.V));
    }

    public UVInterval Interval2dToNative(Interval2d interval)
    {
      return new UVInterval(IntervalToNative(interval.u), IntervalToNative(interval.v));
    }

    // Plane
    public Plane PlaneToSpeckle(RH.Plane plane, string units = null)
    {
      var u = units ?? ModelUnits;
      return new Plane(PointToSpeckle(plane.Origin, u), VectorToSpeckle(plane.Normal, u), VectorToSpeckle(plane.XAxis, u),
        VectorToSpeckle(plane.YAxis, u), u);
    }

    public RH.Plane PlaneToNative(Plane plane)
    {
      var xAxis = VectorToNative(plane.xdir);
      xAxis.Unitize();
      var yAxis = VectorToNative(plane.ydir);
      yAxis.Unitize();

      return new RH.Plane(PointToNative(plane.origin).Location, xAxis, yAxis);
    }

    // Line
    // Gh Line capture
    public Line LineToSpeckle(RH.Line line, string units = null)
    {
      var u = units ?? ModelUnits;
      var sLine = new Line(PointsToFlatArray(new Point3d[ ] { line.From, line.To }), u);
      sLine.length = line.Length;
      sLine.domain = new Interval(0, line.Length);
      var box = new RH.Box(line.BoundingBox);
      sLine.bbox = BoxToSpeckle(box, u);
      return sLine;
    }

    // Rh Line capture
    public Line LineToSpeckle(LineCurve line, string units = null)
    {
      var u = units ?? ModelUnits;
      var sLine = new Line(PointsToFlatArray(new Point3d[ ] { line.PointAtStart, line.PointAtEnd }), u)
      {
      domain = IntervalToSpeckle(line.Domain)
      };
      sLine.length = line.GetLength();
      var box = new RH.Box(line.GetBoundingBox(true));
      sLine.bbox = BoxToSpeckle(box, u);

      return sLine;
    }

    // Back again only to LINECURVES because we hate grasshopper and its dealings with rhinocommon
    public LineCurve LineToNative(Line line)
    {
      var myLine = new LineCurve(PointToNative(line.start).Location, PointToNative(line.end).Location);
      myLine.Domain = line.domain == null ? new RH.Interval(0, line.length) : IntervalToNative(line.domain);
      return myLine;
    }

    // Rectangles now and forever forward will become polylines
    public Polyline PolylineToSpeckle(Rectangle3d rect, string units = null)
    {
      var u = units ?? ModelUnits;
      var length = rect.Height * 2 + rect.Width * 2;
      var sPoly = new Polyline(
        PointsToFlatArray(new Point3d[ ] { rect.Corner(0), rect.Corner(1), rect.Corner(2), rect.Corner(3) }), u)
      {
      closed = true,
      area = rect.Area,
      bbox = BoxToSpeckle(new RH.Box(rect.BoundingBox), u),
      length = length,
      domain = new Interval(0, length)
      };

      return sPoly;
    }

    // Circle
    // Gh Capture
    public Circle CircleToSpeckle(RH.Circle circ, string units = null)
    {
      var u = units ?? ModelUnits;
      var circle = new Circle(PlaneToSpeckle(circ.Plane, u), circ.Radius, u);
      circle.domain = new Interval(0, 1);
      circle.length = 2 * Math.PI * circ.Radius;
      circle.area = Math.PI * circ.Radius * circ.Radius;
      return circle;
    }

    public ArcCurve CircleToNative(Circle circ)
    {
      RH.Circle circle = new RH.Circle(PlaneToNative(circ.plane), ScaleToNative((double)circ.radius, circ.units));

      var myCircle = new ArcCurve(circle);
      if (circ.domain != null)
        myCircle.Domain = IntervalToNative(circ.domain);

      return myCircle;
    }

    // Arc
    // Rh Capture can be a circle OR an arc
    public Base ArcToSpeckle(ArcCurve a, string units = null)
    {
      var u = units ?? ModelUnits;
      if (a.IsClosed)
      {
        RH.Circle preCircle;
        a.TryGetCircle(out preCircle);
        Circle myCircle = CircleToSpeckle(preCircle, u);
        myCircle.domain = IntervalToSpeckle(a.Domain);
        myCircle.length = a.GetLength();
        myCircle.bbox = BoxToSpeckle(new RH.Box(a.GetBoundingBox(true)), u);
        return myCircle;
      }
      else
      {
        RH.Arc preArc;
        a.TryGetArc(out preArc);
        Arc myArc = ArcToSpeckle(preArc, u);
        myArc.domain = IntervalToSpeckle(a.Domain);
        myArc.length = a.GetLength();
        myArc.bbox = BoxToSpeckle(new RH.Box(a.GetBoundingBox(true)), u);
        return myArc;
      }
    }

    // Gh Capture
    public Arc ArcToSpeckle(RH.Arc a, string units = null)
    {
      var u = units ?? ModelUnits;
      Arc arc = new Arc(PlaneToSpeckle(a.Plane, u), a.Radius, a.StartAngle, a.EndAngle, a.Angle, u);
      arc.endPoint = PointToSpeckle(a.EndPoint, u);
      arc.startPoint = PointToSpeckle(a.StartPoint, u);
      arc.midPoint = PointToSpeckle(a.MidPoint, u);
      arc.domain = new Interval(0, 1);
      arc.length = a.Length;
      arc.bbox = BoxToSpeckle(new RH.Box(a.BoundingBox()), u);
      return arc;
    }

    public ArcCurve ArcToNative(Arc a)
    {
      RH.Arc arc = new RH.Arc(PlaneToNative(a.plane), ScaleToNative((double)a.radius, a.units), (double)a.angleRadians);
      arc.StartAngle = (double)a.startAngle;
      arc.EndAngle = (double)a.endAngle;
      var myArc = new ArcCurve(arc);

      if (a.domain != null)
      {
        myArc.Domain = IntervalToNative(a.domain);
      }

      return myArc;
    }

    //Ellipse
    // TODO: handle conversions that define Radius1/Radius2 as major/minor instead of xaxis/yaxis 
    public Ellipse EllipseToSpeckle(RH.Ellipse e, string units = null)
    {
      var u = units ?? ModelUnits;
      var el = new Ellipse(PlaneToSpeckle(e.Plane, u), e.Radius1, e.Radius2, u);
      el.domain = new Interval(0, 1);
      el.length = e.ToNurbsCurve().GetLength();
      el.bbox = BoxToSpeckle(new RH.Box(e.ToNurbsCurve().GetBoundingBox(true)), u);
      el.area = Math.PI * e.Radius1 * e.Radius2; // Manual area computing, could not find the Rhino way...
      return el;
    }

    public RH.Curve EllipseToNative(Ellipse e)
    {
      RH.Ellipse elp = new RH.Ellipse(PlaneToNative(e.plane), ScaleToNative((double)e.firstRadius, e.units), ScaleToNative((double)e.secondRadius, e.units));
      var myEllp = elp.ToNurbsCurve();

      if (e.domain != null)
        myEllp.Domain = IntervalToNative(e.domain);

      if (e.trimDomain != null)
        myEllp = myEllp.Trim(IntervalToNative(e.trimDomain)).ToNurbsCurve();

      return myEllp;
    }

    // Polyline
    // Gh Capture
    public ICurve PolylineToSpeckle(RH.Polyline poly, string units = null) => PolylineToSpeckle(poly, null, units);

    public ICurve PolylineToSpeckle(RH.Polyline poly, Interval domain, string units = null)
    {
      var u = units ?? ModelUnits;
      if (poly.Count == 2)
      {
        var l = new Line(PointsToFlatArray(poly), u);
        l.domain = domain;
        return l;
      }

      var myPoly = new Polyline(PointsToFlatArray(poly), u);
      myPoly.closed = poly.IsClosed;

      if (myPoly.closed)
        myPoly.value.RemoveRange(myPoly.value.Count - 3, 3);

      myPoly.domain = domain;
      myPoly.bbox = BoxToSpeckle(new RH.Box(poly.BoundingBox), u);
      myPoly.length = poly.Length;

      // TODO: Area of 3d polyline cannot be resolved... 
      return myPoly;
    }

    // Rh Capture
    public Base PolylineToSpeckle(PolylineCurve poly, string units = null)
    {
      var u = units ?? ModelUnits;
      RH.Polyline polyline;

      if (poly.TryGetPolyline(out polyline))
      {
        var intervalToSpeckle = IntervalToSpeckle(poly.Domain);
        if (polyline.Count == 2)
        {
          var polylineToSpeckle = new Line(PointsToFlatArray(polyline), u)
          {
          domain = intervalToSpeckle
          };
          polylineToSpeckle.length = polyline.Length;
          var box = new RH.Box(poly.GetBoundingBox(true));
          polylineToSpeckle.bbox = BoxToSpeckle(box, u);
          return polylineToSpeckle;
        }

        var myPoly = new Polyline(PointsToFlatArray(polyline), u);
        myPoly.closed = polyline.IsClosed;

        if (myPoly.closed)
          myPoly.value.RemoveRange(myPoly.value.Count - 3, 3);

        myPoly.domain = intervalToSpeckle;
        myPoly.bbox = BoxToSpeckle(new RH.Box(poly.GetBoundingBox(true)), u);
        myPoly.length = poly.GetLength();
        return myPoly;
      }

      return null;
    }

    // Deserialise
    public PolylineCurve PolylineToNative(Polyline poly)
    {
      var points = PointListToNative(poly.value, poly.units).ToList();
      if (poly.closed)points.Add(points[0]);

      var myPoly = new PolylineCurve(points);
      if (poly.domain != null)
        myPoly.Domain = IntervalToNative(poly.domain);

      return myPoly;
    }

    // Polycurve
    // Rh Capture/Gh Capture
    public Polycurve PolycurveToSpeckle(PolyCurve p, string units = null)
    {
      var u = units ?? ModelUnits;
      var myPoly = new Polycurve();
      myPoly.closed = p.IsClosed;
      myPoly.domain = IntervalToSpeckle(p.Domain);
      myPoly.length = p.GetLength();
      myPoly.bbox = BoxToSpeckle(new RH.Box(p.GetBoundingBox(true)), u);

      var segments = new List<RH.Curve>();
      CurveSegments(segments, p, true);

      //let the converter pick the best type of curve
      myPoly.segments = segments.Select(s => (ICurve)ConvertToSpeckle(s)).ToList();

      return myPoly;
    }

    public PolyCurve PolycurveToNative(Polycurve p)
    {
      PolyCurve myPolyc = new PolyCurve();
      foreach (var segment in p.segments)
      {
        try
        {
          //let the converter pick the best type of curve
          myPolyc.AppendSegment((RH.Curve)ConvertToNative((Base)segment));
        }
        catch
        { }
      }

      if (p.domain != null)
        myPolyc.Domain = IntervalToNative(p.domain);

      return myPolyc;
    }

    // Curve
    public RH.Curve CurveToNative(ICurve curve)
    {
      switch (curve)
      {
        case Circle circle:
          return CircleToNative(circle);

        case Arc arc:
          return ArcToNative(arc);

        case Ellipse ellipse:
          return EllipseToNative(ellipse);

        case Curve crv:
          return NurbsToNative(crv);

        case Polyline polyline:
          return PolylineToNative(polyline);

        case Line line:
          return LineToNative(line);

        case Polycurve polycurve:
          return PolycurveToNative(polycurve);

        default:
          return null;
      }
    }

    public ICurve CurveToSpeckle(NurbsCurve curve, string units = null)
    {
      var u = units ?? ModelUnits;
      var tolerance = 0.0;
      Rhino.Geometry.Plane pln = Rhino.Geometry.Plane.Unset;
      curve.TryGetPlane(out pln, tolerance);

      if (curve.IsCircle(tolerance) && curve.IsClosed)
      {
        curve.TryGetCircle(out var getObj, tolerance);
        var cir = CircleToSpeckle(getObj, u);
        cir.domain = IntervalToSpeckle(curve.Domain);
        return cir;
      }

      if (curve.IsArc(tolerance))
      {
        curve.TryGetArc(out var getObj, tolerance);
        var arc = ArcToSpeckle(getObj, u);
        arc.domain = IntervalToSpeckle(curve.Domain);
        return arc;
      }

      if (curve.IsEllipse(tolerance) && curve.IsClosed)
      {
        curve.TryGetEllipse(pln, out var getObj, tolerance);
        var ellipse = EllipseToSpeckle(getObj, u);
        ellipse.domain = IntervalToSpeckle(curve.Domain);
      }

      if (curve.IsLinear(tolerance) || curve.IsPolyline()) // defaults to polyline
      {
        curve.TryGetPolyline(out var getObj);
        if (null != getObj)
        {
          return PolylineToSpeckle(getObj, IntervalToSpeckle(curve.Domain), u);
        }
      }

      return NurbsToSpeckle(curve, u);
    }

    public Curve NurbsToSpeckle(NurbsCurve curve, string units = null)
    {
      var u = units ?? ModelUnits;
      var tolerance = 0.0;

      curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out var poly);

      Polyline displayValue;

      if (poly.Count == 2)
      {
        displayValue = new Polyline();
        displayValue.value = new List<double> { poly[0].X, poly[0].Y, poly[0].Z, poly[1].X, poly[1].Y, poly[1].Z };
      }
      else
      {
        displayValue = PolylineToSpeckle(poly, u)as Polyline;
      }

      var myCurve = new Curve(displayValue, u);
      var nurbsCurve = curve.ToNurbsCurve();

      // increase knot multiplicity to (# control points + degree + 1)
      // add extra knots at start & end  because Rhino's knot multiplicity standard is (# control points + degree - 1)
      var knots = nurbsCurve.Knots.ToList();
      knots.Insert(0, knots[0]);
      knots.Insert(knots.Count - 1, knots[knots.Count - 1]);

      // Hack: Rebuild curve to prevent interior knot multiplicities.
      //var max = Math.Min(nurbsCurve.Points.Count-1, 3);
      //nurbsCurve = nurbsCurve.Rebuild(nurbsCurve.Points.Count, max, true);

      myCurve.weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList();
      myCurve.points = PointsToFlatArray(nurbsCurve.Points.Select(ctp => ctp.Location)).ToList();
      myCurve.knots = knots;
      myCurve.degree = nurbsCurve.Degree;
      myCurve.periodic = nurbsCurve.IsPeriodic;
      myCurve.rational = nurbsCurve.IsRational;
      myCurve.domain = IntervalToSpeckle(nurbsCurve.Domain);
      myCurve.closed = nurbsCurve.IsClosed;
      myCurve.length = nurbsCurve.GetLength();
      myCurve.bbox = BoxToSpeckle(new RH.Box(nurbsCurve.GetBoundingBox(true)), u);

      return myCurve;
    }

    public NurbsCurve NurbsToNative(Curve curve)
    {
      var ptsList = PointListToNative(curve.points, curve.units);

      var nurbsCurve = NurbsCurve.Create(false, curve.degree, ptsList);

      for (int j = 0; j < nurbsCurve.Points.Count; j++)
      {
        nurbsCurve.Points.SetPoint(j, ptsList[j], curve.weights[j]);
      }

      // check knot multiplicity to match Rhino's standard of (# control points + degree - 1)
      // skip extra knots at start & end if knot multiplicity is (# control points + degree + 1)
      int extraKnots = curve.knots.Count - nurbsCurve.Knots.Count;
      for (int j = 0; j < nurbsCurve.Knots.Count; j++)
      {
        if (extraKnots == 2)
          nurbsCurve.Knots[j] = curve.knots[j + 1];
        else
          nurbsCurve.Knots[j] = curve.knots[j];
      }

      nurbsCurve.Domain = IntervalToNative(curve.domain ?? new Interval(0, 1));
      return nurbsCurve;
    }

    // Box
    public Box BoxToSpeckle(RH.Box box, string units = null)
    {
      var u = units ?? ModelUnits;
      var speckleBox = new Box(PlaneToSpeckle(box.Plane, u), IntervalToSpeckle(box.X), IntervalToSpeckle(box.Y), IntervalToSpeckle(box.Z), u);
      speckleBox.area = box.Area;
      speckleBox.volume = box.Volume;

      return speckleBox;
    }

    public RH.Box BoxToNative(Box box)
    {
      return new RH.Box(PlaneToNative(box.basePlane), IntervalToNative(box.xSize), IntervalToNative(box.ySize), IntervalToNative(box.zSize));
    }

    // Meshes
    public Mesh MeshToSpeckle(RH.Mesh mesh, string units = null)
    {
      var u = units ?? ModelUnits;
      var verts = PointsToFlatArray(mesh.Vertices.ToPoint3dArray());

      var Faces = mesh.Faces.SelectMany(face =>
      {
        if (face.IsQuad)return new int[ ] { 1, face.A, face.B, face.C, face.D };
        return new int[ ] { 0, face.A, face.B, face.C };
      }).ToArray();

      var Colors = mesh.VertexColors.Select(cl => cl.ToArgb()).ToArray();

      var speckleMesh = new Mesh(verts, Faces, Colors, null, u);
      speckleMesh.volume = mesh.Volume();
      speckleMesh.bbox = BoxToSpeckle(new RH.Box(mesh.GetBoundingBox(true)), u);

      return speckleMesh;
    }

    public RH.Mesh MeshToNative(Mesh mesh)
    {
      RH.Mesh m = new RH.Mesh();
      m.Vertices.AddVertices(PointListToNative(mesh.vertices, mesh.units));

      int i = 0;

      while (i < mesh.faces.Count)
      {
        if (mesh.faces[i] == 0)
        {
          // triangle
          m.Faces.AddFace(new MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3]));
          i += 4;
        }
        else
        {
          // quad
          m.Faces.AddFace(new MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3], mesh.faces[i + 4]));
          i += 5;
        }
      }

      try
      {
        m.VertexColors.AppendColors(mesh.colors.Select(c => System.Drawing.Color.FromArgb((int)c)).ToArray());
      }
      catch
      { }

      if (mesh.textureCoordinates != null)
        for (int j = 0; j < mesh.textureCoordinates.Count; j += 2)
        {
          m.TextureCoordinates.Add(mesh.textureCoordinates[j], mesh.textureCoordinates[j + 1]);
        }

      return m;
    }

    private bool HasInvalidMultiplicity(NurbsCurve curve)
    {
      var knots = curve.Knots;
      var degree = curve.Degree;

      for (int i = degree; i < knots.Count - degree; i++)
      {
        var mult = knots.KnotMultiplicity(i);
        i += mult - 1;
        if (mult > degree - 2)
          return true;
      }
      return false;
    }

    /// <summary>
    /// Converts a Rhino <see cref="Rhino.Geometry.Brep"/> instance to a Speckle <see cref="Brep"/>
    /// </summary>
    /// <param name="brep">BREP to be converted.</param>
    /// <returns></returns>
    public Brep BrepToSpeckle(RH.Brep brep, string units = null)
    {
      var tol = 0.0;
      var u = units ?? ModelUnits;
      brep.Repair(tol); //should maybe use ModelAbsoluteTolerance ?
      foreach (var f in brep.Faces)
      {
        f.RebuildEdges(tol, false, false);
      }
      // Create complex
      var joinedMesh = new RH.Mesh();
      var mySettings = MeshingParameters.FastRenderMesh;
      joinedMesh.Append(RH.Mesh.CreateFromBrep(brep, mySettings));
      joinedMesh.Weld(Math.PI);
      joinedMesh.Vertices.CombineIdentical(true, true);
      joinedMesh.Compact();

      var spcklBrep = new Brep(displayValue: MeshToSpeckle(joinedMesh, u), provenance : Applications.Rhino, units : u);

      // Vertices, uv curves, 3d curves and surfaces
      spcklBrep.Vertices = brep.Vertices
        .Select(vertex => PointToSpeckle(vertex, u)).ToList();
      spcklBrep.Curve3D = brep.Curves3D
        .Select(curve3d =>
        {
          Rhino.Geometry.Curve crv = curve3d;
          if (crv is NurbsCurve nurbsCurve)
          {
            // Nurbs curves of degree 2 have weird support in Revit, so we up everything to degree 3.
            if (nurbsCurve.Degree < 3)
              nurbsCurve.IncreaseDegree(3);
            // Check for invalid multiplicity in the curves. This is also to better support Revit.
            var invalid = HasInvalidMultiplicity(nurbsCurve);

            // If the curve has invalid multiplicity and is not closed, rebuild with same number of points and degree.
            // TODO: Figure out why closed curves don't like this hack?
            if (invalid && !nurbsCurve.IsClosed)
              nurbsCurve = nurbsCurve.Rebuild(nurbsCurve.Points.Count, nurbsCurve.Degree, true);
            nurbsCurve.Domain = curve3d.Domain;
            crv = nurbsCurve;
          }
          var icrv = ConvertToSpeckle(crv)as ICurve;
          return icrv;

          // And finally convert to speckle
        }).ToList();
      spcklBrep.Curve2D = brep.Curves2D.ToList().Select(c =>
      {
        var nurbsCurve = c.ToNurbsCurve();
        //nurbsCurve.Knots.RemoveMultipleKnots(1, nurbsCurve.Degree, Doc.ModelAbsoluteTolerance );
        var rebuild = nurbsCurve.Rebuild(nurbsCurve.Points.Count, nurbsCurve.Degree, true);

        var crv = CurveToSpeckle(rebuild, Units.None);
        return crv;
      }).ToList();
      spcklBrep.Surfaces = brep.Surfaces
        .Select(srf => SurfaceToSpeckle(srf.ToNurbsSurface(), u)).ToList();
      spcklBrep.IsClosed = brep.IsSolid;
      spcklBrep.Orientation = (BrepOrientation)brep.SolidOrientation;

      // Faces
      spcklBrep.Faces = brep.Faces
        .Select(f => new BrepFace(
          spcklBrep,
          f.SurfaceIndex,
          f.Loops.Select(l => l.LoopIndex).ToList(),
          f.OuterLoop.LoopIndex,
          f.OrientationIsReversed
        )).ToList();

      // Edges
      spcklBrep.Edges = brep.Edges
        .Select(edge => new BrepEdge(
          spcklBrep,
          edge.EdgeCurveIndex,
          edge.TrimIndices(),
          edge.StartVertex.VertexIndex,
          edge.EndVertex.VertexIndex,
          edge.ProxyCurveIsReversed,
          IntervalToSpeckle(edge.Domain)
        )).ToList();

      // Loops
      spcklBrep.Loops = brep.Loops
        .Select(loop => new BrepLoop(
          spcklBrep,
          loop.Face.FaceIndex,
          loop.Trims.Select(t => t.TrimIndex).ToList(),
          (BrepLoopType)loop.LoopType
        )).ToList();

      // Trims
      spcklBrep.Trims = brep.Trims
        .Select(trim =>
        {
          var t = new BrepTrim(
            spcklBrep,
            trim.Edge?.EdgeIndex ?? -1,
            trim.Face.FaceIndex,
            trim.Loop.LoopIndex,
            trim.TrimCurveIndex,
            (int)trim.IsoStatus,
            (BrepTrimType)trim.TrimType,
            trim.IsReversed(),
            trim.StartVertex.VertexIndex,
            trim.EndVertex.VertexIndex
          );
          t.Domain = IntervalToSpeckle(trim.Domain);

          return t;
        })
        .ToList();
      spcklBrep.volume = brep.GetVolume();
      spcklBrep.bbox = BoxToSpeckle(new RH.Box(brep.GetBoundingBox(true)), u);
      spcklBrep.area = brep.GetArea();
      return spcklBrep;
    }

    /// <summary>
    /// Converts a Speckle <see cref="Brep"/> instance to a Rhino <see cref="Rhino.Geometry.Brep"/>
    /// </summary>
    /// <param name="brep">The Speckle Brep to convert</param>
    /// <returns></returns>
    /// <exception cref="Exception">Throws exception if the provenance is not Rhino</exception>
    public RH.Brep BrepToNative(Brep brep)
    {
      var tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      try
      {
        // TODO: Provenance exception is meaningless now, must change for provenance build checks.
        // if (brep.provenance != Speckle.Core.Kits.Applications.Rhino)
        //   throw new Exception("Unknown brep provenance: " + brep.provenance +
        //                       ". Don't know how to convert from one to the other.");

        var newBrep = new RH.Brep();
        brep.Curve3D.ForEach(crv => newBrep.AddEdgeCurve(CurveToNative(crv)));
        brep.Curve2D.ForEach(crv => newBrep.AddTrimCurve(CurveToNative(crv)));
        brep.Surfaces.ForEach(surf => newBrep.AddSurface(SurfaceToNative(surf)));
        brep.Vertices.ForEach(vert => newBrep.Vertices.Add(PointToNative(vert).Location, tol));
        brep.Edges.ForEach(edge =>
        {
          if (edge.Domain == null || (edge.Domain.start == edge.Curve.domain.start && edge.Domain.end == edge.Curve.domain.end))
            newBrep.Edges.Add(edge.Curve3dIndex);
          else
            newBrep.Edges.Add(edge.StartIndex, edge.EndIndex, edge.Curve3dIndex, IntervalToNative(edge.Domain), tol);
        });
        brep.Faces.ForEach(face =>
        {
          var f = newBrep.Faces.Add(face.SurfaceIndex);
          f.OrientationIsReversed = face.OrientationReversed;
        });

        brep.Loops.ForEach(loop =>
        {
          var f = newBrep.Faces[loop.FaceIndex];
          var l = newBrep.Loops.Add((RH.BrepLoopType)loop.Type, f);
          loop.Trims.ToList().ForEach(trim =>
          {
            RH.BrepTrim rhTrim;
            if (trim.EdgeIndex != -1)
              rhTrim = newBrep.Trims.Add(newBrep.Edges[trim.EdgeIndex], trim.IsReversed,
                newBrep.Loops[trim.LoopIndex], trim.CurveIndex);
            else if (trim.TrimType == BrepTrimType.Singular)
              rhTrim = newBrep.Trims.AddSingularTrim(newBrep.Vertices[trim.EndIndex],
                newBrep.Loops[trim.LoopIndex], (RH.IsoStatus)trim.IsoStatus, trim.CurveIndex);
            else
              rhTrim = newBrep.Trims.Add(trim.IsReversed, newBrep.Loops[trim.LoopIndex], trim.CurveIndex);

            rhTrim.IsoStatus = (IsoStatus)trim.IsoStatus;
            rhTrim.TrimType = (RH.BrepTrimType)trim.TrimType;
            rhTrim.SetTolerances(tol, tol);
          });
        });

        newBrep.Repair(tol);

        return newBrep;
      }
      catch (Exception e)
      {
        ConversionErrors.Add(new Error("Failed to convert brep.", e.Message));
        return null;
      }
    }

    // Extrusions
    // TODO: Research into how to properly create and recreate extrusions. Current way we compromise by transforming them into breps.
    public Brep BrepToSpeckle(Rhino.Geometry.Extrusion extrusion, string units = null)
    {
      return BrepToSpeckle(extrusion.ToBrep(), units ?? ModelUnits);

      //var myExtrusion = new SpeckleExtrusion( SpeckleCore.Converter.Serialise( extrusion.Profile3d( 0, 0 ) ), extrusion.PathStart.DistanceTo( extrusion.PathEnd ), extrusion.IsCappedAtBottom );

      //myExtrusion.PathStart = extrusion.PathStart.ToSpeckle();
      //myExtrusion.PathEnd = extrusion.PathEnd.ToSpeckle();
      //myExtrusion.PathTangent = extrusion.PathTangent.ToSpeckle();

      //var Profiles = new List<SpeckleObject>();
      //for ( int i = 0; i < extrusion.ProfileCount; i++ )
      //  Profiles.Add( SpeckleCore.Converter.Serialise( extrusion.Profile3d( i, 0 ) ) );

      //myExtrusion.Profiles = Profiles;
      //myExtrusion.Properties = extrusion.UserDictionary.ToSpeckle( root: extrusion );
      //myExtrusion.GenerateHash();
      //return myExtrusion;
    }

    // TODO: See above. We're no longer creating new extrusions. This is here just for backwards compatibility.
    public RH.Extrusion ExtrusionToNative(Extrusion extrusion)
    {
      RH.Curve outerProfile = CurveToNative((Curve)extrusion.profile);
      RH.Curve innerProfile = null;
      if (extrusion.profiles.Count == 2)innerProfile = CurveToNative((Curve)extrusion.profiles[1]);

      try
      {
        var IsClosed = extrusion.profile.GetType().GetProperty("IsClosed").GetValue(extrusion.profile, null)as bool?;
        if (IsClosed != true)
          outerProfile.Reverse();
      }
      catch
      { }

      var myExtrusion =
        RH.Extrusion.Create(outerProfile.ToNurbsCurve(), (double)extrusion.length, (bool)extrusion.capped);
      if (innerProfile != null)
        myExtrusion.AddInnerProfile(innerProfile);

      return myExtrusion;
    }

    //  Curve profile = null;
    //  try
    //  {
    //    var toNativeMethod = extrusion.Profile.GetType().GetMethod( "ToNative" );
    //    profile = ( Curve ) toNativeMethod.Invoke( extrusion.Profile, new object[ ] { extrusion.Profile } );
    //    if ( new string[ ] { "Polyline", "Polycurve" }.Contains( extrusion.Profile.Type ) )
    //      try
    //      {
    //        var IsClosed = extrusion.Profile.GetType().GetProperty( "IsClosed" ).GetValue( extrusion.Profile, null ) as bool?;
    //        if ( IsClosed != true )
    //        {
    //          profile.Reverse();
    //        }
    //      }
    //      catch { }

    //    //switch ( extrusion.Profile )
    //    //{
    //    //  case SpeckleCore.SpeckleCurve curve:
    //    //    profile = curve.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpecklePolycurve polycurve:
    //    //    profile = polycurve.ToNative();
    //    //    if ( !profile.IsClosed )
    //    //      profile.Reverse();
    //    //    break;
    //    //  case SpeckleCore.SpecklePolyline polyline:
    //    //    profile = polyline.ToNative();
    //    //    if ( !profile.IsClosed )
    //    //      profile.Reverse();
    //    //    break;
    //    //  case SpeckleCore.SpeckleArc arc:
    //    //    profile = arc.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpeckleCircle circle:
    //    //    profile = circle.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpeckleEllipse ellipse:
    //    //    profile = ellipse.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpeckleLine line:
    //    //    profile = line.ToNative();
    //    //    break;
    //    //  default:
    //    //    profile = null;
    //    //    break;
    //    //}
    //  }
    //  catch { }
    //  var x = new Extrusion();

    //  if ( profile == null ) return null;

    //  var myExtrusion = Extrusion.Create( profile.ToNurbsCurve(), ( double ) extrusion.Length, ( bool ) extrusion.Capped );

    //  myExtrusion.UserDictionary.ReplaceContentsWith( extrusion.Properties.ToNative() );
    //  return myExtrusion;
    //}

    // Proper explosion of polycurves:
    // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon
    public bool CurveSegments(List<RH.Curve> L, RH.Curve crv, bool recursive)
    {
      if (crv == null)
      {
        return false;
      }

      PolyCurve polycurve = crv as PolyCurve;

      if (polycurve != null)
      {
        if (recursive)
        {
          polycurve.RemoveNesting();
        }

        RH.Curve[ ] segments = polycurve.Explode();

        if (segments == null)
        {
          return false;
        }

        if (segments.Length == 0)
        {
          return false;
        }

        if (recursive)
        {
          foreach (RH.Curve S in segments)
          {
            CurveSegments(L, S, recursive);
          }
        }
        else
        {
          foreach (RH.Curve S in segments)
          {
            L.Add(S.DuplicateShallow()as RH.Curve);
          }
        }

        return true;
      }

      //Nothing else worked, lets assume it's a nurbs curve and go from there...
      var nurbs = crv.ToNurbsCurve();
      if (nurbs == null)
      {
        return false;
      }

      double t0 = nurbs.Domain.Min;
      double t1 = nurbs.Domain.Max;
      double t;

      int LN = L.Count;

      do
      {
        if (!nurbs.GetNextDiscontinuity(Continuity.C1_locus_continuous, t0, t1, out t))
        {
          break;
        }

        var trim = new RH.Interval(t0, t);
        if (trim.Length < 1e-10)
        {
          t0 = t;
          continue;
        }

        var M = nurbs.DuplicateCurve();
        M = M.Trim(trim);
        if (M.IsValid)
        {
          L.Add(M);
        }

        t0 = t;
      } while (true);

      if (L.Count == LN)
      {
        L.Add(nurbs);
      }

      return true;
    }

    public NurbsSurface SurfaceToNative(Geometry.Surface surface)
    {
      // Create rhino surface
      var points = surface.GetControlPoints().Select(l => l.Select(p =>
        new ControlPoint(
          ScaleToNative(p.x, p.units),
          ScaleToNative(p.y, p.units),
          ScaleToNative(p.z, p.units),
          p.weight,
          p.units)).ToList()).ToList();

      var result = NurbsSurface.Create(3, surface.rational, surface.degreeU + 1, surface.degreeV + 1,
        points.Count, points[0].Count);

      // Set knot vectors
      for (int i = 0; i < surface.knotsU.Count; i++)
      {
        result.KnotsU[i] = surface.knotsU[i];
      }

      for (int i = 0; i < surface.knotsV.Count; i++)
      {
        result.KnotsV[i] = surface.knotsV[i];
      }

      // Set control points
      for (var i = 0; i < points.Count; i++)
      {
        for (var j = 0; j < points[i].Count; j++)
        {
          var pt = points[i][j];
          result.Points.SetPoint(i, j, pt.x * pt.weight, pt.y * pt.weight, pt.z * pt.weight);
          result.Points.SetWeight(i, j, pt.weight);
        }
      }

      // Return surface
      return result;
    }

    public List<List<ControlPoint>> ControlPointsToSpeckle(NurbsSurfacePointList controlPoints, string units = null)
    {
      var points = new List<List<ControlPoint>>();
      for (var i = 0; i < controlPoints.CountU; i++)
      {
        var row = new List<ControlPoint>();
        for (var j = 0; j < controlPoints.CountV; j++)
        {
          var pt = controlPoints.GetControlPoint(i, j);
          var pos = pt.Location;
          row.Add(new ControlPoint(pos.X, pos.Y, pos.Z, pt.Weight, units ?? ModelUnits));
        }

        points.Add(row);
      }

      return points;
    }

    public Geometry.Surface SurfaceToSpeckle(NurbsSurface surface, string units = null)
    {
      var u = units ?? ModelUnits;
      var result = new Geometry.Surface
      {
        degreeU = surface.OrderU - 1,
        degreeV = surface.OrderV - 1,
        rational = surface.IsRational,
        closedU = surface.IsClosed(0),
        closedV = surface.IsClosed(1),
        domainU = IntervalToSpeckle(surface.Domain(0)),
        domainV = IntervalToSpeckle(surface.Domain(1)),
        knotsU = surface.KnotsU.ToList(),
        knotsV = surface.KnotsV.ToList()
      };
      result.units = u;

      result.SetControlPoints(ControlPointsToSpeckle(surface.Points));
      result.bbox = BoxToSpeckle(new RH.Box(surface.GetBoundingBox(true)), u);

      return result;
    }
  }
}
