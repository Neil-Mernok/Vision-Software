using Be.Timvw.Framework.ComponentModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Vision.Parameter
{
    public class report_test
    {
        public string Module_Functionality { get; set; }
        public string Result
        {
            get
            {
                if (!used)
                    return "Not used";
                else if (pass)
                    return "Pass";
                else
                    return "Fail";
            }
        }

        public Boolean used = false;
        public Boolean pass = false;
        public string Fault_Description { get; set; }

        public report_test(string Text, Boolean Pass, Boolean Used, string Fault)
        {
            Module_Functionality = Text;
            pass = Pass;
            used = Used;
            Fault_Description = Fault;
        }

        public BaseColor GetCol()
        {
            if (pass)
                return BaseColor.DARK_GRAY;
            else if (used == false)
                return BaseColor.LIGHT_GRAY;
            else
                return BaseColor.RED;
        }

    }

    public class Report_writer
    {

        public List<report_test> lines = new List<report_test>();
        public Boolean Pass = false;

        public string notes;               // store any related testing notes
        public string PCB;                 // number assigned by TUB for assembly 
        public string test_person;         // person doing tests
        public string PCB_Number;          // PCB number detected
        public string Firmware_Rev;        // Firmware Revision on date of test
        public string Test_Level;          // Firmware Revision on date of test

        public UInt32 UID;

        public Report_writer(UInt32 uid)
        {
            UID = uid;
        }

        public void Show_test_reportWPF(ref System.Windows.Controls.DataGrid DG)
        {
            DG.ItemsSource = null;

            DG.AutoGenerateColumns = true;
            DG.ItemsSource = lines;

            DG.UnselectAll();
        }


        public void SaveReport(string report_path)
        {
            //Creating iTextSharp Table from the DataTable data
            PdfPTable pdfTable = new PdfPTable(3);
            pdfTable.DefaultCell.Padding = 10;
            //pdfTable.WidthPercentage = 30;
            pdfTable.WidthPercentage = 100f;
            pdfTable.HorizontalAlignment = Element.ALIGN_LEFT;
            pdfTable.DefaultCell.BorderWidth = 1;

            Document Doc = new Document(PageSize.A4); //, 30f, 30f, 30f, 30f);

            iTextSharp.text.Font fontTitle = FontFactory.GetFont("Arial", 24.0f, 1, new BaseColor(0x4c68a2));
            iTextSharp.text.Font fontHeader = FontFactory.GetFont("Arial", 16.0f, 1, BaseColor.WHITE);
            iTextSharp.text.Font fontText = FontFactory.GetFont("Arial", 14.0f, 0, BaseColor.DARK_GRAY);
            iTextSharp.text.Font fontBoldText = FontFactory.GetFont("Arial", 14.0f, 1, BaseColor.DARK_GRAY);
            iTextSharp.text.Font fontTable = FontFactory.GetFont("Arial", 14.0f, 0, BaseColor.LIGHT_GRAY);

            //Exporting to PDF
            DateTime N = DateTime.Now;
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + report_path;
            string path = folderPath + "\\" + Test_Level + " 0x" + UID.ToString("X8") + " , " + N.ToString("yyyy-MM-d , HH;mm;ss") + ".pdf";

            Directory.CreateDirectory(folderPath);
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(Doc, stream);
                Doc.Open();

                //////////////////////////////////////////////////////////////////////////////////////////////////

                // Add the Title to the page
                Doc.AddAuthor(test_person);
                Paragraph Patc = new Paragraph("VISION " + Test_Level + " Acceptance Test Certificate", fontTitle);
                Patc.SpacingAfter = 10f;
                Paragraph Pserial = new Paragraph("Unit serial number: " + PCB, fontText);
                Pserial.SpacingAfter = 10f;
                Paragraph PTestLevel = new Paragraph("Unit tested: " + Test_Level, fontText);
                PTestLevel.SpacingAfter = 10f;
                Paragraph Ptitle = new Paragraph("UID: 0x" + UID.ToString("X8"), fontText);
                Ptitle.SpacingAfter = 10f;
                Paragraph PBoardInfo = new Paragraph("PCB number: " + PCB_Number, fontText);
                PBoardInfo.SpacingAfter = 10f;
                Paragraph PFirmwareRev = new Paragraph("Firmware: " + Firmware_Rev, fontText);
                PFirmwareRev.SpacingAfter = 10f;
                Paragraph Pdate = new Paragraph("Date of test: " + N.ToString("d-MMM-yyyy   HH:mm:ss") + Environment.NewLine, fontText);
                Pdate.SpacingAfter = 10f;
                Paragraph Ptester = new Paragraph("Test operator: " + test_person, fontText);
                Ptester.SpacingAfter = 10f;
                Paragraph Psignature = new Paragraph("Signature: ___________________", fontText);
                Psignature.SpacingAfter = 30f;

                Doc.Add(Patc);
                Doc.Add(Pserial);
                Doc.Add(PTestLevel);
                Doc.Add(Ptitle);
                Doc.Add(PBoardInfo);
                Doc.Add(PFirmwareRev);
                Doc.Add(Pdate);
                Doc.Add(Ptester);
                Doc.Add(Psignature);

                var info = typeof(report_test).GetProperties();
                foreach (PropertyInfo inf in info)
                {
                    var cell1 = new PdfPCell(new Phrase(inf.Name, fontHeader));
                    cell1.BackgroundColor = new BaseColor(0x4c68a2);
                    cell1.Border = 2;
                    pdfTable.AddCell(cell1);
                }


                //Adding DataRow
                foreach (report_test R in lines)
                {
                    foreach (PropertyInfo inf in info)
                    {
                        string s = inf.GetValue(R, null).ToString();
                        iTextSharp.text.Font ft = FontFactory.GetFont("Arial", 12.0f, 0, R.GetCol());
                        var cell1 = new PdfPCell(new Phrase(s, ft));
                        //cell1.BackgroundColor = R.GetCol();
                        pdfTable.AddCell(cell1);
                    }
                }

                Doc.Add(pdfTable);

                Paragraph PNotesTitle = new Paragraph("Additional notes: \n\n", fontBoldText);
                PNotesTitle.SpacingBefore = 30f;
                Doc.Add(PNotesTitle);

                Paragraph Pnotes = new Paragraph(notes, fontText);
                Doc.Add(Pnotes);

                //////////////////////////////////////////////////////////////////////////////////////////////////
                Doc.Close();
                stream.Close();
            }
        }
    }
}
