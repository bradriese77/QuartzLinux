using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace QuartzAdmin.Models
{
    public class PagingData
    {
        public PagingData()
        {
            SortAscending = true;
        }

        public PagingData(NameValueCollection queryString)
        {
            ParseQueryString(queryString);
        }

        public string SortCol { get; set; }
        public bool SortAscending { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }

        public int Draw { get; set; }
        public string Search { get; set; }

        private void ParseQueryString(NameValueCollection queryString)
        {
            Draw = int.Parse(queryString["draw"] ?? "0");
            Skip = int.Parse(queryString["start"] ?? "0");
            Take = int.Parse(queryString["length"] ?? "0");
            Search = queryString["search[value]" ?? ""];
            SortAscending = true;

            // note: we only sort one column at a time
            if (queryString["order[0][column]"] != null)
            {
                var sortColumn = int.Parse(queryString["order[0][column]"]);

                SortCol = queryString["columns[" + sortColumn + "][data]"];
            }

            if (queryString["order[0][dir]"] != null)
            {
                SortAscending = queryString["order[0][dir]"] == "asc";
            }
        }
    }

}