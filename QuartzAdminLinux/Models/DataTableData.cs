using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuartzAdminLinux.Models
{
    public class DataTableData<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public List<T> data { get; set; }

    }
}