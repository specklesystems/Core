using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit
{
    public partial class ConverterRevit
    {
        public ApplicationPlaceholderObject AreaToNative(BER.Area speckleArea)
        {
            // Possibly can only be created in when ActiveView type is an AreaPlan view
            // May need to temporarily create an AreaPlan in order to be allowed to create this
            // Need AreaBoundaryLines -> can retrieve from Surface
            //Doc.Create.NewArea()
            return null;
        }

        public BER.Area AreaToSpeckle(DB.Area revitArea, string units = null)
        {
            var profiles = GetProfiles(revitArea);
            var speckleArea = new BER.Area();

            var u = units ?? ModelUnits;

            // Same BuiltInParameter used for both Areas and Rooms, nice one Autodesk...
            speckleArea.name = revitArea.get_Parameter(DB.BuiltInParameter.ROOM_NAME).AsString();
            speckleArea.number = revitArea.Number;
            speckleArea.area = DB.UnitUtils.Convert
            (
                revitArea.get_Parameter(DB.BuiltInParameter.ROOM_AREA).AsDouble(),
                DB.DisplayUnitType.DUT_SQUARE_FEET,
                DB.DisplayUnitType.DUT_SQUARE_METERS
            ); // Needs to convert to the user's units not hard-coded to m2
            speckleArea.center = (Geometry.Point) LocationToSpeckle(revitArea);
            speckleArea.level = ConvertAndCacheLevel(revitArea, DB.BuiltInParameter.ROOM_LEVEL_ID);
            speckleArea.outline = profiles[0];
            
            if (profiles.Count > 1)
            {
                speckleArea.voids = profiles.Skip(1).ToList();
            }

            GetAllRevitParamsAndIds(speckleArea, revitArea);

            var outlineCurvePoints = revitArea
                .GetBoundarySegments(new DB.SpatialElementBoundaryOptions())
                .FirstOrDefault()
                .Select(x => x.GetCurve().Evaluate(0, true))
                .ToList();

            var tessellatedFace = new DB.TessellatedFace(outlineCurvePoints, DB.ElementId.InvalidElementId);
            // MeshToNative() can take a TessellatedFace. We cn use the TesselatedThingyBuilder and its AddFace method to create a TesselatedFace thing
            

            var polyCoords = outlineCurvePoints.SelectMany(pt => PointToSpeckle(pt).ToList());
            Geometry.Polyline polyLine = new Geometry.Polyline(polyCoords, ModelUnits) { closed = true };
            speckleArea.displayValue = polyLine; 
            return speckleArea;
        }

        private List<ICurve> GetProfiles(DB.Area area)
        {
            var profiles = new List<ICurve>();
            var boundaries = area.GetBoundarySegments(new DB.SpatialElementBoundaryOptions());
            foreach (var loop in boundaries)
            {
                var poly = new Geometry.Polycurve(ModelUnits);
                foreach (var segment in loop)
                {
                    var c = segment.GetCurve();

                    if (c == null)
                    {
                        continue;
                    }

                    poly.segments.Add(CurveToSpeckle(c));
                }

                profiles.Add(poly);
            }

            return profiles;
        }
    }
}