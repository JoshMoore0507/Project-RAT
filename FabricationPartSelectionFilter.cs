using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace TPMechanical.QAQC
{
    public sealed class FabricationPartSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            return element is FabricationPart part && !part.IsAHanger();
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
