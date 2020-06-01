using Microsoft.VisualBasic.FileIO;
using OpenXml.Excel.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppUtilityLib
{
    public class DataUtility:Emailer
    {

        public void ConnectionContext_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            Logger.Info(e.ToString());
        }
        public static string[] GetDistinctstrings(DataTable table, string RowFilter, string ColumnName)
        {

            DataRow[] rows = table.Select(RowFilter);
            var Distinctobjects = (
                 from row in rows.AsEnumerable()
                 select row.Field<object>(ColumnName)).Distinct();
            List<string> list = new List<string>();
            foreach (object obj in Distinctobjects)
            {
                if(obj!=null)
                list.Add(obj.ToString());
            }
            return list.ToArray();
        }
        public DataSet GetErrorNumberDataSet(SqlCommand cmd)
        {
            if (cmd.Connection != null && cmd.Connection.State != ConnectionState.Open) throw new Exception("SqlCommand needs an open Connection");
            cmd.Connection.InfoMessage += ConnectionContext_InfoMessage;
            DataSet ds = new DataSet();
            using (SqlDataAdapter dAdapt = new SqlDataAdapter(cmd))
            {
                Logger.Info(string.Format("Executing {0}", cmd.CommandText));
                dAdapt.SelectCommand = cmd;
                dAdapt.Fill(ds);

                foreach (DataTable dt in ds.Tables)
                {

                    if (dt.Columns.Contains("ErrorNumber"))
                    {
                        if ((int)dt.Rows[0]["ErrorNumber"] != 0)
                        {
                            string ErrorMessage = dt.Rows[0]["ErrorMessage"].ToString();
                            if (ErrorMessage.Contains("are missing"))
                            {
                                ErrorMessage += "\r\n" + string.Join("\r\n", GetDistinctstrings(ds.Tables[0], "", ds.Tables[0].Columns[0].ColumnName));
                            }
                            throw new Exception(ErrorMessage);
                        }
                    }
                }
            }
            return ds;
        }
        public void ExportDataTable(DataTable dt,string Path,string Delimitor= ",")
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(Delimitor,fields));

            }

            File.WriteAllText(Path, sb.ToString());
        }
        public DataSet GetErrorNumberDataSet(SqlCommand cmd, string ConnectionStr)
        {

            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionStr))
                {
                    cmd.Connection = connection;

                    connection.InfoMessage += ConnectionContext_InfoMessage;

                    connection.Open();

                    ds=GetErrorNumberDataSet(cmd);

                    connection.Close();
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ds;
        }
        public string GetTableStr(DataTable Table)
        {
            string str = string.Empty;

            foreach (DataColumn col in Table.Columns)
            {
                string coltype = "varchar(255)";
                switch (col.DataType.ToString())
                {
                    case "System.DateTime":
                        coltype = "datetime";
                        break;
                    case "System.Int32":
                        coltype = "int";
                        break;
                    default:
                        coltype = "varchar(255)";
                        break;
                }
                if (str == string.Empty)
                {
                    str = string.Format("create table {0}\r\n", Table.TableName);
                    str += "(\r\n";
                }
                else str += ",\r\n";
                str += "[" + col.ColumnName + "] " + coltype;
            }

            str += ")\r\n";

            return str + "\r\n";

        }
        public DataTable ParseCSVFile(string FilePath, bool IgnoreMissingColumns = false, bool HasFieldsEnclosedInQuotes=true)
        {
            FileInfo info = new FileInfo(FilePath);

            DataTable CSVTable = new DataTable();
            int LineCounter = 0;
            try
            {
                using (TextFieldParser parser = new TextFieldParser(FilePath, Encoding.UTF8, true) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = HasFieldsEnclosedInQuotes, Delimiters = new string[] { "," } })
                {
                    while (!parser.EndOfData)
                    {
       
                        LineCounter++;
                        
                        if (HasFieldsEnclosedInQuotes && LineCounter == 1)
                        {
                            string[] fields = parser.ReadFields();
                            foreach (string field in fields)
                            {
                                CSVTable.Columns.Add(field);
                            }
                        }
                        else 
                        {
                            DataRow row = CSVTable.NewRow();

                            string[] fields = parser.ReadFields();

                            if (!HasFieldsEnclosedInQuotes && LineCounter == 1)
                            {

                                for (int i = 0; i < fields.Length; i++)
                                {
                                    CSVTable.Columns.Add("Column" + (i + 1).ToString());
                                }

                            }
                            for (int i = 0; i < fields.Length; i++)
                            {
                                try
                                {
                                    row[i] = fields[i];
                                }
                                catch(Exception ex)
                                {
                                    Logger.Warn(string.Format("Line {0} failed to parse column {1}",LineCounter,i),ex);
                                    if(!IgnoreMissingColumns)throw ex;
                                }
                            }
                            CSVTable.Rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Failed to process {1} on line {0}", LineCounter, FilePath), ex);
                throw ex;
            }
            return CSVTable;
        }
        public int WriteToSql(SqlConnection conn, DataTable table,List<SqlBulkCopyColumnMapping> ColumnMappings=null,string []IgnoreColumns=null)
        {
            using (var bc = new SqlBulkCopy(conn))
            {
                bc.DestinationTableName = table.TableName;
                if (ColumnMappings == null)
                    foreach (DataColumn c in table.Columns.Cast<DataColumn>().Where(c=>IgnoreColumns==null || !IgnoreColumns.Contains(c.ColumnName)))
                    {
                        bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));
                    }
                else ColumnMappings.Where(m=>IgnoreColumns==null || !IgnoreColumns.Contains(m.SourceColumn)).ToList().ForEach(m=> bc.ColumnMappings.Add(m));
                bc.WriteToServer(table);
                return table.Rows.Count;
            }
        }
        public DataSet ProcessFileForStaging(string FilePath, string ConnectionStr, string StagingTableName, string StoredProcedure, int SqlCommandTimeOut = 30, List<SqlBulkCopyColumnMapping> ColumnMappings = null, string[] IgnoreColumns = null)
        {
            DataTable dt = new DataTable();
          
            if (Path.GetExtension(FilePath).ToLower() == ".csv")
            {
                dt = ParseCSVFile(FilePath);
            }
            else
            {
                using (ExcelDataReader r = new ExcelDataReader(FilePath))
                {
                    dt.Load(r);
                }
            }

            return ProcessStagingTableFile(dt,  ConnectionStr, StagingTableName, StoredProcedure, SqlCommandTimeOut, ColumnMappings, IgnoreColumns);
        }
        public DataSet ProcessStagingTableFile(DataTable dt, string ConnectionStr, string StagingTableName, string StoredProcedure, int SqlCommandTimeOut = 30, List<SqlBulkCopyColumnMapping> ColumnMappings = null, string[] IgnoreColumns = null)
        {

            DataSet ds = new DataSet();


            using (SqlConnection conn = new SqlConnection(ConnectionStr))
            {
                dt.TableName = StagingTableName;

                conn.Open();

                using (SqlCommand cmd = new SqlCommand() { Connection = conn, CommandText = "truncate table " + StagingTableName, CommandTimeout = SqlCommandTimeOut })
                {
                    cmd.ExecuteNonQuery();
                }

                WriteToSql(conn, dt, ColumnMappings, IgnoreColumns);

                using (SqlCommand cmd = new SqlCommand() { Connection = conn, CommandType = CommandType.StoredProcedure, CommandText = StoredProcedure, CommandTimeout = SqlCommandTimeOut })
                {
                    try
                    {
                        ds = GetErrorNumberDataSet(cmd);
                        if(ds.Tables[ds.Tables.Count-1]?.Rows.Count>0)
                        {
                            Logger.Info($"Loaded {ds.Tables[ds.Tables.Count - 1].Rows[0][0].ToString()} Row(s)");
                        }
                    }
                    catch (Exception ex)
                    {
                        string Sql = cmd.CommandAsSql();
                        Logger.Error(string.Format("Failed to execute the sql {0}", Sql, ex));
                        throw ex;
                    }
                }

                conn.Close();
            }
            return ds;
        }
    }
}
