using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using UCommerce.Sitefinity.UI.Api.Model;
using UCommerce.Sitefinity.UI.Constants;
using UCommerce.Sitefinity.UI.Mvc.Model;
using Ucommerce;
using Ucommerce.Api;
using Ucommerce.EntitiesV2;
using Ucommerce.Infrastructure;
using Ucommerce.Search;
using Ucommerce.Search.Slugs;
using Product = Ucommerce.Search.Models.Product;

namespace UCommerce.Sitefinity.UI.Api
{
    /// <summary>
    /// API Controller exposing endpoints related to search.
    /// </summary>
    public class SearchApiController : ApiController
    {

        public SearchApiController()
        {
        }

        [Route(RouteConstants.SEARCH_ROUTE_VALUE)]
        [HttpPost]
        public IHttpActionResult FullText(FullTextDTO model)
        {
            var search = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IIndex<Ucommerce.Search.Models.Product>>();
            ResultSet<Product> searchResult = search.Find().Where(x => x.Name == Match.FullText(model.SearchQuery)).ToList();

            return Ok(this.ConvertToFullTextSearchResultModel(searchResult, model.ProductDetailsPageId));
        }

        [Route(RouteConstants.SEARCH_SUGGESTIONS_ROUTE_VALUE)]
        [HttpPost]
        public IHttpActionResult Suggestions(FullTextDTO model)
        {
            //No OP yet.
            return Ok(new List<string>());
        }

        private IList<FullTextSearchResultDTO> ConvertToFullTextSearchResultModel(ResultSet<Product> products, Guid? productDetailsPageId)
        {
            var fullTextSearchResultModels = new List<FullTextSearchResultDTO>();
            var catalogContext = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogContext>();
            var catalogLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogLibrary>();
            var urlService = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IUrlService>();
            var currency = catalogContext.CurrentPriceGroup.CurrencyISOCode;
            var productsPrices = catalogLibrary.CalculatePrices(products.Select(x => x.Guid).ToList()).Items;
            var catalog = catalogContext.CurrentCatalog;
            catalogLibrary.GetCategories(catalog.Categories);
            var culture = System.Threading.Thread.CurrentThread.CurrentUICulture;

            foreach (var product in products)
            {

                var fullTextSearchResultDto = new FullTextSearchResultDTO()
                {
                    ThumbnailImageUrl = product.ThumbnailImageUrl,
                    Name = product.Name,
                    Url = urlService.GetUrl(catalog, product),
                    Price = new Money(productsPrices.First(x => x.ProductGuid == product.Guid).PriceInclTax, currency).ToString(),
                };

                fullTextSearchResultModels.Add(fullTextSearchResultDto);
            }

            return fullTextSearchResultModels;
        }
    }
}
