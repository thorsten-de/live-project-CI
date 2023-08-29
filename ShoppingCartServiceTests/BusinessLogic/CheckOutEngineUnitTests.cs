using System.Collections.Generic;
using Moq.AutoMock;
using ShoppingCartService.BusinessLogic;
using ShoppingCartService.BusinessLogic.Exceptions;
using ShoppingCartService.DataAccess.Entities;
using ShoppingCartService.Models;
using ShoppingCartServiceTests.Builders;
using Xunit;
using static ShoppingCartServiceTests.Builders.AddressBuilder;
using static ShoppingCartServiceTests.Builders.ItemBuilder;

namespace ShoppingCartServiceTests.BusinessLogic
{
    public class CheckOutEngineUnitTests : TestBase
    {

        private readonly AutoMocker _mocker;

        public CheckOutEngineUnitTests()
        {
            // This is boilerplate code and could be moved to TestBase
            // It has been left here for readability purposes
            _mocker = new AutoMocker();
            _mocker.Use(Mapper);
        }

        [Fact]
        public void CalculateTotals_NoShippingAddress_ThrowException()
        {
            _mocker.Use<IShippingCalculator>(new ShippingCalculator());
            var target = _mocker.CreateInstance<CheckOutEngine>();
            var cart = new CartBuilder()
                .WithShippingAddress(null)
                .Build();

            Assert.Throws<MissingDataException>(() => target.CalculateTotals(cart));
        }

        [Theory]
        [InlineData(CustomerType.Standard, 0)]
        [InlineData(CustomerType.Premium, 10)]
        public void CalculateTotals_DiscountBasedOnCustomerType(CustomerType customerType, double expectedDiscount)
        {
            var address = CreateAddress();

            _mocker.Use<IShippingCalculator>(new ShippingCalculator());

            var target = _mocker.CreateInstance<CheckOutEngine>();

            var cart = new CartBuilder()
                .WithCustomerType(customerType)
                .WithShippingAddress(address)
                .Build();

            var result = target.CalculateTotals(cart);


            Assert.Equal(expectedDiscount, result.CustomerDiscount);
        }


        [Theory]
        [InlineData(ShippingMethod.Standard)]
        [InlineData(ShippingMethod.Express)]
        [InlineData(ShippingMethod.Expedited)]
        [InlineData(ShippingMethod.Priority)]
        public void CalculateTotals_StandardCustomer_TotalEqualsCostPlusShipping(ShippingMethod shippingMethod)
        {
            var originAddress = CreateAddress(city: "city 1");
            _mocker.Use<IShippingCalculator>(new ShippingCalculator(originAddress));
            var destinationAddress = CreateAddress(city: "city 2");

            var target = _mocker.CreateInstance<CheckOutEngine>();

            var cart = new CartBuilder()
                .WithShippingAddress(destinationAddress)
                .WithShippingMethod(shippingMethod)
                .WithItems(new List<Item>
                {
                    CreateItem(price: 2, quantity: 3)
                })
                .Build();

            var result = target.CalculateTotals(cart);

            Assert.Equal(2 * 3 + result.ShippingCost, result.Total);
        }

        [Fact]
        public void CalculateTotals_MoreThanOneItem_TotalEqualsCostPlusShipping()
        {
            var originAddress = CreateAddress(city: "city 1");
            _mocker.Use<IShippingCalculator>(new ShippingCalculator(originAddress));

            var destinationAddress = CreateAddress(city: "city 2");

            var target = _mocker.CreateInstance<CheckOutEngine>();

            var cart = new CartBuilder()
                .WithShippingAddress(destinationAddress)
                .WithShippingMethod(ShippingMethod.Standard)
                .WithItems(new List<Item>
                {
                    CreateItem(price: 2, quantity: 3),
                    CreateItem(price: 4, quantity: 5)
                })
                .Build();


            var result = target.CalculateTotals(cart);

            Assert.Equal(2 * 3 + 4 * 5 + result.ShippingCost, result.Total);
        }

        [Fact]
        public void CalculateTotals_PremiumCustomer_TotalEqualsCostPlusShippingMinusDiscount()
        {
            var originAddress = CreateAddress(city: "city 1");
            _mocker.Use<IShippingCalculator>(new ShippingCalculator(originAddress));

            var destinationAddress = CreateAddress(city: "city 2");

            var target = _mocker.CreateInstance<CheckOutEngine>();

            var cart = new CartBuilder()
                .WithCustomerType(CustomerType.Premium)
                .WithShippingAddress(destinationAddress)
                .WithItems(new List<Item>
                {
                    CreateItem(price: 2, quantity: 3)
                })
                .Build();
            var result = target.CalculateTotals(cart);

            Assert.Equal((2 * 3 + result.ShippingCost) * 0.9, result.Total);
        }
    }
}