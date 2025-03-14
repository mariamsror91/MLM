﻿namespace Nop.Core.Domain.Orders;

/// <summary>
/// Represents a best sellers report line
/// </summary>
[Serializable]
public partial class BestsellersReportLine
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// Gets or sets the total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the total quantity
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Gets or sets the CategoryName
    /// </summary>
    public string CategoryName { get; set; }
    /// <summary>
    /// Gets or sets the Vendor
    /// </summary>
    public string VendorName { get; set; }


    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string ProductImage { get; set; }


}