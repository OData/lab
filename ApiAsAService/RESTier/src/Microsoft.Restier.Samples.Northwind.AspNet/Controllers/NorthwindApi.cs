﻿using System;
using System.Linq;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Samples.Northwind.AspNet.Data;

namespace Microsoft.Restier.Samples.Northwind.AspNet.Controllers
{

    /// <summary>
    /// 
    /// </summary>
    public partial class NorthwindApi : EntityFrameworkApi<NorthwindEntities>
    {

        public NorthwindApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entitySet"></param>
        /// <returns></returns>
        protected internal IQueryable<Category> OnFilterCategories(IQueryable<Category> entitySet)
        {
            //TraceEvent("CompanyEmployee", RestierOperationTypes.Filtered);
            return entitySet;//.Take(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        protected internal void OnInsertingCategory(Category entity)
        {
            //CompanyEmployeeManager.OnInserting(entity);
            //TrackEvent(entity, RestierOperationTypes.Inserting);
            Console.WriteLine("Inserting Category...");
        }


    }
}