module Tests

open Xunit
open FsUnit.Xunit

open simpleStore.Domain
open simpleStore.Services

// -------------------------
// 1) اختبار كاتالوج المنتجات
// -------------------------

[<Fact>]
let ``Catalog should return product with Id 1`` () =
    let catalog = Catalog.initCatalog()

    let result = Catalog.getProduct 1 catalog

    match result with
    | Some p ->
        p.Id |> should equal 1
        p.Name |> should not' (be Empty)
    | None ->
        failwith "Product not found in catalog"

// -------------------------
// 2) addToCart لأول مرة
// -------------------------

[<Fact>]
let ``addToCart should add product with quantity 1`` () =
    let product = { Id = 999; Name = "TestProduct"; Price = 10.0m; Category = "Test" }

    let cart = []
    let updated = Cart.addToCart product cart

    updated.Length |> should equal 1
    updated.Head.Quantity |> should equal 1
    updated.Head.Product |> should equal product

// -------------------------
// 3) addToCart مرتين يجب أن يزيد الكمية
// -------------------------

[<Fact>]
let ``addToCart twice should increase quantity`` () =
    let product = { Id = 999; Name = "TestProduct"; Price = 10.0m; Category = "Test" }

    let cart1 = Cart.addToCart product []
    let cart2 = Cart.addToCart product cart1

    cart2.Length |> should equal 1
    cart2.Head.Quantity |> should equal 2

// -------------------------
// 4) removeFromCart
// -------------------------

[<Fact>]
let ``removeFromCart should decrease quantity`` () =
    let product = { Id = 10; Name = "X"; Price = 5m; Category = "Test" }

    let cart = [
        { Product = product; Quantity = 2 }
    ]

    let updated = Cart.removeFromCart 10 cart

    updated.Head.Quantity |> should equal 1

[<Fact>]
let ``removeFromCart should remove item when quantity becomes 0`` () =
    let product = { Id = 10; Name = "X"; Price = 5m; Category = "Test" }

    let cart = [
        { Product = product; Quantity = 1 }
    ]

    let updated = Cart.removeFromCart 10 cart

    updated |> should be Empty
