using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Sitefinity.Data.Linq.Dynamic;
using Ucommerce.Api;
using Ucommerce.EntitiesV2;
using Ucommerce.Pipelines;
using UCommerce.Sitefinity.UI.Mvc.Model.Contracts;
using UCommerce.Sitefinity.UI.Mvc.ViewModels;

namespace UCommerce.Sitefinity.UI.Mvc.Model
{
    public class AddReviewModel : IAddReviewModel
    {
        private readonly IRepository<ProductReviewStatus> _productReviewStatusRepository;
        private readonly IOrderContext _orderContext;
        private readonly IPipeline<Ucommerce.EntitiesV2.ProductReview> _productReviewPipeline;
        private ICatalogContext _catalogContext;
        public AddReviewModel()
        {
            _productReviewStatusRepository = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IRepository<ProductReviewStatus>>();
            _orderContext = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IOrderContext>();
            _productReviewPipeline = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<IPipeline<Ucommerce.EntitiesV2.ProductReview>>();
            _catalogContext = Ucommerce.Infrastructure.ObjectFactory.Instance.Resolve<ICatalogContext>();
        }
        public bool CanProcessRequest(Dictionary<string, object> parameters, out string message)
        {
            if (Telerik.Sitefinity.Services.SystemManager.IsDesignMode)
            {
                message = "The widget is in Page Edit mode.";
                return false;
            }

            message = null;
            return true;
        }

        public virtual AddReviewDTO Add(AddReviewSubmitModel viewModel)
        {
            Product product;

            if (viewModel.ProductId.HasValue)
            {
                product = Ucommerce.EntitiesV2.Product.Get(viewModel.ProductId.Value);
            }
            else
            {
                product = Product.All().Single(x => x.Guid == _catalogContext.CurrentProduct.Guid);
            }

            var request = System.Web.HttpContext.Current.Request;
            var basket = _orderContext.GetBasket();
            var name = viewModel.Name;
            var email = viewModel.Email;
            var rating = viewModel.Rating * 20;
            var reviewHeadline = viewModel.Title;
            var reviewText = viewModel.Comments;

            if (basket.PurchaseOrder.Customer == null)
            {
                basket.PurchaseOrder.Customer = new Customer()
                {
                    FirstName = name,
                    LastName = String.Empty,
                    EmailAddress = email
                };
            }
            else
            {
                basket.PurchaseOrder.Customer.FirstName = name;
                if (basket.PurchaseOrder.Customer.LastName == null)
                {
                    basket.PurchaseOrder.Customer.LastName = String.Empty;
                }
                basket.PurchaseOrder.Customer.EmailAddress = email;
            }

            basket.PurchaseOrder.Customer.Save();

            ProductCatalogGroup catalogGroup;

            if (viewModel.CatalogGroupId.HasValue)
            {
                catalogGroup = Ucommerce.EntitiesV2.ProductCatalogGroup.Get(viewModel.CatalogGroupId.Value);
            }
            else
            {
                catalogGroup = Ucommerce.EntitiesV2.ProductCatalogGroup.All().Single(x => x.Guid == _catalogContext.CurrentCatalogGroup.Guid);
            }

            var review = new Ucommerce.EntitiesV2.ProductReview();
            review.ProductCatalogGroup = catalogGroup;
            review.ProductReviewStatus = _productReviewStatusRepository.SingleOrDefault(s => s.Name == "New");
            review.CreatedOn = DateTime.Now;
            review.CreatedBy = "System";
            review.Product = product;
            review.Customer = basket.PurchaseOrder.Customer;
            review.Rating = rating;
            review.ReviewHeadline = reviewHeadline;
            review.ReviewText = reviewText;
            review.Ip = request.UserHostName;

            var reviewDTO = new AddReviewDTO
            {
                Rating = rating,
                Comments = reviewText,
                ReviewHeadline = reviewHeadline,
                CreatedBy = review.CreatedBy,
                CreatedOn = review.CreatedOn.ToString("MMM dd, yyyy"),
                CreatedOnForMeta = review.CreatedOn.ToString("yyyy-MM-dd")
            };

            product.AddProductReview(review);

            _productReviewPipeline.Execute(review);

            return reviewDTO;
        }
    }
}