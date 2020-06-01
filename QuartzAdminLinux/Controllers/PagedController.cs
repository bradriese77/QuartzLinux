using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Linq.Expressions;
using System.Collections.Specialized;
using QuartzAdminLinux.Models;

namespace QuartzAdminLinux.Controllers
{
    
    public class PagedController<T> : Controller
    {

        public PagedController()
        {

        }


        public async Task<List<T>> GetList(IQueryable<T> query, PagingData pgData, Expression<Func<T, bool>> where = null)
        {
            return await Task.Run(() => GetListItems(query, pgData, where).ToList());
           
        }
      

        private IQueryable<T> GetListItems(IQueryable<T> query, PagingData pgData, Expression<Func<T, bool>> where = null)
        {


            if (where != null)
            {
                query = query.Where(where);
            }

            if (pgData.Skip >= 0 && pgData.Take > 0)
            {
                query = query.Skip(pgData.Skip).Take(pgData.Take);
            }
            if (pgData.SortCol!=null && pgData.SortCol != string.Empty)
            {
                query = query.OrderByWithDirection(pgData.SortCol, pgData.SortAscending);
            }
            return query;
        }
    }

    public static class LinqExtensions
    {
        public static IOrderedQueryable<T> OrderByWithDirection<T>(this IQueryable<T> query, string memberName,
                                                                   bool ascending = true)
        {
            // Get the expression parameter type
            var typeParams = new ParameterExpression[] { Expression.Parameter(typeof(T), "") };

            // Determine the property field
            var pi = typeof(T).GetProperty(memberName);

            // Build and return the linq query
            return ascending
                ? (IOrderedQueryable<T>)query.Provider.CreateQuery(
                                                                    Expression.Call(
                                                                                    typeof(Queryable),
                                                                                    "OrderBy",
                                                                                    new Type[]
                                                                                    {typeof (T), pi.PropertyType},
                                                                                    query.Expression,
                                                                                    Expression.Lambda(
                                                                                                      Expression
                                                                                                          .Property(
                                                                                                                    typeParams
                                                                                                                        [
                                                                                                                         0
                                                                                                                        ],
                                                                                                                    pi),
                                                                                                      typeParams)))
                : (IOrderedQueryable<T>)query.Provider.CreateQuery(
                                                                    Expression.Call(
                                                                                    typeof(Queryable),
                                                                                    "OrderByDescending",
                                                                                    new Type[]
                                                                                    {typeof (T), pi.PropertyType},
                                                                                    query.Expression,
                                                                                    Expression.Lambda(
                                                                                                      Expression
                                                                                                          .Property(
                                                                                                                    typeParams
                                                                                                                        [
                                                                                                                         0
                                                                                                                        ],
                                                                                                                    pi),
                                                                                                      typeParams)));
        }
    }
}