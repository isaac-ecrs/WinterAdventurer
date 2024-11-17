using System.Collections;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using WinterAdventurer.Library.Extensions;
using WinterAdventurer.Library.Models;
using MigraDoc;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using System.Data.Common;
using System.Xml;
using PdfSharp.Fonts;

namespace WinterAdventurer.Library
{
    public class ExcelUtilities
    {
        public List<Workshop> Workshops = new List<Workshop>();
        
        readonly Color COLOR_BLACK = Color.FromRgb(0, 0, 0);

        public void ImportExcel(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            stream.Position = 0;
            
            using (var package = new ExcelPackage(stream))
            {
                Workshops.AddRange(ParseWorkshops(package));                                
            }
        }

        public List<Workshop> ParseWorkshops(ExcelPackage package)
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

        public List<Workshop> CollectWorkshops(ExcelWorksheet sheet)
        {
            var rows = sheet.Dimension.Rows;
            var columns = sheet.Dimension.Columns;
            var workshops = new Dictionary<string, Workshop>();
            Workshop workshop;

            // First get all the workshops, find the correct column by name and then get distinct values
            // Put the column header in the type value for each workshop
            for(int i = 1; i <= columns; i++)
            {
                // There's a value in the column header and it's a class
                if(sheet.Cells[1,i].Value != null && sheet.Cells[1,i].Value.ToString().ToLower().Contains("classes"))
                {
                    for(int j = 2; j <= rows; j++)
                    {
                        var rowValue = sheet.Cells[j,i].Value;

                        if(rowValue != null)
                        {
                            var workshopName = rowValue.ToString().GetWorkshopName();
                            var leaderName = rowValue.ToString().GetLeaderName();
                            var participantName = new PersonName(
                                sheet.Cells[j,
                                Constants.COLUMN_ATTENDEE_NAME].Value.ToString());

                            if(!string.IsNullOrWhiteSpace(workshopName))
                            {
                                var workshopSelected = new WorkshopSelection()
                                {
                                    ClassName = workshopName,
                                    RegistrationId = int.Parse(sheet.Cells[j, Constants.COLUMN_REGISTRATION_ID].Value.ToString()),
                                    SelectionId = int.Parse(sheet.Cells[j, Constants.COLUMN_SELECTION_ID].Value.ToString()),
                                    FullName = participantName.FullName,
                                    FirstName = participantName.FirstName,
                                    LastName = participantName.LastName                                
                                };

                                if(workshops.TryGetValue(rowValue.ToString(), out workshop))
                                {
                                    workshops[rowValue.ToString()].Selections.Add(workshopSelected);
                                }
                                else
                                {
                                    workshops.Add(rowValue.ToString(), new Workshop() 
                                        {
                                            Name = rowValue.ToString().GetWorkshopName(),
                                            Leader = rowValue.ToString().GetLeaderName(),
                                            Selections = new List<WorkshopSelection>()
                                            {
                                                workshopSelected
                                            }
                                        }
                                    );
                                }
                            }                       
                        }
                    }
                }
            }            

            return workshops.Values.ToList();
        }

        public Document CreatePdf()
        {
            if (Workshops != null)
            {
                GlobalFontSettings.FontResolver = new CustomFontResolver();

                var document = new Document();
                var style = document.Styles["Normal"];
                style.Font = new Font("noto");

                foreach(var section in PrintWorkshopParticipants())
                {                    
                    section.PageSetup.TopMargin = Unit.FromInch(.25);
                    section.PageSetup.LeftMargin = Unit.FromInch(.25);
                    
                    document.Sections.Add(section);                    
                }
                /*
                foreach(var section in PrintSchedules())
                {
                    document.Sections.Add(section);
                }
                */
                
                return document;
            }

            return null;
        }

        private List<Section> PrintWorkshopParticipants()
        {
            var sections = new List<Section>();
            var fontResolver = new CustomFontResolver();

            foreach(var workshopListing in Workshops)
            {
                var section = new Section();                

                //Add heading
                var header = section.AddParagraph();
                header.Format.Font.Color = COLOR_BLACK;                
                header.AddFormattedText(workshopListing.Name + " (" + workshopListing.Leader + ")", TextFormat.Bold);

                foreach(var attendee in workshopListing.Selections)
                {
                    var formattedAttendee = section.AddParagraph();
                    formattedAttendee.Format.Font.Color = COLOR_BLACK;
                    formattedAttendee.AddFormattedText(attendee.FullName);
                }
                
                sections.Add(section);
            }

            return sections;
        }

        // @TODO switch from hardcoded workshops to those from the uploaded file
        private Table BuildScheduleTemplate(Section section)
        {
            section.PageSetup.Orientation = Orientation.Landscape;

            var scheduleTable = section.AddTable();

            scheduleTable.Borders.Width = .75;

            string[] headers = { "Period", "Days", "Dining Room", "Martin Room", "Chapel A", "Library", "Rec Hall", "Craft Room", "Elm Room" };

            foreach (string header in headers)
            {
                Column column = scheduleTable.AddColumn(Unit.FromCentimeter(3)); // Adjust the column width as needed
                column.Format.Alignment = ParagraphAlignment.Center;
            }

            Row headerRow = scheduleTable.AddRow();
            
            for(int i = 0; i < headers.Length; i++)
            {
                AddTextToCell(headerRow.Cells[i], headers[i]);
                headerRow.Cells[i].Format.Font.Bold = true;
                headerRow.Cells[i].Shading.Color = Colors.LightGray;
            }            

            var row1 = scheduleTable.AddRow();
            var row2 = scheduleTable.AddRow();
            var row3 = scheduleTable.AddRow();
            var row4 = scheduleTable.AddRow();
            var row5 = scheduleTable.AddRow();
            var row6 = scheduleTable.AddRow();

            // Row 1
            AddTextToCell(row1.Cells[0], "1st Period");
            row1.Cells[0].MergeDown = 1;

            AddTextToCell(row1.Cells[1], "Days 1-2");

            AddTextToCell(row1.Cells[2], "International Folk Dancing (Patricia)");
            row1.Cells[2].MergeDown = 1;

            AddTextToCell(row1.Cells[3], "Wood Carving (Steve)");
            row1.Cells[3].MergeDown = 1;

            AddTextToCell(row1.Cells[4], "Sing and Play Instruments (Bonnie)");
            AddTextToCell(row1.Cells[5], "");
            AddTextToCell(row1.Cells[6], "Storytelling (Frank)");
            AddTextToCell(row1.Cells[7], "");

            AddTextToCell(row1.Cells[8], "Children's Program");
            row1.Cells[8].MergeDown = 1;

            // Row 2
            AddTextToCell(row2.Cells[1], "Days 3-4");

            AddTextToCell(row2.Cells[4], "A Capella Songs from the Sea and the City (Mark)");

            AddTextToCell(row2.Cells[6], "Informal Dramatics (Lane)");

            // Row 3
            AddTextToCell(row3.Cells[0], "2nd Period");
            row3.Cells[0].MergeDown = 1;

            AddTextToCell(row3.Cells[1], "Days 1-2");

            AddTextToCell(row3.Cells[2], "Late Night Dance");
            AddTextToCell(row3.Cells[4], "Breakthrough Decluttering (Bari)");
            AddTextToCell(row3.Cells[5], "The Write Stuff (Beverly)");
            AddTextToCell(row3.Cells[6], "Improv Theater (Howard)");

            AddTextToCell(row3.Cells[8], "Cooperative Children's Program");
            row3.Cells[8].MergeDown = 1;

            // Row 4
            AddTextToCell(row4.Cells[1], "Days 3-4");

            AddTextToCell(row4.Cells[2], "Scottish Dance (Patricia)");
            AddTextToCell(row4.Cells[4], "A Journey to Self-Kindness (Heather)");
            AddTextToCell(row4.Cells[5], "The Write Stuff (Beverly)");
            AddTextToCell(row4.Cells[6], "Party Games for All Ages (Trevor)");

            // Row 5
            AddTextToCell(row5.Cells[0], "3rd Period");
            row5.Cells[0].MergeDown = 1;

            AddTextToCell(row5.Cells[1], "Days 1-2");

            AddTextToCell(row5.Cells[2], "Dymanic Group Leadership (Lane)");

            AddTextToCell(row5.Cells[4], "Small Scenes (Glenn & Isaac)");
            row5.Cells[4].MergeDown = 1;

            AddTextToCell(row5.Cells[6], "Active Games for Teens and Adults (Trevor)");

            AddTextToCell(row5.Cells[7], "Posters with Wings (Teri)");
            row5.Cells[7].MergeDown = 1;

            AddTextToCell(row5.Cells[8], "Children's Program");
            row5.Cells[8].MergeDown = 1;

            // Row 6
            AddTextToCell(row6.Cells[1], "Days 3-4");

            AddTextToCell(row6.Cells[2], "Collaborative Board Games (Max)");
            AddTextToCell(row6.Cells[3], "The Children's March (Carolyn)");
            AddTextToCell(row6.Cells[6], "The Joy of Movement (Judi)");

            return scheduleTable;
        }

        private void AddTextToCell(Cell cell, string text)
        {
            var paragraph = cell.AddParagraph();
            paragraph.AddText(text);
        }

       private List<Section> PrintSchedules()
        {
            var sections = new List<Section>();

            foreach(var person in CollectSelectionsByAttendee())
            {
                var section = new Section();

                BuildScheduleTemplate(section);

                //var formattedSelection = section.AddParagraph();
                //formattedSelection.Format.Font.Color = COLOR_BLACK;
                //formattedSelection.AddFormattedText(person.Key + ':' + PrintClassSelections(person.Value));

                sections.Add(section);
            }

            return sections;
        }

        private string PrintClassSelections(List<WorkshopSelection> selections)
        {
            var result = "\n";

            foreach(var selection in selections)
            {
                result += selection.ClassName + '\n';
            }

            return result;
        }

        private int GetColumnIndex(ExcelWorksheet sheet, string columnName) 
        {
            return sheet.Cells["1:1"].First(cell => 
                cell.Value.ToString()
                    .Contains(columnName))
                    .Start.Column;
        }

        private Dictionary<string, List<WorkshopSelection>> CollectSelectionsByAttendee()
        {
            var people = new Dictionary<string, List<WorkshopSelection>>();

            foreach (var workshop in Workshops)
            {
                foreach(var selection in workshop.Selections)
                {
                    people.TryAdd(selection.FullName, new List<WorkshopSelection>());
                }
            }

            foreach (var person in people)
            {
                foreach (var workshop in Workshops)
                {
                    person.Value.AddRange(workshop.Selections.Where(s => s.FullName == person.Key));
                }
            }

            return people;
        }

        private List<WorkshopSelection> ParseClassSelections(ExcelPackage package)
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
                        SelectionId = (int)classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "ClassSelection_Id").Start.Column].Value,
                        FirstName = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "Name_First").Start.Column].Value.ToString(),
                        LastName = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "Name_Last").Start.Column].Value.ToString()
                    }
                );
            };

            return selections;
        }
    }
}
