using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components;

public partial class OrderTotalsViewComponent : NopViewComponent
{
    protected readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly IGenericAttributeService _genericAttributeService;

    public OrderTotalsViewComponent(IShoppingCartModelFactory shoppingCartModelFactory,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IGenericAttributeService genericAttributeService)
    {
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
        _genericAttributeService = genericAttributeService;
    }

    protected virtual async Task<IList<ShoppingCartItem>> GetScopedShoppingCartAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var selectedIdsValue = await _genericAttributeService.GetAttributeAsync<string>(customer,
            NopCustomerDefaults.SelectedCheckoutCartItemIdsAttribute, store.Id);
        if (string.IsNullOrWhiteSpace(selectedIdsValue))
            return cart;

        var selectedIds = selectedIdsValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(idString => int.TryParse(idString, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();
        if (!selectedIds.Any())
            return cart;

        var scopedCart = cart.Where(item => selectedIds.Contains(item.Id)).ToList();
        return scopedCart.Any() ? scopedCart : cart;
    }

    public async Task<IViewComponentResult> InvokeAsync(bool isEditable)
    {
        var cart = await GetScopedShoppingCartAsync();
        var model = await _shoppingCartModelFactory.PrepareOrderTotalsModelAsync(cart, isEditable);
        return View(model);
    }
}
