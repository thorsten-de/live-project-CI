using System;
using ShoppingCartService.DataAccess.Entities;
using ShoppingCartService.Models;

namespace ShoppingCartServiceTests.Builders
{
    public class CouponBuilder
    {
        public static Coupon CreateCoupon(
            string id = "coupon-1",
            CouponType couponType = CouponType.Amount,
            double value = 0,
            DateTime? expiration = null
        )
        {
            expiration ??= DateTime.Now.AddDays(1);

            return new Coupon
            {
                Id = id,
                CouponType = couponType,
                Value = value,
                Expiration = expiration.Value
            };
        }
    }
}