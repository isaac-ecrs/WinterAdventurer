using System.Collections;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using WinterAdventurer.Library.Extensions;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library
{
    public class ExcelUtilities
    {
        public static void ImportExcel(MemoryStream stream)
        {
            using(var package = new ExcelPackage())
            {
                var classSelections = ParseClassSelections(package);
            }
        }

        public static List<Workshop> ParseWorkshops(ExcelPackage package)
        {
            var periodOneSheet = package
                                    .Workbook
                                    .Worksheets
                                    .FirstOrDefault(ws =>
                                        ws.Name.Contains("MorningFirstPeriod"));

            var periodTwoSheet = package
                                    .Workbook
                                    .Worksheets
                                    .FirstOrDefault(ws =>
                                        ws.Name.Contains("MorningSecondPeriod"));

            var periodThreeSheet = package
                                    .Workbook
                                    .Worksheets
                                    .FirstOrDefault(ws =>
                                        ws.Name.Contains("AfternoonPeriod"));

            if (periodOneSheet == null || periodOneSheet == default)
            {
                throw new InvalidDataException("Could not find the Morning First Period worksheet.");
            }

            if (periodTwoSheet == null || periodTwoSheet == default)
            {
                throw new InvalidDataException("Could not find the Morning Second Period worksheet.");
            }

            if (periodThreeSheet == null || periodThreeSheet == default)
            {
                throw new InvalidDataException("Could not find the Afternoon Period worksheet.");
            }
            
            return 
                CollectWorkshops(periodOneSheet)
                .Union(CollectWorkshops(periodTwoSheet))
                .Union(CollectWorkshops(periodThreeSheet)).ToList();
        }

        public static List<Workshop> CollectWorkshops(ExcelWorksheet sheet)
        {
            var rows = sheet.Dimension.Rows;
            var columns = sheet.Dimension.Columns;
            var workshops = new Dictionary<string, Workshop>();
            string workshopListing;

            for(int i=2; i<= rows; i++)
            {
                for(int j=5; j<= columns; j++)
                {
                    workshopListing = sheet.Cells[i,j].Value.ToString();
                    workshops[workshopListing] = new Workshop()
                    {
                        Name = workshopListing.GetWorkshopName(),
                        Leader = workshopListing.GetLeaderName(),
                        Type = sheet.Cells[1, j].Value.ToString()
                    };

                    if (workshops[workshopListing].Selections is null)
                    {
                        workshops[workshopListing].Selections = new List<WorkshopSelection>();
                    }

                    workshops[workshopListing].Selections.Add(new WorkshopSelection()
                    {
                        ClassName = workshopListing.GetWorkshopName(),
                        FullName = sheet.Cells[i, 4].Value.ToString(),
                        SelectionId = sheet.Cells[i, 2].Value.ToString(),
                        RegistrationId = int.Parse(sheet.Cells[i, 1].Value.ToString())
                    });
                }          
            }

            return workshops.Values.ToList();
        }

        private static int GetColumnIndex(ExcelWorksheet sheet, string columnName) 
        {
            return sheet.Cells["1:1"].First(cell => 
                cell.Value.ToString()
                    .Contains(columnName))
                    .Start.Column;
        }

        private static List<WorkshopSelection> ParseClassSelections(ExcelPackage package)
        {
            var classSelectionsSheet = package
                                            .Workbook
                                            .Worksheets
                                            .FirstOrDefault(ws =>
                                                    ws.Name == "ClassSelection");

            if (classSelectionsSheet == null || classSelectionsSheet == default)
            {
                throw new InvalidDataException("Could not find the Class Selection worksheet.");
            }

            var rows = classSelectionsSheet.Dimension.Rows;
            var selections = new List<WorkshopSelection>();

            for (int i = 2; i <= rows; i++)
            {
                selections.Add(
                    new WorkshopSelection()
                    {
                        RegistrationId = int.Parse(
                            classSelectionsSheet.Cells[i,
                            classSelectionsSheet.Cells["1:1"].First(cell =>
                                cell.Value.ToString().StartsWith("WinterAdventure")).Start.Column].Value.ToString()),
                        SelectionId = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "ClassSelection_Id").Start.Column].Value.ToString(),
                        FirstName = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "Name_First").Start.Column].Value.ToString(),
                        LastName = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "Name_Last").Start.Column].Value.ToString()
                    }
                );
            };

            return selections;
        }
    }
}
