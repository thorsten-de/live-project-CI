﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ShoppingCartService.BusinessLogic.Exceptions;
using ShoppingCartService.BusinessLogic.Validation;
using ShoppingCartService.Controllers.Models;
using ShoppingCartService.DataAccess;
using ShoppingCartService.DataAccess.Entities;

namespace ShoppingCartService.BusinessLogic
{
    public class ShoppingCartManager
    {
        private readonly IAddressValidator _addressValidator;
        private readonly ICheckOutEngine _checkOutEngine;
        private readonly ICouponEngine _couponEngine;
        private readonly IMapper _mapper;
        private readonly IShoppingCartRepository _shoppingCartRepository;
        private readonly ICouponRepository _couponRepository;

        public ShoppingCartManager(IAddressValidator addressValidator,
            ICheckOutEngine checkOutEngine,
            ICouponEngine couponEngine,
            IShoppingCartRepository shoppingCartRepository,
            ICouponRepository couponRepository,
            IMapper mapper)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _mapper = mapper;
            _checkOutEngine = checkOutEngine;
            _couponEngine = couponEngine;
            _couponRepository = couponRepository;
            _addressValidator = addressValidator;
        }

        public IEnumerable<ShoppingCartDto> GetAllShoppingCarts()
        {
            var all = _shoppingCartRepository.FindAll();

            return _mapper.Map<IEnumerable<ShoppingCartDto>>(all);
        }

        public ShoppingCartDto GetCart(string id)
        {
            var cart = _shoppingCartRepository.FindById(id);

            return _mapper.Map<ShoppingCartDto>(cart);
        }

        public ShoppingCartDto Create(CreateCartDto createCart)
        {
            var cart = _mapper.Map<Cart>(createCart);

            if (!_addressValidator.IsValid(cart.ShippingAddress))
                throw new InvalidInputException("Cannot created shopping cart without shipping address");

            CheckForDuplicateProductId(cart);

            _shoppingCartRepository.Create(cart);

            return _mapper.Map<ShoppingCartDto>(cart);
        }

        public void AddItemToCart(string cartId, ItemDto item)
        {
            var cart = GetCartFromDb(cartId);

            var newItem = _mapper.Map<Item>(item);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == newItem.ProductId);

            if (existingItem == null)
            {
                cart.Items.Add(newItem);
            }
            else
            {
                var itemsAreNotTheSame = Math.Abs(existingItem.Price - newItem.Price) > 0.001 ||
                                         existingItem.ProductName != newItem.ProductName;
                if (itemsAreNotTheSame)
                    throw new InvalidInputException(
                        $"Cannot Add item {newItem} to existing item {existingItem} - properties are not the same");

                existingItem.Quantity += newItem.Quantity;
            }

            _shoppingCartRepository.Update(cart.Id, cart);
        }

        public void RemoveFromShoppingCart(string cartId, string productId)
        {
            var cart = GetCartFromDb(cartId);

            var existingProduct = FindProductInCart(productId, cart);

            cart.Items.Remove(existingProduct);

            _shoppingCartRepository.Update(cart.Id, cart);
        }

        public CheckoutDto CalculateTotals(string cartId, string couponId)
        {
            var cart = GetCartFromDb(cartId);

            var checkoutDto = _checkOutEngine.CalculateTotals(cart);

            var coupon = _couponRepository.FindById(couponId);

            var couponDiscount = _couponEngine.CalculateDiscount(checkoutDto, coupon);

            checkoutDto.CouponDiscount = couponDiscount;

            return checkoutDto;
        }

        private static Item FindProductInCart(string productId, Cart cart)
        {
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem == null)
                throw new ItemNotFoundInCartException($"item with product id {productId} not found in shopping cart");

            return existingItem;
        }

        private Cart GetCartFromDb(string cartId)
        {
            var cart = _shoppingCartRepository.FindById(cartId);

            if (cart == null) throw new ShoppingCartNotFoundException($"Shopping cart {cartId} not found");

            return cart;
        }

        private static void CheckForDuplicateProductId(Cart cart)
        {
            var duplicates = cart.Items.GroupBy(s => s.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                var duplicateList = string.Join('\n', duplicates.ToArray());
                throw new InvalidInputException(
                    $"Cannot created shopping cart with two or more items with the same product id\n:{duplicateList}");
            }
        }

        public void DeleteCart(string cartId)
        {
            _shoppingCartRepository.Remove(cartId);
        }
    }
}