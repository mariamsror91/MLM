﻿using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Helpers;
using Nop.Services.Orders;

namespace Nop.Services.Customers;

/// <summary>
/// Customer report service
/// </summary>
public partial class CustomerReportService : ICustomerReportService
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IRepository<Customer> _customerRepository;
    protected readonly IRepository<Order> _orderRepository;

    #endregion

    #region Ctor

    public CustomerReportService(ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IRepository<Customer> customerRepository,
        IRepository<Order> orderRepository)
    {
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get best customers
    /// </summary>
    /// <param name="createdFromUtc">Order created date from (UTC); null to load all records</param>
    /// <param name="createdToUtc">Order created date to (UTC); null to load all records</param>
    /// <param name="os">Order status; null to load all records</param>
    /// <param name="ps">Order payment status; null to load all records</param>
    /// <param name="ss">Order shipment status; null to load all records</param>
    /// <param name="orderBy">1 - order by order total, 2 - order by number of orders</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the report
    /// </returns>
    public virtual async Task<IPagedList<BestCustomerReportLine>> GetBestCustomersReportAsync(DateTime? createdFromUtc,
        DateTime? createdToUtc, OrderStatus? os, PaymentStatus? ps, ShippingStatus? ss, OrderByEnum orderBy,
        int pageIndex = 0, int pageSize = 214748364)
    {
        int? orderStatusId = null;
        if (os.HasValue)
            orderStatusId = (int)os.Value;

        int? paymentStatusId = null;
        if (ps.HasValue)
            paymentStatusId = (int)ps.Value;

        int? shippingStatusId = null;
        if (ss.HasValue)
            shippingStatusId = (int)ss.Value;
        var query1 = from c in _customerRepository.Table
            join o in _orderRepository.Table on c.Id equals o.CustomerId
            where (!createdFromUtc.HasValue || createdFromUtc.Value <= o.CreatedOnUtc) &&
                  (!createdToUtc.HasValue || createdToUtc.Value >= o.CreatedOnUtc) &&
                  (!orderStatusId.HasValue || orderStatusId == o.OrderStatusId) &&
                  (!paymentStatusId.HasValue || paymentStatusId == o.PaymentStatusId) &&
                  (!shippingStatusId.HasValue || shippingStatusId == o.ShippingStatusId) &&
                  !o.Deleted &&
                  !c.Deleted
            select new { c, o };

        var query2 = from co in query1
            group co by co.c.Id into g
            select new
            {
                CustomerId = g.Key,
                OrderTotal = g.Sum(x => x.o.OrderTotal),
                OrderCount = g.Count()
            };
        query2 = orderBy switch
        {
            OrderByEnum.OrderByQuantity => query2.OrderByDescending(x => x.OrderCount),
            OrderByEnum.OrderByTotalAmount => query2.OrderByDescending(x => x.OrderTotal),
            _ => throw new ArgumentException("Wrong orderBy parameter", nameof(orderBy)),
        };
        var ranked = query2.Select((x, index) => new BestCustomerReportLine
        {
            CustomerId = x.CustomerId,
            OrderCount = x.OrderCount,
            OrderTotal = x.OrderTotal,
            Rank = index + 1,
            
        });
        var tmp = await ranked.ToPagedListAsync(pageIndex, pageSize);
        return new PagedList<BestCustomerReportLine>(tmp.Select(x => new BestCustomerReportLine
            {
                CustomerId = x.CustomerId,
                OrderTotal = x.OrderTotal,
                OrderCount = x.OrderCount,
                Rank = x.Rank
            }).ToList(),
            tmp.PageIndex, tmp.PageSize, tmp.TotalCount);
    }

    /// <summary>
    /// Gets a report of customers registered in the last days
    /// </summary>
    /// <param name="days">Customers registered in the last days</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the number of registered customers
    /// </returns>
    public virtual async Task<int> GetRegisteredCustomersReportAsync(int days)
    {
        var date = (await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.Now)).AddDays(-days);

        var registeredCustomerRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName);
        if (registeredCustomerRole == null)
            return 0;

        return (await _customerService.GetAllCustomersAsync(
            date,
            customerRoleIds: new[] { registeredCustomerRole.Id })).Count;
    }

    public async Task<List<GenderStatisticsModel>> GetGenderStatisticsAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _customerRepository.Table;

        if (startDate.HasValue)
            query = query.Where(c => c.CreatedOnUtc >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.CreatedOnUtc <= endDate.Value);

        var result = await query.GroupBy(c => c.Gender)
            .Select(g => new GenderStatisticsModel
            {
                Gender = g.Key,
                Count = g.Count()
            }).ToListAsync();

        return result;
    }

    public async Task<List<UsersStatisticsModel>> GetPeriodUserStatisticsAsync(DateTime? startDate, DateTime? endDate, string period = "")
    {
        var currentYear = startDate.HasValue? startDate.Value.Year : DateTime.UtcNow.Year;
        var months = Enumerable.Range(1, 12)
            .Select(month => new { Month = month, Year = currentYear })
            .ToList();

        var currentMonth = startDate.HasValue ? startDate.Value.Month : DateTime.UtcNow.Month;
        var daysinmonth = DateTime.DaysInMonth(currentYear, currentMonth);
        var days = Enumerable.Range(1, daysinmonth)
            .Select(day => new { Year = currentYear, Day = day, Month = currentMonth })
            .ToList();

        var query = _customerRepository.Table;

        if (startDate.HasValue)
            query = query.Where(c => c.CreatedOnUtc >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.CreatedOnUtc <= endDate.Value);
        var result = new List<UsersStatisticsModel>();
        if (period == "month")
        {
            var groupedData = await query.GroupBy(c => new { Year = c.CreatedOnUtc.Year, Month = c.CreatedOnUtc.Month })
            .Select(g => new UsersStatisticsModel
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            }).ToListAsync();

            result = months.GroupJoin(groupedData,
       m => new { m.Month, m.Year },
       g => new { g.Month, g.Year },
       (m, g) => new UsersStatisticsModel
       {
           Month = m.Month,
           Year = m.Year,
           Count = g.Sum(x => x.Count) // 0 if no data
       }).ToList();

        }
        if (period == "day")
        {
            var groupedData = await query
       .GroupBy(v => v.CreatedOnUtc.Date)
       .Select(g => new
       {
           Day = g.Key.Day,
           Month = g.Key.Month,
           Year = g.Key.Year,
           Count = g.Count()
       }).ToListAsync();

            // Merge grouped data with all days
            result = days.GroupJoin(groupedData,
                d => new { d.Day, d.Month, d.Year },
                g => new { g.Day, g.Month, g.Year },
                (d, g) => new UsersStatisticsModel
                {
                    Day = d.Day,
                    Month = d.Month,
                    Year = d.Year,
                    Count = g.Sum(x => x.Count) // Default to 0 if no data
                }).ToList();
        }
        

        return result;
    }

    //public async Task<List<UsersStatisticsModel>> GetPeriodVisitorsStatisticsAsync(DateTime? startDate, DateTime? endDate, string period)
    //{
    //    var query = _customerRepository.Table.Where(x=>);

    //    if (startDate.HasValue)
    //        query = query.Where(c => c.CreatedOnUtc >= startDate.Value);

    //    if (endDate.HasValue)
    //        query = query.Where(c => c.CreatedOnUtc <= endDate.Value);
    //    var result = new List<UsersStatisticsModel>();
    //    if (period == "month")
    //    {
    //        result = await query.GroupBy(c => new { Year = c.CreatedOnUtc.Year, Month = c.CreatedOnUtc.Month })
    //        .Select(g => new UsersStatisticsModel
    //        {
    //            Year = g.Key.Year,
    //            Month = g.Key.Month,
    //            Count = g.Count()
    //        }).ToListAsync();
    //    }
    //    if (period == "day")
    //    {
    //        result = await query.GroupBy(c => new { Year = c.CreatedOnUtc.Year, Month = c.CreatedOnUtc.Month, Day = c.CreatedOnUtc.Day })
    //        .Select(g => new UsersStatisticsModel
    //        {
    //            Year = g.Key.Year,
    //            Month = g.Key.Month,
    //            Day = g.Key.Day,
    //            Count = g.Count()
    //        }).ToListAsync();
    //    }


    //    return result;
    //}
    #endregion
}