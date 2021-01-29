﻿using System.Web;
using System.Web.Optimization;

namespace QuartzAdmin
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js", "~/Scripts/jquery-ui.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));
            bundles.UseCdn = true;
            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                 "~/Scripts/jquery.dataTables.min.js", "~/Scripts/dataTables.bootstrap4.min.js"));


            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css","~/Content/jquery-ui*"));
 
            bundles.Add(new StyleBundle("~/datatablescss").Include(
           "~/Content/jquery.dataTables.min.css", "~/Content/dataTables.bootstrap4.min.css"
           ));
        }
    }
}