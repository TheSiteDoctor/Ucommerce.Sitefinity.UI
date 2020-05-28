using System.Linq;
using System.Web.Http;
using Telerik.Sitefinity.Abstractions;
using Ucommerce;
using Ucommerce.Api;
using Ucommerce.Content;
using Ucommerce.EntitiesV2;
using Ucommerce.Search.Slugs;
using UCommerce.Sitefinity.UI.Api.Model;
using UCommerce.Sitefinity.UI.Constants;

namespace UCommerce.Sitefinity.UI.Api
{
    /// <summary>
    /// API Controller exposing endpoints for managing the basket.
    /// </summary>
    public class BasketController : ApiController
    {
        
        [Route(RouteConstants.GET_BASKET_ROUTE_VALUE)]
        [HttpGet]
        public IHttpActionResult GetBasket()
        {
            var transactionLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();
            if (!transactionLibrary.HasBasket())
            {
                return Json(new BasketDTO());
            }

            return Json(this.GetBasketModel());
        }

        [Route(RouteConstants.ADD_VOUCHER_ROUTE_VALUE)]
        [HttpPost]
        public IHttpActionResult AddVoucher(AddVoucherDTO model)
        {
            var transactionLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();
            var marketingLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IMarketingLibrary>();

            var voucherAdded = marketingLibrary.AddVoucher(model.VoucherCode);
            transactionLibrary.ExecuteBasketPipeline();
            return Json(this.GetBasketModel());
        }

        [Route(RouteConstants.PRICE_GROUP_ROUTE_VALUE)]
        [HttpPut]
        public IHttpActionResult ChangePriceGroup(ChangePriceGroupDTO model)
        {
            var catalogLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogLibrary>();
            var priceGroupRepository = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IRepository<PriceGroup>>();
            catalogLibrary.ChangePriceGroup(priceGroupRepository.Get(model.PriceGroupId).Guid);

            return Ok();
        }

        [Route(RouteConstants.UPDATE_LINE_ITEM_ROUTE_VALUE)]
        [HttpPost]
        public IHttpActionResult UpdateLineItem(UpdateLineItemDTO model)
        {
            var transactionLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();

            transactionLibrary.UpdateLineItem(model.OrderlineId, model.NewQuantity);
            transactionLibrary.ExecuteBasketPipeline();

            return Json(this.GetBasketModel());
        }

        [Route(RouteConstants.ADD_TO_BASKET_ROUTE_VALUE)]
        [HttpPost]
        public IHttpActionResult Add(AddToBasketDTO model)
        {
            if (model.Quantity < 1)
            {
                var responseDTO = new OperationStatusDTO();
                responseDTO.Status = "failed";
                responseDTO.Message = "Quantity must be greater than 0";

                return this.Json(responseDTO);
            }
            
            var catalogLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogLibrary>();
            var transactionLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();

            string variantSku = null;
            Ucommerce.Search.Models.Product product = catalogLibrary.GetProduct(model.Sku);
            var variants = catalogLibrary.GetVariants(product).ToList();

            if (model.Variants == null || !model.Variants.Any())
            {
                var variant = variants.FirstOrDefault();

                if (variant != null)
                {
                    variantSku = variant.VariantSku;
                }
            }
            else
            {
                foreach (var v in model.Variants)
                {
                    variants = variants.Where(pv => pv.GetUserDefinedFields().Values.Any(pp => pp.ToString() == v.Value)).ToList();
                }

                var variant = variants.FirstOrDefault();
                if (variant != null)
                {
                    variantSku = variant.VariantSku;
                }
            }

            transactionLibrary.AddToBasket((int)model.Quantity, model.Sku, variantSku);
            return Json(this.GetBasketModel());
        }

        private BasketDTO GetBasketModel()
        {
            var model = new BasketDTO();
            var transactionLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();
            var catalogContext = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogContext>();
            var catalogLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogLibrary>();

            var basket = transactionLibrary.GetBasket(false);
            var urlService = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IUrlService>();

            foreach (var orderLine in basket.OrderLines)
            {
                var orderLineModel = this.MapOrderLine(orderLine);
                var currentCatalog = catalogContext.CurrentCatalog;
                orderLineModel.ProductUrl = urlService.GetUrl(currentCatalog, catalogLibrary.GetProduct(orderLine.Sku));

                orderLineModel.ThumbnailImageMediaUrl = catalogLibrary.GetProduct(orderLine.Sku).ThumbnailImageUrl;

                model.OrderLines.Add(orderLineModel);
            }

            foreach (var discount in basket.Discounts)
            {
                model.Discounts.Add(new DiscountDTO
                {
                    Name = discount.CampaignItemName,
                    Value = new Money(discount.AmountOffTotal, basket.BillingCurrency.ISOCode).ToString(),
                });
            }

            model.OrderTotal = new Money(basket.OrderTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            model.TaxTotal = new Money(basket.TaxTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            model.DiscountTotal = new Money(basket.DiscountTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            model.HasDiscount = basket.Discount.GetValueOrDefault() > 0;
            model.NumberOfItemsInBasket = basket.OrderLines.Sum(x => x.Quantity);
            model.ShippingTotal =
                new Money(basket.ShippingTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            model.PaymentTotal = new Money(basket.PaymentTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();

            return model;
        }

        private OrderLineDTO MapOrderLine(OrderLine orderLine)
        {
            var orderLineViewModel = new OrderLineDTO
            {
                ProductName = orderLine.ProductName,
                Quantity = orderLine.Quantity,
                UnitPrice = new Money(orderLine.Price, orderLine.PurchaseOrder.BillingCurrency.ISOCode).ToString(),
                Total = new Money(orderLine.Total.GetValueOrDefault(), orderLine.PurchaseOrder.BillingCurrency.ISOCode).ToString(),
                OrderlineId = orderLine.OrderLineId,
                HasDiscount = orderLine.Discounts.Any(),
            };

            return orderLineViewModel;
        }
    }
}
