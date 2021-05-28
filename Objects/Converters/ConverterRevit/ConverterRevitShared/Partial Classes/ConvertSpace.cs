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
        public ApplicationPlaceholderObject SpaceToNative(BER.Area speckleArea)
        {
            return null;
        }

        public BER.Area SpaceToSpeckle(DB.Area revitArea, string units = null)
        {
            return null;
        }
    }
}