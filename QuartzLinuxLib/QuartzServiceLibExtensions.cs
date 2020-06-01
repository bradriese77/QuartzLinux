using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{ 
        public static class QuartzServiceLibExtensions
        {

        public static object GetMappedValue(this JobDataMap map, string Key, object Default)
        {
            return map.ContainsKey(Key) ? map[Key] : Default; 
        }
      
    }

}
