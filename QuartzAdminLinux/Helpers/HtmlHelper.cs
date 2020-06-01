using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace QuartzAdminLinux.Helpers
{
    public static class HtmlHelpers
    {

        public static MvcHtmlString IsActive(this HtmlHelper htmlHelper, string action, string controller, string activeClass, string inActiveClass = "")
        {
            var routeData = htmlHelper.ViewContext.RouteData;

            var routeAction = routeData.Values["action"].ToString();
            var routeController = routeData.Values["controller"].ToString();

            var returnActive = (controller == routeController && action == routeAction);

            return new MvcHtmlString(returnActive ? activeClass : inActiveClass);
        }
        public static string IsActive(this HtmlHelper htmlHelper, string action, string controller)
        {
            var routeData = htmlHelper.ViewContext.RouteData;

            var routeAction = routeData.Values["action"].ToString();
            var routeController = routeData.Values["controller"].ToString();

            var returnActive = (controller == routeController && action == routeAction);

            return returnActive ? "active" : "";
        }
    }
}