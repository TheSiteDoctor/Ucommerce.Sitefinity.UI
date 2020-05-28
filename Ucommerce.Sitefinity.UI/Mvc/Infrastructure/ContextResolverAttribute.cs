using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Web;

namespace UCommerce.Sitefinity.UI.Mvc
{
    /// <summary>
    /// Action attribute class that handles the initilization of the Catalog Context.
    /// </summary>
    public class ContextResolverAttribute : ActionFilterAttribute, IActionFilter
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (SystemManager.CurrentHttpContext.Items[contextInitializedKey] == null || (bool)SystemManager.CurrentHttpContext.Items[contextInitializedKey] == false)
            {
                List<string> urlSegments = null;

                if (SystemManager.CurrentHttpContext.Request.Url != null)
                {
                    urlSegments = SystemManager.CurrentHttpContext.Request.Url.Segments.Select(i => i.Replace("/", string.Empty)).ToList();
                }
                else
                {
                    var currentNode = SiteMapBase.GetActualCurrentNode();

                    if (currentNode != null)
                    {
                        urlSegments = currentNode.Url.Split('/').ToList();
                    }
                }

                if (urlSegments != null)
                {

                }

                if (SystemManager.CurrentHttpContext.Items[contextInitializedKey] == null)
                {
                    SystemManager.CurrentHttpContext.Items.Add(contextInitializedKey, true); 
                }
                else
                {
                    SystemManager.CurrentHttpContext.Items[contextInitializedKey] = true;
                }
            }
        }
        
        private const string contextInitializedKey = "UCommerceContextInitialized";
    }
}
