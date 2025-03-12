using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Models.Vendors;

public partial record VendorReviewModel : BaseNopModel
{
    public string Title { get; set; }
    public string ReviewText { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
