using CorporateOnboardingAPIs.CRMWrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Unity;
using Unity.Lifetime;
using Unity.WebApi;

namespace Loan_Orginition
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);



            var container = new UnityContainer();

            // Register types
            container.RegisterType<CRMWrapper>(new HierarchicalLifetimeManager());


            UnityConfig.RegisterComponents(); // Register Unity components
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);



        }
    }
}
