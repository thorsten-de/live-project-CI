using System.Linq;
using AutoMapper;
using ShoppingCartService.BusinessLogic.Exceptions;
using ShoppingCartService.Controllers.Models;
using ShoppingCartService.DataAccess.Entities;
using ShoppingCartService.Models;

namespace ShoppingCartService.BusinessLogic
{
    public class CheckOutEngine : ICheckOutEngine
    {
        private readonly IMapper _mapper;
        private readonly IShippingCalculator _shippingCalculator;

        public CheckOutEngine(IShippingCalculator shippingCalculator, IMapper mapper)
        {
            _shippingCalculator = shippingCalculator;
            _mapper = mapper;
        }

        public CheckoutDto CalculateTotals(Cart cart)
        {
            if (cart.ShippingAddress == null)
                throw new MissingDataException("Cannot calculate total cost - missing Shipping method");

            var itemCost = cart.Items.Sum(item => item.Price * item.Quantity);
            var shippingCost = _shippingCalculator.CalculateShippingCost(cart);
            
            var customerDiscount = GetCustomerDiscountPercent(cart, out var customerDiscountPercent);

            var total = (itemCost + shippingCost) * customerDiscountPercent;

            var shoppingCartDto = _mapper.Map<ShoppingCartDto>(cart);

            return new CheckoutDto(shoppingCartDto, shippingCost, customerDiscount, total);
        }

        private static double GetCustomerDiscountPercent(Cart cart, out double customerDiscountPercent)
        {
            var customerDiscount = 0.0;
            if (cart.CustomerType == CustomerType.Premium) customerDiscount = 10.0;

            customerDiscountPercent = (100.0 - customerDiscount) / 100.0;
            return customerDiscount;
        }
    }
}