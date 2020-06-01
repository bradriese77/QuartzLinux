using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpenXml.Excel.Data.Util;
using Tuple = System.Tuple;
using System.Diagnostics;

namespace OpenXml.Excel.Data
{
    public class ExcelDataReader : IDataReader
    {
        private SpreadsheetDocument _document;
        private OpenXmlReader _reader;
        private OpenXmlElement _currentRow;
        private string[] _headers;

        public string[] Headers
        {
            get { return _headers; }
            set { _headers = value; }
        }
        private readonly IDictionary<int, string> _sharedStrings;

        public static DataTable GetDataTable(string FilePath, string SheetName, bool FirstRowAsHeader)
        {
            DataTable dt = new DataTable();
            using (ExcelDataReader r = new ExcelDataReader(FilePath,SheetName,FirstRowAsHeader))
            {
                dt.Load(r);
            }
            return dt;
        }

        public static DataTable GetDataTable(string FilePath, int SheetIndex,string []Headers)
        {
            DataTable dt = new DataTable();
            using (ExcelDataReader r = new ExcelDataReader(FilePath, SheetIndex, false))
            {
                r.Headers = Headers;
                dt.Load(r);
            }

            return dt;
        }
        public ExcelDataReader(string path, int sheetIndex = 0, bool firstRowAsHeader = true)
        {
            _document = SpreadsheetDocument.Open(path, false);
            _sharedStrings = GetSharedStrings(_document);

            var worksheetPart = _document.WorkbookPart.GetPartById(GetSheetByIndex(sheetIndex).Id.Value);
            _reader = OpenXmlReader.Create(worksheetPart);
            SkipRows(GetEmptyRowsCount(worksheetPart));
            _headers = firstRowAsHeader ? GetFirstRowAsHeaders() : GetRangeHeaders(worksheetPart);
        }

        public ExcelDataReader(Stream stream, int sheetIndex = 0, bool firstRowAsHeader = true)
        {
            _document = SpreadsheetDocument.Open(stream, false);
            _sharedStrings = GetSharedStrings(_document);

            var worksheetPart = _document.WorkbookPart.GetPartById(GetSheetByIndex(sheetIndex).Id.Value);
            _reader = OpenXmlReader.Create(worksheetPart);
            SkipRows(GetEmptyRowsCount(worksheetPart));
            _headers = firstRowAsHeader ? GetFirstRowAsHeaders() : GetRangeHeaders(worksheetPart);
        }

        public ExcelDataReader(string path, string sheetName, bool firstRowAsHeader = true)
        {
            _document = SpreadsheetDocument.Open(path, false);
            _sharedStrings = GetSharedStrings(_document);

            var worksheetPart = _document.WorkbookPart.GetPartById(GetSheetByName(sheetName).Id.Value);
            _reader = OpenXmlReader.Create(worksheetPart);
            SkipRows(GetEmptyRowsCount(worksheetPart));
            _headers = firstRowAsHeader ? GetFirstRowAsHeaders() : GetRangeHeaders(worksheetPart);
        }

        public ExcelDataReader(Stream stream, string sheetName, bool firstRowAsHeader = true)
        {
            _document = SpreadsheetDocument.Open(stream, false);
            _sharedStrings = GetSharedStrings(_document);

            var worksheetPart = _document.WorkbookPart.GetPartById(GetSheetByName(sheetName).Id.Value);
            _reader = OpenXmlReader.Create(worksheetPart);
            SkipRows(GetEmptyRowsCount(worksheetPart));
            _headers = firstRowAsHeader ? GetFirstRowAsHeaders() : GetRangeHeaders(worksheetPart);
        }

        #region methods

        private void SkipRows(int count)
        {
            for (var i = 0; i < count; i++)
                SkipRow();
        }

        private void SkipRow()
        {
            while (_reader.Read())
                if (_reader.ElementType == typeof(Row) && _reader.IsEndElement)
                    break;
        }

        private static int GetEmptyRowsCount(OpenXmlPart worksheetPart)
        {
            var emptyRowsCount = 0;
            using (var reader = OpenXmlReader.Create(worksheetPart))
            {
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Row))
                    {
                        var row = reader.LoadCurrentElement();
                        if (!string.IsNullOrEmpty(row.InnerText))
                            break;

                        emptyRowsCount ++;
                    }
                }
            }
            return emptyRowsCount;
        }

        private IEnumerable<Sheet> GetSheets()
        {
            return _document.WorkbookPart.Workbook
                .GetFirstChild<Sheets>()
                .Elements<Sheet>();
        }

        private Sheet GetSheetByIndex(int sheetIndex)
        {
            var sheets = GetSheets().ToArray();
            if (sheetIndex < 0 || sheetIndex >= sheets.Count())
                throw new ApplicationException(Error.NotFoundSheetIndex(sheetIndex));

            return sheets.ElementAt(sheetIndex);
        }

        private Sheet GetSheetByName(string sheetName)
        {
            var sheet = GetSheets().FirstOrDefault(x => x.Name == sheetName);
            if (sheet == null)
                throw new ApplicationException(Error.NotFoundSheetName(sheetName));

            return sheet;
        }

        private string[] GetFirstRowAsHeaders()
        {
            var result = new string[] { };
            if (Read())
            {
                result = AdjustRow(_currentRow, -1)
                    .Select(GetCellValue)
                    .ToArray();
            }
            _currentRow = null;
            return result;
        }

        private static string[] GetRangeHeaders(OpenXmlPart worksheetPart)
        {
            var count = 0;
            using (var reader = OpenXmlReader.Create(worksheetPart))
            {
                while (reader.Read())
                {
                    if (reader.ElementType == typeof (Row))
                    {
                        count = reader.LoadCurrentElement().Elements<Cell>().Count();
                        break;
                    }
                }
            }
            return Enumerable.Range(0, count).Select(x => "col" + x).ToArray();
        }

        private static IDictionary<int, string> GetSharedStrings(SpreadsheetDocument document)
        {
            return document.WorkbookPart.SharedStringTablePart.SharedStringTable
                .Select((x, i) => Tuple.Create(i, x.InnerText))
                .ToDictionary(x => x.Item1, x => x.Item2);
        }
        private WorkbookPart GetWorkbookPartFromCell(CellType cell)
        {
            Worksheet workSheet = cell.Ancestors<Worksheet>().FirstOrDefault();
            SpreadsheetDocument doc = this._document.WorkbookPart.OpenXmlPackage as SpreadsheetDocument;
            return doc.WorkbookPart;
        }
        private string GetFormatedValue(CellType cell, CellFormat cellformat)
        {
            string value;


            if (cellformat.NumberFormatId != 0)
            {
                WorkbookPart p=GetWorkbookPartFromCell(cell);
                CellFormats formats = p.WorkbookStylesPart.Stylesheet.CellFormats;
             
                /*
                var numberFormatId = cellFormat.NumberFormatId.Value;
                var numberingFormat = numberingFormats.Cast<NumberingFormat>()
                    .SingleOrDefault(f => f.NumberFormatId.Value == numberFormatId);

                // Here's yer string! Example: $#,##0.00_);[Red]($#,##0.00)
                if (numberingFormat != null && numberingFormat.FormatCode.Value.Contains("mm/dd/yy"))
                {
                    string formatString = numberingFormat.FormatCode.Value;
                    isDate = true;
                }
                */

                string format = formats.Elements<NumberingFormat>()
                    .Where(i => i.NumberFormatId.Value == cellformat.NumberFormatId.Value)
                    .First().FormatCode;
                double number = double.Parse(cell.InnerText);
                value = number.ToString(format);
            }
            else
            {
                value = cell.InnerText;
            }
            return value;
        }

        private string GetCellValue(CellType cell)
        {
            string value;
            
           

            if (cell == null || cell.CellValue == null)
                return null;


            if (cell.StyleIndex != null)
            {
                    value = cell.CellValue.InnerXml;
        
                    CellFormat cellFormat = null;
                  
                    cellFormat = (CellFormat)this._document.WorkbookPart.WorkbookStylesPart.Stylesheet.CellFormats.ElementAt((int)cell.StyleIndex.Value);


                    var numberFormatId = cellFormat.NumberFormatId.Value;

                
                if(cell.CellReference.ToString().StartsWith("D") || cell.CellReference.ToString().StartsWith("O"))
                {
                    
                   
                }
                switch (numberFormatId)
                    {
                    case 20:
                    case 166:
                    case 19:
                        if (value.ToString().Contains("."))
                        {
                            value = DateTime.FromOADate(Convert.ToDouble(value)).ToString("h:mm:ss tt");
                        }
                        else
                        {
                            value = DateTime.FromOADate(Convert.ToDouble(value)).ToString("M/dd/yyyy");
                        }
                            break;
                    case 165:

                    case 14:
                    
                            value = DateTime.FromOADate(Convert.ToDouble(value)).ToString("M/dd/yyyy");


                            break;
                        default:
                            value = cell.CellValue.InnerXml;
                            if (value == null)
                                return null;

                            int index;
                            if (int.TryParse(value, out index) && cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                                return _sharedStrings[index];
                            break;
                    }
                   
                    /*
    Todo add the case statements for all the different formats listed below and/or find an easier way to do this                 

    0 = 'General';
    1 = '0';
    2 = '0.00';
    3 = '#,##0';
    4 = '#,##0.00';

    9 = '0%';
    10 = '0.00%';
    11 = '0.00E+00';
    12 = '# ?/?';
    13 = '# ??/??';
    14 = 'mm-dd-yy';
    15 = 'd-mmm-yy';
    16 = 'd-mmm';
    17 = 'mmm-yy';
    18 = 'h:mm AM/PM';
    19 = 'h:mm:ss AM/PM';
    20 = 'h:mm';
    21 = 'h:mm:ss';
    22 = 'm/d/yy h:mm';

    37 = '#,##0 ;(#,##0)';
    38 = '#,##0 ;[Red](#,##0)';
    39 = '#,##0.00;(#,##0.00)';
    40 = '#,##0.00;[Red](#,##0.00)';

    44 = '_("$"* #,##0.00_);_("$"* \(#,##0.00\);_("$"* "-"??_);_(@_)';
    45 = 'mm:ss';
    46 = '[h]:mm:ss';
    47 = 'mmss.0';
    48 = '##0.0E+0';
    49 = '@';

    27 = '[$-404]e/m/d';
    30 = 'm/d/yy';
    36 = '[$-404]e/m/d';
    50 = '[$-404]e/m/d';
    57 = '[$-404]e/m/d';

    59 = 't0';
    60 = 't0.00';
    61 = 't#,##0';
    62 = 't#,##0.00';
    67 = 't0%';
    68 = 't0.00%';
    69 = 't# ?/?';
    70 = 't# ??/??';
                    */
                
            }
            else
            {
                value = cell.CellValue.InnerXml;
                if (value == null)
                    return null;

                int index;
                if (int.TryParse(value, out index) && cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                {

                    value = _sharedStrings[index];
                }
            }
            return value;
        }

        private static IEnumerable<Cell> AdjustRow(OpenXmlElement row, int capacity)
        {
            if (row == null)
                return new Cell[] {};

            var cells = row.Elements<Cell>().ToArray();
            if (capacity == -1)
                capacity = cells.Count();
            int LastIndex = -1;
            List<Cell> list = new List<Cell>();
            for(int c=0;c<cells.Length;c++)
            {
                StringValue cellref=cells[c].CellReference;
                int i=ExcelUtil.GetColumnIndexByName(cellref.Value);
          //      Debug.WriteLine(cellref.Value + " " + i.ToString());
                if (LastIndex + 1 != i)
                    while (LastIndex + 1 < i)
                    {
                        list.Add(new Cell());
                        LastIndex++;
                    }

                list.Add(cells[c]);
                LastIndex=i;
            }
            while (list.Count() < capacity)
                list.Add(new Cell());
            /*
            var list = cells
                .OrderBy(x => ExcelUtil.GetColumnIndexByName(x.CellReference.Value))
                .Take(capacity)
                .ToList();

            while (list.Count() < capacity)
                list.Add(new Cell());
            */
            return list;
        }

        #endregion

        #region IDataReader Members

        public void Close()
        {
            Dispose();
        }

        public int Depth
        {
            get { return 0; }
        }

        public DataTable GetSchemaTable()
        {
            return null;
        }

        public bool IsClosed
        {
            get { return _document == null; }
        }

        public bool NextResult()
        {
            return false;
        }

        public bool Read()
        {
            while (_reader.Read())
            {
                if (_reader.ElementType == typeof (Row))
                {
                    _currentRow = _reader.LoadCurrentElement();
                    // skip empty rows
                    if (string.IsNullOrEmpty(_currentRow.InnerText))
                        continue;
                    break;
                }
            }
            return _currentRow != null && !_reader.EOF;
        }

        public int RecordsAffected
        {
            /*
             * RecordsAffected is only applicable to batch statements
             * that include inserts/updates/deletes. The sample always
             * returns -1.
             */
            get { return -1; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader.Dispose();
                _reader = null;
            }

            if (_document != null)
            {
                _document.Dispose();
                _document = null;
            }
        }

        #endregion

        #region IDataRecord Members

        public int FieldCount
        {
            get { return _headers.Length; }
        }

        public bool GetBoolean(int i)
        {
            return SafeConverter.Convert<bool>(GetValue(i));
        }

        public byte GetByte(int i)
        {
            return SafeConverter.Convert<byte>(GetValue(i));
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            return SafeConverter.Convert<char>(GetValue(i));
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            return null;
        }

        public string GetDataTypeName(int i)
        {
            return typeof (string).Name;
        }

        public DateTime GetDateTime(int i)
        {
            return DateTime.FromOADate(GetDouble(i));
        }

        public decimal GetDecimal(int i)
        {
            var value = GetValue(i);
            if (value != null)
            {
                decimal num;
                if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out num))
                    return num;
            }
            return SafeConverter.Convert<decimal>(value);
        }

        public double GetDouble(int i)
        {
            var value = GetValue(i);
            if (value != null)
            {
                double num;
                if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out num))
                    return num;
            }
            return SafeConverter.Convert<double>(value);
        }

        public float GetFloat(int i)
        {
            var value = GetValue(i);
            if (value != null)
            {
                float num;
                if (float.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out num))
                    return num;
            }
            return SafeConverter.Convert<float>(value);
        }

        public Type GetFieldType(int i)
        {
            return typeof(string);
        }

        public Guid GetGuid(int i)
        {
            return SafeConverter.Convert<Guid>(GetValue(i));
        }

        public short GetInt16(int i)
        {
            return SafeConverter.Convert<short>(GetValue(i));
        }

        public int GetInt32(int i)
        {
            return SafeConverter.Convert<int>(GetValue(i));
        }

        public long GetInt64(int i)
        {
            return SafeConverter.Convert<long>(GetValue(i));
        }

        public string GetName(int i)
        {
            return _headers[i];
        }

        public int GetOrdinal(string name)
        {
            for(var i = 0; i < _headers.Length; i++)
                if (string.Equals(_headers[i], name, StringComparison.InvariantCultureIgnoreCase))
                    return i;

            return -1;
        }

        public string GetString(int i)
        {
            return SafeConverter.Convert<string>(GetValue(i));
        }

        public object GetValue(int i)
        {
            var cell = AdjustRow(_currentRow, _headers.Length).ElementAtOrDefault(i);
            return GetCellValue(cell);
        }

        public int GetValues(object[] values)
        {
            var num = values.Length < _headers.Length ? values.Length : _headers.Length;
            var row = AdjustRow(_currentRow, num)
                .Select(GetCellValue)
                .ToArray();

            for (var i = 0; i < num; i++)
                values[i] = row[i];

            return num;
        }

        public bool IsDBNull(int i)
        {
            return Convert.IsDBNull(GetValue(i));
        }

        public object this[string name]
        {
            get { return this[GetOrdinal(name)]; }
        }

        public object this[int i]
        {
            get { return GetValue(i); }
        }

        #endregion
    }
}