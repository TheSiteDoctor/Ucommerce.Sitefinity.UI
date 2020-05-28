using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Web;
using Ucommerce;
using Ucommerce.Api;
using Ucommerce.Content;
using Ucommerce.EntitiesV2;
using Ucommerce.Search.Slugs;
using UCommerce.Sitefinity.UI.Mvc.ViewModels;

namespace UCommerce.Sitefinity.UI.Mvc.Model
{
    /// <summary>
    /// The Model class of the Cart MVC widget.
    /// </summary>
    public class CartModel : ICartModel
    {
        private Guid productDetailsPageId;
        private Guid nextStepId;
        private Guid redirectPageId;
        private readonly ITransactionLibrary _transactionLibraryInternal;
        private ICatalogLibrary _catalogLibrary;
        private IUrlService _urlService;
        private ICatalogContext _catalogContext;
        private IMarketingLibrary _marketingLibrary;


        public CartModel(Guid? nextStepId = null, Guid? productDetailsPageId = null, Guid? redirectPageId = null)
        {
            _urlService = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IUrlService>();;
            _transactionLibraryInternal = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();
            _catalogLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogLibrary>();
            _catalogContext = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogContext>();
            _marketingLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IMarketingLibrary>();
            this.nextStepId = nextStepId ?? Guid.Empty;
            this.productDetailsPageId = productDetailsPageId ?? Guid.Empty;
            this.redirectPageId = redirectPageId ?? Guid.Empty;
        }

        public virtual CartRenderingViewModel GetViewModel(string refreshUrl, string removeOrderLineUrl)
        {
            var basketVM = new CartRenderingViewModel();

            if (!_transactionLibraryInternal.HasBasket())
            {
                return basketVM;
            }

            PurchaseOrder basket = _transactionLibraryInternal.GetBasket(false);
            foreach (var orderLine in basket.OrderLines)
            {
                var product = _catalogLibrary.GetProduct(orderLine.Sku);
                var imageService = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IImageService>();
                var orderLineViewModel = new OrderlineViewModel
                {
                    Quantity = orderLine.Quantity,
                    ProductName = orderLine.ProductName,
                    Sku = orderLine.Sku,
                    VariantSku = orderLine.VariantSku,
                    Total = new Money(orderLine.Total.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString(),
                    Discount = orderLine.Discount,
                    Tax = new Money(orderLine.VAT, basket.BillingCurrency.ISOCode).ToString(),
                    Price = new Money(orderLine.Price, basket.BillingCurrency.ISOCode).ToString(),
                    ProductUrl = _urlService.GetUrl(_catalogContext.CurrentCatalog, product),
                    PriceWithDiscount = new Money(orderLine.Price - orderLine.UnitDiscount.GetValueOrDefault(),
                        basket.BillingCurrency.ISOCode).ToString(),
                    OrderLineId = orderLine.OrderLineId,
                    ThumbnailName = product.ThumbnailImageUrl,
                    ThumbnailUrl = product.ThumbnailImageUrl
                };
                basketVM.OrderLines.Add(orderLineViewModel);
            }

            this.GetDiscounts(basketVM, basket);
            basketVM.OrderTotal = new Money(basket.OrderTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            basketVM.DiscountTotal = basket.DiscountTotal.GetValueOrDefault() > 0
                ? new Money(basket.DiscountTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString()
                : "";
            basketVM.TaxTotal = new Money(basket.TaxTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            basketVM.SubTotal = new Money(basket.SubTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            basketVM.NextStepUrl = GetNextStepUrl(nextStepId);
            basketVM.RedirectUrl = GetRedirectUrl(redirectPageId);
            basketVM.RefreshUrl = refreshUrl;
            basketVM.RemoveOrderlineUrl = removeOrderLineUrl;

            return basketVM;
        }

        private void GetDiscounts(CartRenderingViewModel basketVM, PurchaseOrder basket)
        {
            foreach (var item in basket.Discounts)
            {
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    if (item.Description.Contains(","))
                    {
                        basketVM.Discounts = item.Description.Split(',').ToList();
                    }
                    else
                    {
                        basketVM.Discounts.Add(item.Description);
                    }
                }
            }
        }

        public virtual bool CanProcessRequest(Dictionary<string, object> parameters, out string message)
        {
            if (Telerik.Sitefinity.Services.SystemManager.IsDesignMode)
            {
                message = "The widget is in Page Edit mode.";
                return false;
            }

            object submitModel = null;

            if (parameters.TryGetValue("submitModel", out submitModel))
            {
                var updateModel = submitModel as CartUpdateBasket;

                if (updateModel != null)
                {
                    foreach (var item in updateModel.RefreshBasket)
                    {
                        if (item.OrderLineQty < 1)
                        {
                            message = string.Format("Quantity of {0} must be greater than 0", item.OrderLineId);
                            return false;
                        }
                    }
                }
            }

            message = null;
            return true;
        }

        public virtual CartUpdateBasketViewModel Update(CartUpdateBasket model)
        {
            foreach (var updateOrderline in model.RefreshBasket)
            {
                var newQuantity = updateOrderline.OrderLineQty;
                if (newQuantity <= 0)
                {
                    newQuantity = 0;
                }

                _transactionLibraryInternal.UpdateLineItemByOrderLineId(updateOrderline.OrderLineId, newQuantity);
            }

            _transactionLibraryInternal.ExecuteBasketPipeline();

            var updatedBasket = MapCartUpdate(model);

            return updatedBasket;
        }

        public virtual CartUpdateBasketViewModel RemoveVoucher(CartUpdateBasket model)
        {
            var basket = _transactionLibraryInternal.GetBasket(false);
            var prop = basket.OrderProperties.FirstOrDefault(v => v.Key == "voucherCodes");
            var vouchers = model.Vouchers;

            if (vouchers.Any())
            {
                foreach (var voucher in vouchers)
                {
                    if (prop != null)
                    {
                        prop.Value = prop.Value.Replace(voucher + ",", string.Empty);
                        prop.Save();
                    }
                }
            }

            basket.Save();
            _transactionLibraryInternal.ExecuteBasketPipeline();

            var updatedBasket = MapCartUpdate(model);
            updatedBasket.Vouchers.Except(vouchers).ToList();

            return updatedBasket;
        }

        public virtual CartUpdateBasketViewModel AddVoucher(CartUpdateBasket model)
        {
            if (model.Vouchers.Any())
            {
                foreach (var modelVoucher in model.Vouchers)
                {
                    _marketingLibrary.AddVoucher(modelVoucher);
                }
            }

            _transactionLibraryInternal.ExecuteBasketPipeline();
            var updatedBasket = MapCartUpdate(model);
            updatedBasket.Vouchers = model.Vouchers;

            return updatedBasket;
        }

        private static CartUpdateBasketViewModel MapOrderline(PurchaseOrder basket)
        {
            var updatedBasket = new CartUpdateBasketViewModel();

            foreach (var orderLine in basket.OrderLines)
            {
                var orderLineViewModel = new CartUpdateOrderline();
                orderLineViewModel.OrderlineId = orderLine.OrderLineId;
                orderLineViewModel.Quantity = orderLine.Quantity;
                orderLineViewModel.Total =
                    new Money(orderLine.Total.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
                orderLineViewModel.Discount = orderLine.Discount;
                orderLineViewModel.Tax = new Money(orderLine.VAT, basket.BillingCurrency.ISOCode).ToString();
                orderLineViewModel.Price = new Money(orderLine.Price, basket.BillingCurrency.ISOCode).ToString();
                orderLineViewModel.PriceWithDiscount =
                    new Money(orderLine.Price - orderLine.Discount, basket.BillingCurrency.ISOCode).ToString();

                updatedBasket.OrderLines.Add(orderLineViewModel);
            }

            return updatedBasket;
        }

        private CartUpdateBasketViewModel MapCartUpdate(CartUpdateBasket model)
        {
            var basket = _transactionLibraryInternal.GetBasket(false);
            var updatedBasket = MapOrderline(basket);

            string orderTotal = new Money(basket.OrderTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            string discountTotal = basket.DiscountTotal.GetValueOrDefault() > 0
                ? new Money(basket.DiscountTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString()
                : "";
            string taxTotal = new Money(basket.TaxTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();
            string subTotal = new Money(basket.SubTotal.GetValueOrDefault(), basket.BillingCurrency.ISOCode).ToString();

            updatedBasket.OrderTotal = orderTotal;
            updatedBasket.DiscountTotal = discountTotal;
            updatedBasket.TaxTotal = taxTotal;
            updatedBasket.SubTotal = subTotal;
            updatedBasket.Vouchers.AddRange(model.Vouchers);

            return updatedBasket;
        }

        private string GetNextStepUrl(Guid nextStepId)
        {
            var nextStepUrl = Pages.UrlResolver.GetPageNodeUrl(nextStepId);

            return Pages.UrlResolver.GetAbsoluteUrl(nextStepUrl);
        }

        private string GetRedirectUrl(Guid redirectPageId)
        {
            var redirectUrl = Pages.UrlResolver.GetPageNodeUrl(redirectPageId);

            return Pages.UrlResolver.GetAbsoluteUrl(redirectUrl);
        }
    }
}