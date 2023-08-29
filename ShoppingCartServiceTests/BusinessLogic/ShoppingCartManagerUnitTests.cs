using Moq;
using Moq.AutoMock;
using ShoppingCartService.BusinessLogic;
using ShoppingCartService.DataAccess;
using ShoppingCartService.DataAccess.Entities;
using ShoppingCartService.Models;
using ShoppingCartServiceTests.Builders;
using Xunit;
using static ShoppingCartServiceTests.Builders.CouponBuilder;
using static ShoppingCartServiceTests.Builders.CheckOutDtoBuilder;

namespace ShoppingCartServiceTests.BusinessLogic
{
    public class ShoppingCartManagerUnitTests : TestBase
    {
        private readonly AutoMocker _mocker;

        public ShoppingCartManagerUnitTests()
        {
            // This is boilerplate code and could be moved to TestBase
            // It has been left here for readability purposes
            _mocker = new AutoMocker();
            _mocker.Use(Mapper);
        }

        [Fact]
        public void CalculateTotals_CombineTheCouponDataWithShoppingCart()
        {
            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();
            fakeCouponRepository
                .Setup(x => x.FindById("coupon-1"))
                .Returns(CreateCoupon("coupon-1", CouponType.Amount, 100));

            var fakeCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeCartRepository.Setup(x => x.FindById("cart-1"))
                .Returns(new CartBuilder().WithId("cart-1").Build());

            var fakeCheckoutEngine = _mocker.GetMock<ICheckOutEngine>();
            fakeCheckoutEngine
                .Setup(x => x.CalculateTotals(It.IsAny<Cart>()))
                .Returns(CreateCheckOutDto(total: 150));

            _mocker.Use<ICouponEngine>(new CouponEngine());

            var target = _mocker.CreateInstance<ShoppingCartManager>();

            var result = target.CalculateTotals("cart-1", "coupon-1");

            Assert.Equal(100, result.CouponDiscount);
        }

        [Fact]
        public void CalculateTotals_SubtractCouponCodeFromTotal()
        {
            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();
            fakeCouponRepository
                .Setup(x => x.FindById("coupon-1"))
                .Returns(CreateCoupon("coupon-1", CouponType.Amount, 100));
            var fakeCartRepository = _mocker.GetMock<IShoppingCartRepository>();
            fakeCartRepository.Setup(x => x.FindById("cart-1"))
                .Returns(new CartBuilder().WithId("cart-1").Build());

            var fakeCheckoutEngine = _mocker.GetMock<ICheckOutEngine>();
            fakeCheckoutEngine
                .Setup(x => x.CalculateTotals(It.IsAny<Cart>()))
                .Returns(CreateCheckOutDto(total: 150));

            _mocker.Use<ICouponEngine>(new CouponEngine());
            var target = _mocker.CreateInstance<ShoppingCartManager>();

            var result = target.CalculateTotals("cart-1", "coupon-1");

            Assert.Equal(50, result.TotalAfterCoupon);
        }
    }
}