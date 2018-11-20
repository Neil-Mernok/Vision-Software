using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Excel = Microsoft.Office.Interop.Excel;

namespace TestOperators
{
    public class TestOperatorsInfo
    {
        public List<string> Operators_NameSurname = new List<string>();
        public List<string> Operators_Passwords = new List<string>();

        public TestOperatorsInfo()
        {

            Excel.Application XL_TestOperators = new Excel.Application();
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range xlRange;
            xlWorkBook = XL_TestOperators.Workbooks.Open("D:/Users/FrancoisHattingh/Desktop/TITAN_TestOperators/TITAN_TestOperators.xlsx");

            try
            {
                xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                xlRange = xlWorkSheet.UsedRange;

                for (int rCnt = 2; rCnt <= xlRange.Rows.Count; rCnt++)
                {
                    Operators_NameSurname.Add(((xlRange.Cells[rCnt, 2] as Excel.Range).Value).ToString() + " " + ((xlRange.Cells[rCnt, 3] as Excel.Range).Value).ToString());
                    Operators_Passwords.Add(((xlRange.Cells[rCnt, 4] as Excel.Range).Value).ToString());
                }
            }
            catch (Exception ex)
            {
                //some error msg
                System.Windows.MessageBox.Show("Error" + ex.ToString());
            }
            finally
            {
                xlWorkBook.Close();
                XL_TestOperators.Quit();
            }
        }


        public List<string> Get_Operators_NameSurname
        {
            get
            {
                return Operators_NameSurname;
            }
        }

        public List<string> Get_TestOperators_Passwords
        {
            get
            {
                return Operators_Passwords;
            }
        }

    }
}
