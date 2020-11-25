﻿using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> RoofToNative(IRoof speckleRoof)
    {
      if (speckleRoof.outline == null)
      {
        throw new Exception("Only outline based Floor are currently supported.");
      }

      DB.RoofBase revitRoof = null;
      DB.Level level = null;
      var outline = CurveToNative(speckleRoof.outline);

      var speckleRevitRoof = speckleRoof as RevitRoof;
      if (speckleRevitRoof != null)
      {
        level = LevelToNative(speckleRevitRoof.level);
      }
      else
      {
        level = LevelToNative(LevelFromCurve(outline.get_Item(0)));
      }

      var roofType = GetElementType<RoofType>(speckleRoof);

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element.
      var docObj = GetExistingElementByApplicationId(speckleRoof.applicationId);
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      switch (speckleRoof)
      {
        case RevitExtrusionRoof speckleExtrusionRoof:
          {
            var referenceLine = LineToNative(speckleExtrusionRoof.referenceLine);
            var norm = GetPerpendicular(referenceLine.GetEndPoint(0) - referenceLine.GetEndPoint(1)).Negate();
            ReferencePlane plane = Doc.Create.NewReferencePlane(referenceLine.GetEndPoint(0),
              referenceLine.GetEndPoint(1),
              norm,
              Doc.ActiveView);
            //create floor without a type
            revitRoof = Doc.Create.NewExtrusionRoof(outline, plane, level, roofType, speckleExtrusionRoof.start, speckleExtrusionRoof.end);
            break;
          }

        case RevitFootprintRoof speckleFootprintRoof:
          {
            ModelCurveArray curveArray = new ModelCurveArray();
            var revitFootprintRoof = Doc.Create.NewFootPrintRoof(outline, level, roofType, out curveArray);
            for (var i = 0; i < curveArray.Size; i++)
            {
              var poly = speckleFootprintRoof.outline as Polycurve;
              revitFootprintRoof.set_DefinesSlope(curveArray.get_Item(i), ((Base)poly.segments[i]).GetMemberSafe<bool>("isSloped"));
              try
              {
                revitFootprintRoof.set_SlopeAngle(curveArray.get_Item(i), ((Base)poly.segments[i]).GetMemberSafe<double>("slopeAngle"));
              }
              catch { }
              revitFootprintRoof.set_Offset(curveArray.get_Item(i), ((Base)poly.segments[i]).GetMemberSafe<double>("offset"));
            }
            if (speckleFootprintRoof.cutOffLevel != null)
            {
              var cutOffLevel = LevelToNative(speckleFootprintRoof.cutOffLevel);
              TrySetParam(revitFootprintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM, cutOffLevel);
            }

            revitRoof = revitFootprintRoof;
            break;
          }
        default:
          ConversionErrors.Add(new Error("Cannot create Roof", "Roof type not supported"));
          throw new Exception("Roof type not supported");

      }

      Doc.Regenerate();

      try
      {
        MakeOpeningsInRoof(revitRoof, speckleRoof.voids.ToList());
      }
      catch (Exception ex)
      {
        ConversionErrors.Add(new Error("Could not create holes in roof", ex.Message));
      }

      if (speckleRevitRoof != null)
      {
        SetElementParams(revitRoof, speckleRevitRoof);
      }

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleRevitRoof.applicationId, ApplicationGeneratedId = revitRoof.UniqueId } };

      // TODO: nested elements.

      return placeholders;
    }

    private void MakeOpeningsInRoof(DB.RoofBase roof, List<ICurve> holes)
    {
      foreach (var hole in holes)
      {
        var curveArray = CurveToNative(hole);
        Doc.Create.NewOpening(roof, curveArray, true);
      }
    }

    private IRoof RoofToSpeckle(DB.RoofBase revitRoof)
    {
      var profiles = GetProfiles(revitRoof);

      var speckleRoof = new RevitRoof();

      switch (revitRoof)
      {
        //assigning correct type for when going back in Revit
        case FootPrintRoof footPrintRoof:
          {
            var baseLevelParam = footPrintRoof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
            var cutOffLevelParam = footPrintRoof.get_Parameter(BuiltInParameter.ROOF_UPTO_LEVEL_PARAM);
            var speckleFootprintRoof = new RevitFootprintRoof
            {
              level = ConvertAndCacheLevel(baseLevelParam),
              cutOffLevel = ConvertAndCacheLevel(cutOffLevelParam)
            };

            speckleRoof = speckleFootprintRoof;
            break;
          }
        case ExtrusionRoof revitExtrusionRoof:
          {
            var speckleExtrusionRoof = new RevitExtrusionRoof
            {
              start = (double)ParameterToSpeckle(
                revitExtrusionRoof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM)),
              end = (double)ParameterToSpeckle(revitExtrusionRoof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM))
            };
            var plane = revitExtrusionRoof.GetProfile().get_Item(0).SketchPlane.GetPlane();
            speckleExtrusionRoof.referenceLine = new Line(PointToSpeckle(plane.Origin.Add(plane.XVec.Normalize().Negate())), PointToSpeckle(plane.Origin), ModelUnits); //TODO: test!
            var baseLevelParam = revitExtrusionRoof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
            speckleExtrusionRoof.level = ConvertAndCacheLevel(baseLevelParam);
            speckleRoof = speckleExtrusionRoof;
            break;
          }
      }

      speckleRoof.type = Doc.GetElement(revitRoof.GetTypeId()).Name;

      // TODO handle case if not one of our supported roofs
      if (profiles.Any())
      {
        speckleRoof.outline = profiles[0];
        if (profiles.Count > 1)
        {
          speckleRoof.voids = profiles.Skip(1).ToList();
        }
      }

      AddCommonRevitProps(speckleRoof, revitRoof);

      (speckleRoof.displayMesh.faces, speckleRoof.displayMesh.vertices) = GetFaceVertexArrayFromElement(revitRoof, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      // TODO
      var hostedElements = revitRoof.FindInserts(true, true, true, true);

      return speckleRoof;
    }

    //Nesting the various profiles into a polycurve segments
    private List<ICurve> GetProfiles(DB.RoofBase roof)
    {
      // TODO handle case if not one of our supported roofs
      var profiles = new List<ICurve>();

      switch (roof)
      {
        case FootPrintRoof footprint:
          {
            ModelCurveArrArray crvLoops = footprint.GetProfiles();

            for (var i = 0; i < crvLoops.Size; i++)
            {
              var crvLoop = crvLoops.get_Item(i);
              var poly = new Polycurve(ModelUnits);
              foreach (DB.ModelCurve curve in crvLoop)
              {
                if (curve == null)
                {
                  continue;
                }

                var segment = CurveToSpeckle(curve.GeometryCurve) as Base; //it's a safe casting, should be improved tho...
                segment["slopeAngle"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_SLOPE));
                segment["isSloped"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_CURVE_IS_SLOPE_DEFINING));
                segment["offset"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET));
                poly.segments.Add(segment as ICurve);

                //roud profiles are returned duplicated!
                if (curve is ModelArc arc && RevitVersionHelper.IsCurveClosed(arc.GeometryCurve))
                {
                  break;
                }
              }
              profiles.Add(poly);
            }

            break;
          }
        case ExtrusionRoof extrusion:
          {
            var crvloop = extrusion.GetProfile();
            var poly = new Polycurve(ModelUnits);
            foreach (DB.ModelCurve curve in crvloop)
            {
              if (curve == null)
              {
                continue;
              }

              poly.segments.Add(CurveToSpeckle(curve.GeometryCurve));
            }
            profiles.Add(poly);
            break;
          }
      }
      return profiles;
    }
  }
}