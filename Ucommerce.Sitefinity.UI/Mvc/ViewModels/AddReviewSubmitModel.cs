using System;

namespace UCommerce.Sitefinity.UI.Mvc.ViewModels
{
    public class AddReviewSubmitModel
    {
        //Sitefinity.uCommerce.UI review-form.js passes GUIDs not ints here!
        public Guid? CatalogGroupId { get; set; }
        public string CategoryGuid { get; set; }
        //Sitefinity.uCommerce.UI review-form.js passes GUIDs not ints here!
        public Guid? CategoryId { get; set; }
        public string Comments { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedOnForMeta { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string ProductGuid { get; set; }
        //Sitefinity.uCommerce.UI review-form.js passes GUIDs not ints here!
        public Guid? ProductId { get; set; }
        public int Rating { get; set; }
        public string ReviewHeadline { get; set; }
        public string Title { get; set; }
    }
}
