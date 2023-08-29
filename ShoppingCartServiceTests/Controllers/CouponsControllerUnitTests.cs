using System;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using ShoppingCartService.Controllers;
using ShoppingCartService.Controllers.Models;
using ShoppingCartService.DataAccess;
using ShoppingCartService.DataAccess.Entities;
using ShoppingCartService.Models;
using Xunit;
using static ShoppingCartServiceTests.Builders.CouponBuilder;

namespace ShoppingCartServiceTests.Controllers
{
    public class CouponsControllerUnitTests : TestBase
    {
        private readonly AutoMocker _mocker;

        public CouponsControllerUnitTests()
        {
            // This is boilerplate code and could be moved to TestBase
            // It has been left here for readability purposes
            _mocker = new AutoMocker();
            _mocker.Use(Mapper);
        }

        [Fact]
        public void CreateCoupon_ReturnCreatedAtRoute()
        {
            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();
            fakeCouponRepository
                .Setup(x => x.Create(It.IsAny<Coupon>()))
                .Callback((Coupon c) =>
                {
                    c.Id = "coupon-1"; // set id so that it would return to the controller
                });


            var target = _mocker.CreateInstance<CouponController>();

            var expiration = DateTime.Now.ToUniversalTime().Date;
            var createCouponDto = new CreateCouponDto(
                CouponType.Amount,
                10,
                expiration
            );

            var result = target.CreateCoupon(createCouponDto);

            Assert.IsType<CreatedAtRouteResult>(result.Result);

            // You are allowed to use two asserts if one dependent on the other
            var couponId = ((CreatedAtRouteResult) result.Result).RouteValues["id"].ToString();

            Assert.Equal("coupon-1", couponId);
        }

        [Fact]
        public void CreateCoupon_CreateCouponAtTheDatabase()
        {
            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();

            var target = _mocker.CreateInstance<CouponController>();
            fakeCouponRepository
                .Setup(x => x.Create(It.IsAny<Coupon>()))
                .Returns(CreateCoupon("coupon-1"));

            var expiration = DateTime.Now.ToUniversalTime().Date;
            var createCouponDto = new CreateCouponDto(
                CouponType.Amount,
                10,
                expiration
            );

            target.CreateCoupon(createCouponDto);

            var expected = new Coupon
            {
                CouponType = CouponType.Amount,
                Value = 10,
                Expiration = expiration
            };

            fakeCouponRepository.Verify(x => x.Create(expected));
        }

        [Fact]
        public void FindById_HasOneCouponWithSameId_returnAllCouponInformation()
        {
            const string couponId = "coupon-1";

            var coupon = CreateCoupon(couponId, CouponType.Percentage, 10);

            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();
            fakeCouponRepository
                .Setup(x => x.FindById(couponId))
                .Returns(coupon);

            var target = _mocker.CreateInstance<CouponController>();

            var actual = target.FindById(coupon.Id);

            var expected = new CouponDto(couponId, CouponType.Percentage, 10, coupon.Expiration);

            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public void FindById_notFound_returnNotFoundResult()
        {
            const string couponId = "coupon-1";

            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();
            fakeCouponRepository
                .Setup(x => x.FindById(couponId))
                .Returns<Coupon>(null);

            var target = _mocker.CreateInstance<CouponController>();

            var actual = target.FindById(couponId);

            Assert.IsType<NotFoundResult>(actual.Result);
        }

        [Fact]
        public void Delete_ReturnNoContentResultAndDeleteItem()
        {
            var fakeCouponRepository = _mocker.GetMock<ICouponRepository>();

            var target = _mocker.CreateInstance<CouponController>();

            var actual = target.DeleteCoupon("coupon-1");
            Assert.IsType<NoContentResult>(actual);

            fakeCouponRepository.Verify(x => x.DeleteById("coupon-1"));
        }
    }
}