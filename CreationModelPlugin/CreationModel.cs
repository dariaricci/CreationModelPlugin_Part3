using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;


            Level level1 = GetLevels(doc)
                 .Where(x => x.Name.Equals("Уровень 1"))
                 .FirstOrDefault();
            Level level2 = GetLevels(doc)
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            Transaction ts = new Transaction(doc, "Создание элементов");
            ts.Start();
            CreateWallsRectangle(doc, width, depth, level1, level2);
            List<Wall> listWall = GetWalls(doc);
            AddDoor(doc, level1, listWall[0]);
            for (int i = 1; i < 4; i++)
            {
                AddWindow(doc, level1, listWall[i]);
            }
            AddRoof(doc, level2, listWall[0]);
          
            ts.Commit();
            return Result.Succeeded;
        }

        public void CreateWallsRectangle(Document doc, double width, double depth, Level baseConstraint, Level topConstraint)
        {
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, baseConstraint.Id, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(topConstraint.Id);
            }
        }

        public static List<Level> GetLevels(Document doc)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();
            return listLevel;
        }
        public static List<Wall> GetWalls(Document doc)
        {
            List<Wall> listWall = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .OfType<Wall>()
                .ToList();
            return listWall;
        }
        public void AddDoor(Document doc, Level baseConstraint, Wall wall)
        {

            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2032 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, baseConstraint, StructuralType.NonStructural);
        }
        public void AddWindow(Document doc, Level baseConstraint, Wall wall)
        {

            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();
            FamilyInstance window=doc.Create.NewFamilyInstance(point, windowType, wall, baseConstraint, StructuralType.NonStructural);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(900, UnitTypeId.Millimeters));
        }
        public void AddRoof(Document doc, Level level, Wall wall)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();
            LocationCurve curve = wall.Location as LocationCurve;
            CurveArray curveArray = new CurveArray();
            XYZ s1 = new XYZ(-wall.Width/2, -wall.Width / 2, UnitUtils.ConvertToInternalUnits(4000, UnitTypeId.Millimeters));
            XYZ e1 = new XYZ(wall.Width/2, -wall.Width / 2, UnitUtils.ConvertToInternalUnits(4000, UnitTypeId.Millimeters));
            XYZ h = new XYZ(0, 0, UnitUtils.ConvertToInternalUnits(4000, UnitTypeId.Millimeters));
            XYZ sP = curve.Curve.GetEndPoint(0)+s1;
            XYZ eP = curve.Curve.GetEndPoint(1)+e1;
            XYZ mP = (sP + eP) / 2+h;
            curveArray.Append(Line.CreateBound(sP, mP));
            curveArray.Append(Line.CreateBound(mP, eP));
            ReferencePlane plane = doc.Create.NewReferencePlane(sP, eP, h, doc.ActiveView);
            ExtrusionRoof extrusionroof=doc.Create.NewExtrusionRoof(curveArray, plane, level, roofType, 0, UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters)+ wall.Width);
       

        }
    }
}
