using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Telerik.Sitefinity.Abstractions;
using Ucommerce;
using Ucommerce.Api;
using Ucommerce.EntitiesV2;
using UCommerce.Sitefinity.UI.Mvc.ViewModels;

namespace UCommerce.Sitefinity.UI.Mvc.Model
{
    /// <summary>
    /// The Model class of the Shipping Picker MVC widget.
    /// </summary>
    public class ShippingPickerModel : IShippingPickerModel
    {
        private Guid nextStepId;
        private Guid previousStepId;
        private readonly ITransactionLibrary _transactionLibrary;
        private readonly ICatalogLibrary _catalogLibrary;
        private readonly ICatalogContext _catalogContext;

        public ShippingPickerModel(ICatalogLibrary catalogLibrary, ICatalogContext catalogContext, Guid? nextStepId = null, Guid? previousStepId = null)
        {
            _catalogLibrary = catalogLibrary;
            _catalogContext = catalogContext;
            this.nextStepId = nextStepId ?? Guid.Empty;
            this.previousStepId = previousStepId ?? Guid.Empty;
            _transactionLibrary = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ITransactionLibrary>();
        }

        public virtual bool CanProcessRequest(Dictionary<string, object> parameters, out string message)
        {
            object mode = null;

            if (parameters.TryGetValue("mode", out mode) && mode != null)
            {
                if (mode.ToString() == "index")
                {
                    if (Telerik.Sitefinity.Services.SystemManager.IsDesignMode)
                    {
                        message = "The widget is in Page Edit mode.";
                        return false;
                    }
                }

                message = null;
                return true;
            }
            else
            {
                PurchaseOrder basketPurchaseOrder = null;
                try
                {
                    basketPurchaseOrder = _transactionLibrary.GetBasket();
                }
                catch (Exception ex)
                {
                    Log.Write(ex, ConfigurationPolicy.ErrorLog);
                }

                if (basketPurchaseOrder != null)
                {
                    var address = basketPurchaseOrder.GetAddress(Ucommerce.Constants.DefaultShipmentAddressName);

                    if (address == null)
                    {
                        message = "Address must be specified";
                        return false;
                    }
                    else
                    {
                        message = null;
                        return true;
                    }
                }

                message = "The checkout is not started yet";
                return false;
            }
        }

        public virtual ShippingPickerViewModel GetViewModel()
        {
            var shipmentPickerViewModel = new ShippingPickerViewModel();
            PurchaseOrder basketPurchaseOrder = null;

            try
            {
                var basket = _transactionLibrary.GetBasket();
            }
            catch (Exception ex)
            {
                Log.Write(ex, ConfigurationPolicy.ErrorLog);
                return null;
            }

            if (!basketPurchaseOrder.OrderLines.Any())
            {
                return null;
            }

            if (_transactionLibrary.HasBasket())
            {
                var shippingCountry = basketPurchaseOrder.GetAddress(Ucommerce.Constants.DefaultShipmentAddressName)?
                    .Country;
                if (shippingCountry != null)
                {
                    shipmentPickerViewModel.ShippingCountry = shippingCountry.Name;
                    var availableShippingMethods = _transactionLibrary.GetShippingMethods(shippingCountry);

                    if (basketPurchaseOrder.Shipments.Count > 0)
                    {
                        shipmentPickerViewModel.SelectedShippingMethodId =
                            basketPurchaseOrder.Shipments.FirstOrDefault()?.ShippingMethod.ShippingMethodId ?? 0;
                    }
                    else
                    {
                        shipmentPickerViewModel.SelectedShippingMethodId = -1;
                    }

                    foreach (var availableShippingMethod in availableShippingMethods)
                    {
                        var priceGroup = _catalogContext.CurrentPriceGroup;
                        var localizedShippingMethod = availableShippingMethod.ShippingMethodDescriptions.FirstOrDefault(s =>
                            s.CultureCode.Equals(CultureInfo.CurrentCulture.ToString()));
                        var price = availableShippingMethod.GetPriceForPriceGroup(PriceGroup.All().First(x => x.Guid == priceGroup.Guid));
                        var formattedPrice = new Money((price == null ? 0 : price.Price),
                            basketPurchaseOrder.BillingCurrency.ISOCode);

                        if (localizedShippingMethod != null)
                            shipmentPickerViewModel.AvailableShippingMethods.Add(new SelectListItem()
                            {
                                Selected = shipmentPickerViewModel.SelectedShippingMethodId ==
                                           availableShippingMethod.ShippingMethodId,
                                Text = String.Format(" {0} ({1})", localizedShippingMethod.DisplayName, formattedPrice),
                                Value = availableShippingMethod.ShippingMethodId.ToString()
                            });
                    }
                }
            }

            _transactionLibrary.ExecuteBasketPipeline();

            shipmentPickerViewModel.NextStepUrl = GetNextStepUrl(nextStepId);
            shipmentPickerViewModel.PreviousStepUrl = GetPreviousStepUrl(previousStepId);

            return shipmentPickerViewModel;
        }

        public virtual void CreateShipment(ShippingPickerViewModel createShipmentViewModel)
        {
            _transactionLibrary.CreateShipment(createShipmentViewModel.SelectedShippingMethodId,
                Ucommerce.Constants.DefaultShipmentAddressName, true);
            _transactionLibrary.ExecuteBasketPipeline();
        }

        private string GetNextStepUrl(Guid nextStepId)
        {
            var nextStepUrl = Pages.UrlResolver.GetPageNodeUrl(nextStepId);

            return Pages.UrlResolver.GetAbsoluteUrl(nextStepUrl);
        }

        private string GetPreviousStepUrl(Guid previousStepId)
        {
            var previousStepUrl = Pages.UrlResolver.GetPageNodeUrl(previousStepId);

            return Pages.UrlResolver.GetAbsoluteUrl(previousStepUrl);
        }
    }
}