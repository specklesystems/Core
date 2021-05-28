using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationPlaceholderObject GridLineToNative(BuiltElements.GridLine speckleGridLine)
    {
      if (speckleGridLine == null) return null;
      var speckleLine = speckleGridLine.baseLine;

      var revitLine = LineToNative(speckleLine);
      if (revitLine == null) throw new Exception("Gridline had no line");
      
      // Checking to see if the Gridline already exists in Revit
      var existingGrid = GetExistingElementByApplicationId(speckleGridLine.applicationId);

      Grid revitGrid = null;
      if (existingGrid != null)
      {
        try
        {
          revitGrid = (DB.Grid)existingGrid;
          (revitGrid.Location as LocationCurve).Curve = revitLine;
        }
        catch
        {
          //something went wrong, re-create it
        }
      }
      
      if(existingGrid == null)
      {
        revitGrid = Grid.Create(Doc, revitLine);
      }
      return new ApplicationPlaceholderObject
      {
        applicationId = speckleGridLine.applicationId,
        ApplicationGeneratedId = revitGrid.UniqueId,
        NativeObject = revitGrid
      };
    }

    public GridLine GridLineToSpeckle(DB.Grid revitGrid, string units = null)
    {
      var u = units ?? ModelUnits;
      var speckleLine = LineToSpeckle((DB.Line) revitGrid.Curve, u);
      var level = (DB.Level)Doc.GetElement(revitGrid.LevelId);
      var speckleLevel = level != null ? LevelToSpeckle(level) : null;
      var speckleGrid = new GridLine(speckleLine, speckleLevel);
      
      GetAllRevitParamsAndIds(speckleGrid, revitGrid);
      return speckleGrid;
    }
  }
}