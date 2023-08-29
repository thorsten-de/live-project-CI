using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using ShoppingCartService.BusinessLogic;
using ShoppingCartService.BusinessLogic.Validation;
using ShoppingCartService.Controllers;
using ShoppingCartService.Controllers.Models;
using ShoppingCartService.DataAccess;
using ShoppingCartService.DataAccess.Entities;
using ShoppingCartService.Models;
using ShoppingCartServiceTests.Builders;
using Xunit;
using static ShoppingCartServiceTests.Builders.CouponBuilder;
using static ShoppingCartServiceTests.Builders.CheckOutDtoBuilder;
using static ShoppingCartServiceTests.Builders.ItemBuilder;
using static ShoppingCartServiceTests.Builders.AddressBuilder;

namespace ShoppingCartServiceTests.Controllers
{
    public class ShoppingCartControllerUnitTests : TestBase
    {
        private readonly AutoMocker _mocker;

        public ShoppingCartControllerUnitTests()
        {
            // This is boilerplate code and could be moved to TestBase
            // It has been left here for readability purposes
            _mocker = new AutoMocker();
            _mocker.Use(Mapper);          
        }

        [Fact]
        public void GetAll_HasOneCart_returnAllShoppingCartsInformation()
        {
            var cart = new CartBuilder()
                .WithId("cart-1")
                .WithCustomerId("1")
                .WithItems(new List<Item> {CreateItem()})
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindAll()).Returns(new List<Cart> {cart});

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var actual = target.GetAll();

            var cartItem = cart.Items[0];
            var expected =
                new ShoppingCartDto
                {
                    Id = cart.Id,
                    CustomerId = cart.CustomerId,
                    CustomerType = cart.CustomerType,
                    ShippingAddress = cart.ShippingAddress,
                    ShippingMethod = cart.ShippingMethod,
                    Items = new List<ItemDto>
                    {
                        new(cartItem.ProductId,
                            cartItem.ProductName,
                            cartItem.Price,
                            cartItem.Quantity
                        )
                    }
                };

            Assert.Equal(expected, actual.Single());
        }

        [Fact]
        public void FindById_HasOneCartWithSameId_returnAllShoppingCartsInformation()
        {
            const string cartId = "cart-1";

            var cart = new CartBuilder()
                .WithId("cart-1")
                .WithCustomerId("1")
                .WithItems(new List<Item> {CreateItem()})
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById(cartId)).Returns(cart);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var actual = target.FindById(cartId);

            var cartItem = cart.Items[0];
            var expected =
                new ShoppingCartDto
                {
                    Id = cart.Id,
                    CustomerId = cart.CustomerId,
                    CustomerType = cart.CustomerType,
                    ShippingAddress = cart.ShippingAddress,
                    ShippingMethod = cart.ShippingMethod,
                    Items = new List<ItemDto>
                    {
                        new(cartItem.ProductId,
                            cartItem.ProductName,
                            cartItem.Price,
                            cartItem.Quantity
                        )
                    }
                };

            Assert.Equal(expected, actual.Value);
        }


        [Fact]
        public void FindById_ItemNotFound_returnNotFoundResult()
        {
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();

            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns<Cart>(null);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var actual = target.FindById("cart-1");

            Assert.IsType<NotFoundResult>(actual.Result);
        }

        [Fact]
        public void CalculateTotals_ShoppingCartNotFound_ReturnNotFound()
        {
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns<Cart>(null);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var actual = target.CalculateTotals("cart-1", null);

            Assert.IsType<NotFoundResult>(actual.Result);
        }

        [Fact]
        public void CalculateTotals_ShippingCartFound_ReturnTotals()
        {
            var cart = new CartBuilder()
                .WithId("cart-1")
                .WithItems(new List<Item> {CreateItem(quantity:1 ,price:10)})
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);

            _mocker.Use<IAddressValidator>(new AddressValidator());
            _mocker.Use<ICouponEngine>(new CouponEngine()); // we need a REAL coupon engine
            _mocker.Use<ICheckOutEngine>(_mocker.CreateInstance<CheckOutEngine>()); // we need a REAL checkout engine
            _mocker.Use(_mocker.CreateInstance<ShoppingCartManager>());

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var actual = target.CalculateTotals(cart.Id, null);

            Assert.Equal(10.0, actual.Value.Total);
        }

        [Fact]
        public void Create_ValidData_SaveShoppingCartToDB()
        {
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.Create(It.IsAny<Cart>()))
                .Callback((Cart c) =>
                {
                    c.Id = "cart-1"; // set it to be used by controller
                })
                .Returns(new Cart {Id = "cart-1"});

            _mocker.Use<IAddressValidator>(new AddressValidator());
            _mocker.Use<ICheckOutEngine>(_mocker.CreateInstance<CheckOutEngine>()); // we need a REAL checkout engine
            _mocker.Use(_mocker.CreateInstance<ShoppingCartManager>());

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var customerDto = new CustomerDto
            {
                Id = "customer-1",
                Address = CreateAddress()
            };
           
            var result = target.Create(new CreateCartDto
            {
                Customer = customerDto,

                Items = new[] {CreateItemDto()}
            });

            Assert.IsType<CreatedAtRouteResult>(result.Result);
            var cartId = ((CreatedAtRouteResult) result.Result).RouteValues["id"].ToString();
            Assert.Equal("cart-1", cartId);
        }

        [Fact]
        public void Create_DuplicateItem_ReturnBadRequestResult()
        {
            var target = _mocker.CreateInstance<ShoppingCartController>();

            var itemDto = CreateItemDto();
            var result = target.Create(new CreateCartDto
            {
                Customer = new CustomerDto
                {
                    Address = CreateAddress()
                },

                Items = new[] {itemDto, CreateItemDto(itemDto.ProductId)}
            });

            Assert.IsType<BadRequestResult>(result.Result);
        }

        public static List<object[]> InvalidAddresses()
        {
            return new()
            {
                new object[] {null},
                new object[] {CreateAddress(null)},
                new object[] {CreateAddress(city: null)},
                new object[] {CreateAddress(street: null)}
            };
        }

        [Theory]
        [MemberData(nameof(InvalidAddresses))]
        public void Create_InValidAddress_ReturnBadRequestResult(Address address)
        {
            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.Create(new CreateCartDto
            {
                Customer = new CustomerDto
                {
                    Address = address
                },
                Items = new[] {CreateItemDto()}
            });

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public void Delete_ValidData_RemoveShoppingCartToDB()
        {
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();

            var target = _mocker.CreateInstance<ShoppingCartController>();

            target.DeleteCart("cart-1");

            fakeShoppingCartRepository.Verify(x => x.Remove("cart-1"));
        }

        [Fact]
        public void CalculateTotals_InvalidCouponExceptionThrown_ReturnBadRequest()
        {
            var coupon = CreateCoupon("coupon-1", CouponType.Amount, 100);
            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();
            fakeCouponRepository.Setup(x => x.FindById("coupon-1")).Returns(coupon);

            var cart = new CartBuilder()
                .WithId("cart-1")
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1"))
                .Returns(cart); // I do not care about this result

            var fakeCheckoutEngine = _mocker.GetMock<ICheckOutEngine>();
            fakeCheckoutEngine
                .Setup(x => x.CalculateTotals(It.IsAny<Cart>()))
                .Returns(CreateCheckOutDto(total: 0));

            _mocker.Use<IAddressValidator>(new AddressValidator());
            _mocker.Use<ICouponEngine>(new CouponEngine()); // we need a REAL coupon engine
            _mocker.Use<ICheckOutEngine>(_mocker.CreateInstance<CheckOutEngine>()); // we need a REAL checkout engine
            _mocker.Use(_mocker.CreateInstance<ShoppingCartManager>());

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result =
                target.CalculateTotals("cart-1", "coupon-1"); // cart id not use as fake cart repository was faked 

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public void AddItemToCart_shoppingCartNotFound_ReturnNotFound()
        {
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns<Cart>(null);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.AddItemToCart("cart-1", CreateItemDto());

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void AddItemToCart_shoppingCartItemWithSameIdNotInCart_CreateNewItem()
        {
            var cart = new CartBuilder()
                .WithId("cart-1")
                .WithItems(new List<Item> {CreateItem("other")})
                .Build();

            string[] actualItemIds = null;
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);
            fakeShoppingCartRepository.Setup(x => x.Update("cart-1", It.IsAny<Cart>()))
                .Callback((string _, Cart c) =>
                {
                    actualItemIds = c.Items.Select(i => i.ProductId).ToArray();
                }); // this is a trick to extract complex objects and using assert instead of using verify (see below)

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.AddItemToCart("cart-1", CreateItemDto("item-1"));

            Assert.IsType<OkResult>(result.Result);

            // This is a verify that only looks like an assertion
            Assert.Equal(new[] {"other", "item-1"}, actualItemIds);

            /* This is the alternative, the problem is that when this test fail it is hard to know why
             
            fakeShoppingCartRepository.Verify(x => x.Update("cart-1", 
                It.Is((Cart c )=> c.Items.Select(i => i.ProductId).ToArray() == new[] { "other", "item-1" })));
            */
        }

        [Fact]
        public void AddItemToCart_shoppingCartItemWIthSameIdInCartSameDetails_IncreaseItemCount()
        {
            const string productId = "item-1";
            const string productName = "name";
            const double price = 1;

            var cart = new CartBuilder()
                .WithItems(new List<Item>
                    {CreateItem(productId, productName, price, 2)})
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.AddItemToCart("cart-1",
                CreateItemDto(productId, productName, price, 3));

            Assert.IsType<OkResult>(result.Result);

            fakeShoppingCartRepository.Verify(x =>
                x.Update("cart-1", It.Is<Cart>(c => c.Items.Single().Quantity == 5)));
        }

        [Fact]
        public void AddItemToCart_shoppingCartItemWIthSameIdInCartDifferentName_DoNotSaveReturnBadRequest()
        {
            const string productId = "item-1";
            const double price = 1;

            var cart = new CartBuilder()
                .WithItems(new List<Item>
                {
                    CreateItem(productId, "product name", price, 1)
                })
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);

            var target = _mocker.CreateInstance<ShoppingCartController>();

                         var result = target.AddItemToCart("cart-1",
                CreateItemDto(productId, "other name", price, 2));

            Assert.IsType<BadRequestResult>(result.Result);

            fakeShoppingCartRepository.Verify(x => x.Update("cart-1", It.IsAny<Cart>()), Times.Never());
        }

        [Fact]
        public void AddItemToCart_shoppingCartItemWIthSameIdInCartDifferentPrice_DoNotSaveReturnBadRequest()
        {
            const string productId = "item-1";
            const string productName = "name";

            var cart = new CartBuilder()
                .WithItems(new List<Item>
                    {CreateItem(productId, productName, 1.0, 1)})
                .Build();

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.AddItemToCart("cart-1",
                CreateItemDto(productId, productName, 2.0, 2));

            Assert.IsType<BadRequestResult>(result.Result);
            fakeShoppingCartRepository.Verify(x => x.Update("cart-1", It.IsAny<Cart>()), Times.Never());
        }

        [Fact]
        public void RemoveItemFromCart_ShoppingCartNotFound_ReturnNotFound()
        {
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns<Cart>(null);

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.RemoveItemFromCart("cart-1", "item-1");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void RemoveItemFromCart_ItemWithProductIdNotFoundNotFound_ReturnNotFound()
        {
            var cart = new CartBuilder().Build();
            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);

            var target = _mocker.CreateInstance<ShoppingCartController>();
            
            var result = target.RemoveItemFromCart("cart-1", "item-1");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void RemoveItemFromCart_ItemWithProductIdFound_RemoveItemFromCart()
        {
            var cart = new CartBuilder()
                .WithItems(new List<Item>
                {
                    CreateItem("item-1"),
                    CreateItem("item-2"),
                    CreateItem("item-3")
                })
                .Build();

            string[] actualItemIds = null;

            var fakeShoppingCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeShoppingCartRepository.Setup(x => x.FindById("cart-1")).Returns(cart);
            fakeShoppingCartRepository.Setup(x => x.Update("cart-1", It.IsAny<Cart>()))
                .Callback((string _, Cart c) =>
                {
                    actualItemIds = c.Items.Select(i => i.ProductId).ToArray();
                }); // this is a trick to extract complex objects and using assert instead of using verify (see below)

            var target = _mocker.CreateInstance<ShoppingCartController>();

            var result = target.RemoveItemFromCart("cart-1", "item-2");

            Assert.IsType<OkResult>(result);

            Assert.Equal(new[] {"item-1", "item-3"}, actualItemIds);
            // This is a verify that only looks like an assertion
            Assert.Equal(new[] {"item-1", "item-3"}, actualItemIds);

            /* This is the alternative, the problem is that when this test fail it is hard to know why
             
            fakeShoppingCartRepository.Verify(x => x.Update("cart-1", 
                It.Is((Cart c )=> c.Items.Select(i => i.ProductId).ToArray() == new[] { "item-1", "item-3" })));
            */
        }
    }
}