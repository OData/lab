// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Service.ApiAsAService.Models;
using Microsoft.OData.Service.ApiAsAService.Submit;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.AspNet.Model;
using Microsoft.OData.Edm.Csdl;
using System.Xml;
using Microsoft.OData.Edm.Validation;

namespace Microsoft.OData.Service.ApiAsAService.Api
{
    public class DynamicApi<T> : EntityFrameworkApi<T> where T : System.Data.Entity.DbContext
    {
        public T ModelContext { get { return DbContext; } }

        //[Resource]
        //public Person Me
        //{
        //    get
        //    {
        //        return DbContext.People
        //            .Include("Friends")
        //            .Include("Trips")
        //            .Single(p => p.PersonId == 1);
        //    }
        //}

        //private IQueryable<Person> PeopleWithFriends
        //{
        //    get { return ModelContext.People.Include("Friends"); }
        //}

        ///// <summary>
        ///// Implements an action import.
        ///// </summary>
        //[Operation(Namespace = "Microsoft.OData.Service.ApiAsAService.Models", HasSideEffects = true)]
        //public void ResetDataSource()
        //{
        //    TrippinModel.ResetDataSource();
        //}

        ///// <summary>
        ///// Action import - clean up all the expired trips.
        ///// </summary>
        //[Operation(Namespace = "Microsoft.OData.Service.ApiAsAService.Models", HasSideEffects = true)]
        //public void CleanUpExpiredTrips()
        //{
        //    // DO NOT ACTUALLY REMOVE THE TRIPS.
        //}

        ///// <summary>
        ///// Bound action - set the end-up time of a trip.
        ///// </summary>
        ///// <param name="trip">The trip to update.</param>
        ///// <returns>The trip updated.</returns>
        //[Operation(Namespace = "Microsoft.OData.Service.ApiAsAService.Models", IsBound = true, HasSideEffects = true)]
        //public Trip EndTrip(Trip trip)
        //{
        //    // DO NOT ACTUALLY UPDATE THE TRIP.
        //    return trip;
        //}

        ///// <summary>
        ///// Bound function - gets the number of friends of a person.
        ///// </summary>
        ///// <param name="person">The key of the binding person.</param>
        ///// <returns>The number of friends of the person.</returns>
        //[Operation(Namespace = "Microsoft.OData.Service.ApiAsAService.Models", IsBound = true)]
        //public int GetNumberOfFriends(Person person)
        //{
        //    if (person == null)
        //    {
        //        return 0;
        //    }

        //    var personWithFriends = PeopleWithFriends.Single(p => p.PersonId == person.PersonId);
        //    return personWithFriends.Friends == null ? 0 : personWithFriends.Friends.Count;
        //}

        ///// <summary>
        ///// Function import - gets the person with most friends.
        ///// </summary>
        ///// <returns>The person with most friends.</returns>
        //[Operation(Namespace = "Microsoft.OData.Service.ApiAsAService.Models", EntitySet = "People")]
        //public Person GetPersonWithMostFriends()
        //{
        //    Person result = null;

        //    foreach (var person in PeopleWithFriends)
        //    {
        //        if (person.Friends == null)
        //        {
        //            continue;
        //        }

        //        if (result == null)
        //        {
        //            result = person;
        //        }

        //        if (person.Friends.Count > result.Friends.Count)
        //        {
        //            result = person;
        //        }
        //    }

        //    return result;
        //}

        ///// <summary>
        ///// Function import - gets people with at least n friends.
        ///// </summary>
        ///// <param name="n">The minimum number of friends.</param>
        ///// <returns>People with at least n friends.</returns>
        //[Operation(Namespace = "Microsoft.OData.Service.ApiAsAService.Models", EntitySet = "People")]
        //public IEnumerable<Person> GetPeopleWithFriendsAtLeast(int n)
        //{
        //    foreach (var person in PeopleWithFriends)
        //    {
        //        if (person.Friends == null)
        //        {
        //            continue;
        //        }

        //        if (person.Friends.Count >= n)
        //        {
        //            yield return person;
        //        }
        //    }
        //}

        //protected bool CanDeleteTrips()
        //{
        //    return false;
        //}

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            // Add customized OData validation settings 
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = (sp) => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth =3,
                MaxExpansionDepth = 3
            };

            IServiceCollection serviceCollection = EntityFrameworkApi<T>.ConfigureApi(apiType, services)
                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>()
                .AddSingleton<ODataValidationSettings>(validationSettingFactory)
                .AddService<IChangeSetItemFilter, CustomizedSubmitProcessor>()
                .AddService<IModelBuilder, DynamicModelBuilder>();

            return serviceCollection;
        }


        private class DynamicModelBuilder : IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                //return await InnerHandler.GetModelAsync(context, cancellationToken);
                IEdmModel model;
                IEnumerable<EdmError> errors;
                var appData = System.Web.HttpContext.Current.Server.MapPath("~/App_Data");
                var file = System.IO.Path.Combine(appData, "Trippin.xml");

                XmlReader xmlReader = XmlReader.Create(file);
                if (CsdlReader.TryParse(xmlReader, out model, out errors))
                {
                    return model;
                }

                throw new Exception("Couldn't parse xml");
            }
        }

        public DynamicApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
 }