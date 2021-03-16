﻿using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;

using RH = Rhino.Geometry;

using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using Objects.Other;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh : ISpeckleConverter
  {
    public string Description => "Default Speckle Kit for Rhino & Grasshopper";
    public string Name => nameof(ConverterRhinoGh);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { Applications.Rhino, Applications.Grasshopper };

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public RhinoDoc Doc { get; private set; }

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();

    public void SetContextDocument(object doc)
    {
      Doc = (RhinoDoc)doc;
    }

    // speckle user string for custom schemas
    // TODO: address consistency weak point, since SpeckleApplySchema command in the connector needs to match this exact string!!!
    string SpeckleSchemaKey = "SpeckleSchema";

    public Base ConvertToSpeckle(object @object)
    {
      RenderMaterial material = null;
      Base @base = null;
      if (@object is RhinoObject ro)
      {
        material = GetMaterial(ro);
        // special case for rhino objects that have a `SpeckleSchema` attribute
        // this will change in the near future
        if (ro.Attributes.GetUserString(SpeckleSchemaKey) != null)
        {
          @base = ConvertToSpeckleBE(ro);
          if (@base != null)
          {
            @base["renderMaterial"] = material;
            return @base;
          }
        }
        //conversion to built elem failed, revert to just send the base geom
        @object = ro.Geometry;
      }

      switch (@object)
      {
        case Point3d o:
          @base = PointToSpeckle(o);
          break;

        case Rhino.Geometry.Point o:
          @base = PointToSpeckle(o);
          break;

        case Vector3d o:
          @base = VectorToSpeckle(o);
          break;
        case RH.Interval o:
          @base = IntervalToSpeckle(o);
          break;
        case UVInterval o:
          @base = Interval2dToSpeckle(o);
          break;
        case RH.Line o:
          @base = LineToSpeckle(o);
          break;
        case LineCurve o:
          @base = LineToSpeckle(o);
          break;
        case RH.Plane o:
          @base = PlaneToSpeckle(o);
          break;
        case Rectangle3d o:
          @base = PolylineToSpeckle(o);
          break;
        case RH.Circle o:
          @base = CircleToSpeckle(o);
          break;
        case RH.Arc o:
          @base = ArcToSpeckle(o);
          break;
        case ArcCurve o:
          @base = ArcToSpeckle(o);
          break;
        case RH.Ellipse o:
          @base = EllipseToSpeckle(o);
          break;
        case RH.Polyline o:
          @base = PolylineToSpeckle(o) as Base;
          break;
        case NurbsCurve o:
          @base = CurveToSpeckle(o) as Base;
          break;
        case PolylineCurve o:
          @base = PolylineToSpeckle(o);
          break;
        case PolyCurve o:
          @base = PolycurveToSpeckle(o);
          break;
        case RH.Box o:
          @base = BoxToSpeckle(o);
          break;
        case RH.Mesh o:
          @base = MeshToSpeckle(o);
          break;
        case RH.Extrusion o:
          @base = BrepToSpeckle(o);
          break;
        case RH.Brep o:
          @base = BrepToSpeckle(o.DuplicateBrep());
          break;
        case NurbsSurface o:
          @base = SurfaceToSpeckle(o);
          break;
        default:
          throw new NotSupportedException();
      }

      if (material != null)
        @base["renderMaterial"] = material;

      return @base;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public Base ConvertToSpeckleBE(object @object)
    {
      // get schema if it exists
      RhinoObject obj = @object as RhinoObject;
      string schema = GetSchema(obj, out string[] args);

      switch (obj.Geometry)
      {
        case RH.Curve o:
          switch (schema)
          {
            case "Column":
              return CurveToSpeckleColumn(o);

            case "Beam":
              return CurveToSpeckleBeam(o);

            default:
              throw new NotSupportedException();
          }

        case RH.Brep o:
          switch (schema)
          {
            case "Floor":
              return BrepToSpeckleFloor(o);

            case "Ceiling":
              return BrepToSpeckleCeiling(o);

            case "Roof":
              return BrepToSpeckleRoof(o);

            case "Wall":
              return BrepToSpeckleWall(o);

            case "FaceWall":
              return BrepToFaceWall(o, args);

            case "DirectShape":
              return BrepToDirectShape(o, args);

            default:
              throw new NotSupportedException();
          }

        case RH.Extrusion o:
          switch (schema)
          {
            case "DirectShape":
              return ExtrusionToDirectShape(o, args);

            default:
              throw new NotSupportedException();
          }

        case RH.Mesh o:
          switch (schema)
          {
            case "DirectShape":
              return MeshToDirectShape(o, args);

            default:
              throw new NotSupportedException();
          }

        default:
          throw new NotSupportedException();
      }
    }

    public List<Base> ConvertToSpeckleBE(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckleBE(x)).ToList();
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point o:
          return PointToNative(o);

        case Vector o:
          return VectorToNative(o);

        case Interval o:
          return IntervalToNative(o);

        case Interval2d o:
          return Interval2dToNative(o);

        case Line o:
          return LineToNative(o);

        case Plane o:
          return PlaneToNative(o);

        case Circle o:
          return CircleToNative(o);

        case Arc o:
          return ArcToNative(o);

        case Ellipse o:
          return EllipseToNative(o);

        case Polyline o:
          return PolylineToNative(o);

        case Polycurve o:
          return PolycurveToNative(o);

        case Curve o:
          return CurveToNative(o);

        case Box o:
          return BoxToNative(o);

        case Mesh o:
          return MeshToNative(o);

        case Brep o:
          // Brep conversion should always fallback to mesh if it fails.
          var b = BrepToNative(o);
          if (b == null)
            return MeshToNative(o.displayValue);
          else
            return b;

        case Surface o:
          return SurfaceToNative(o);

        default:
          throw new NotSupportedException();
      }
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      if (@object is RhinoObject ro)
      {
        @object = ro.Geometry;
      }

      switch (@object)
      {
        case Point3d _:
          return true;

        case Rhino.Geometry.Point _:
          return true;

        case Vector3d _:
          return true;

        case RH.Interval _:
          return true;

        case UVInterval _:
          return true;

        case RH.Line _:
          return true;

        case LineCurve _:
          return true;

        case RH.Plane _:
          return true;

        case Rectangle3d _:
          return true;

        case RH.Circle _:
          return true;

        case RH.Arc _:
          return true;

        case ArcCurve _:
          return true;

        case RH.Ellipse _:
          return true;

        case RH.Polyline _:
          return true;

        case PolylineCurve _:
          return true;

        case PolyCurve _:
          return true;

        case NurbsCurve _:
          return true;

        case RH.Box _:
          return true;

        case RH.Mesh _:
          return true;

        case RH.Extrusion _:
          return true;

        case RH.Brep _:
          return true;

        case NurbsSurface _:
          return true;

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
          return true;

        case Vector _:
          return true;

        case Interval _:
          return true;

        case Interval2d _:
          return true;

        case Line _:
          return true;

        case Plane _:
          return true;

        case Circle _:
          return true;

        case Arc _:
          return true;

        case Ellipse _:
          return true;

        case Polyline _:
          return true;

        case Polycurve _:
          return true;

        case Curve _:
          return true;

        case Box _:
          return true;

        case Mesh _:
          return true;

        case Brep _:
          return true;

        case Surface _:
          return true;

        default:
          return false;
      }
    }

    private RenderMaterial GetMaterial(RhinoObject o)
    {
      var material = o.GetMaterial(true);
      var renderMaterial = new RenderMaterial();

      // If it's a default material use the display color.
      if (!material.HasId)
      {
        renderMaterial.diffuse = o.Attributes.DrawColor(Doc).ToArgb();
        return renderMaterial;
      }

      // Otherwise, extract what properties we can. 
      renderMaterial.name = material.Name;
      renderMaterial.diffuse = material.DiffuseColor.ToArgb();
      renderMaterial.emissive = material.EmissionColor.ToArgb();

      renderMaterial.opacity = 1 - material.Transparency;
      renderMaterial.metalness = material.Reflectivity;

      if (material.Name.ToLower().Contains("glass") && renderMaterial.opacity == 0)
      {
        renderMaterial.opacity = 0.3;
      }

      return renderMaterial;
    }

    private string GetSchema(RhinoObject obj, out string[] args)
    {
      args = null;

      // user string has format "DirectShape{[family], [type]}" if it is a directshape conversion
      // otherwise, it is just the schema type name
      string schema = obj.Attributes.GetUserString(SpeckleSchemaKey);

      if (schema == null)
        return null;

      string[] parsedSchema = schema.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
      if (parsedSchema.Length > 2) // there is incorrect formatting in the schema string!
        return null;
      if (parsedSchema.Length == 2)
        args = parsedSchema[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();
      return parsedSchema[0].Trim();
    }
  }
}