using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadTransMgr = Autodesk.AutoCAD.DatabaseServices.TransactionManager;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace KochSnowflake
{
    public class Class1
    {
        const int color1 = 3;
        const int color2 = 5;
   
        [CommandMethod("Snowflake")]
        public void Snowflake()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;
            AcadTransMgr transMgr = db.TransactionManager;

            Point2d point1 = new Point2d(2000, 2000);
            Point2d point2 = new Point2d(5000, 2000);
            Point2d point3 = new Point2d(3500, 4000);

            using (Transaction baseTransaction = transMgr.StartTransaction())
            {
                BlockTable BlockTable;
                BlockTable = baseTransaction.GetObject(db.BlockTableId, 
                    OpenMode.ForRead) as BlockTable;

                BlockTableRecord blockTableRecord;
                blockTableRecord = baseTransaction.GetObject(BlockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite) as BlockTableRecord;

                DrawLine(baseTransaction, blockTableRecord, point1, point2, 3);
                DrawLine(baseTransaction, blockTableRecord, point2, point3, 3);
                DrawLine(baseTransaction, blockTableRecord, point3, point1, 3);

                Fractal(baseTransaction, blockTableRecord, point1, point2, point3, 5);
                Fractal(baseTransaction, blockTableRecord, point2, point3, point1, 5);
                Fractal(baseTransaction, blockTableRecord, point3, point1, point2, 5);

                // Находим синии полилинии
                TypedValue[] filterlist = new TypedValue[2] {
                    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                    new TypedValue((int)DxfCode.Color, 5)
                };
                SelectionFilter filter = new SelectionFilter(filterlist);
                PromptSelectionResult selResult = editor.SelectAll(filter);

                if (selResult.Status != PromptStatus.OK)
                {
                    return;
                }

                ObjectId[] objectIds = selResult.Value.GetObjectIds();
            
                foreach (var ObjectId in objectIds)
                {
                    Polyline polyline = ObjectId.GetObject(OpenMode.ForRead) as Polyline;

                    // Удаляем полилинии
                    polyline.Erase(); 
                }

                baseTransaction.Commit();
            }
        }

        private void DrawLine(Transaction transaction, BlockTableRecord blockTableRecord, Point2d point1, Point2d point2, int color)
        {
            using (Polyline triangle = new Polyline())
            {
                triangle.AddVertexAt(0, point1, 0, 0, 0);
                triangle.AddVertexAt(1, point2, 0, 0, 0);

                triangle.ColorIndex = color;

                blockTableRecord.AppendEntity(triangle);
                transaction.AddNewlyCreatedDBObject(triangle, true);
            }
        }

        private int Fractal(Transaction transaction, BlockTableRecord blockTableRecord, Point2d point1, Point2d point2, Point2d point3, int countIteration = 0)
        {
            if (countIteration > 0)
            {
                // Средняя треть отрезка
                Point2d point4 = new Point2d((point2.X + 2 * point1.X) / 3, (point2.Y + 2 * point1.Y) / 3);
                Point2d point5 = new Point2d((2 * point2.X + point1.X) / 3, (point1.Y + 2 * point2.Y) / 3);

                // Координаты вершины угла
                Point2d ps = new Point2d((point2.X + point1.X) / 2, (point2.Y + point1.Y) / 2);
                Point2d pn = new Point2d((4 * ps.X - point3.X) / 3, (4 * ps.Y - point3.Y) / 3);

                // Рисуем треугольники
                DrawLine(transaction, blockTableRecord, point4, pn, color1);
                DrawLine(transaction, blockTableRecord, point5, pn, color1);
                DrawLine(transaction, blockTableRecord, point4, point5, color2);
     
                // Рекурсивный вызов необходимое кол-во раз
                Fractal(transaction, blockTableRecord, point4, pn, point5, countIteration - 1);
                Fractal(transaction, blockTableRecord, pn, point5, point4, countIteration - 1);
                Fractal(transaction, blockTableRecord, point1, point4, new Point2d((2 * point1.X + point3.X) / 3, (2 * point1.Y + point3.Y) / 3), countIteration - 1);
                Fractal(transaction, blockTableRecord, point5, point2, new Point2d((2 * point2.X + point3.X) / 3, (2 * point2.Y + point3.Y) / 3), countIteration - 1);
            }
            return countIteration;
        }
    }
}
