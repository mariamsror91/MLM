﻿using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Catalog;

public partial record CategorySimpleModel : BaseNopEntityModel
{
    public CategorySimpleModel()
    {
        SubCategories = new List<CategorySimpleModel>();
    }

    public string Name { get; set; }

    public string SeName { get; set; }

    public int? NumberOfProducts { get; set; }

    public bool IncludeInTopMenu { get; set; }

    public List<CategorySimpleModel> SubCategories { get; set; }

    public bool HaveSubCategories { get; set; }

    public string Route { get; set; }

    public int? Parent { get; set; }

}