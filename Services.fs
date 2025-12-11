module simpleStore.Services

open System
open System.IO
open System.Text.Json
open simpleStore.Domain

// مثال: Catalog, Cart, Pricing, FileIO (مختصر)
module Catalog =
    let initialProducts = [
        { Id = 1; Name = "Gaming Laptop"; Price = 1200.00m; Category = "Electronics" }
        { Id = 2; Name = "Mechanical Keyboard"; Price = 85.50m; Category = "Electronics" }
        // ... ضع بقية المنتجات هنا أو استخدم products.json
    ]

    let initCatalog () =
        initialProducts |> List.map (fun p -> (p.Id, p)) |> Map.ofList

    let getProduct id catalog = Map.tryFind id catalog

module Cart =
    let addToCart (product: Product) (cart: CartItem list) =
        match cart |> List.tryFind (fun item -> item.Product.Id = product.Id) with
        | Some existingItem ->
            let newItem = { existingItem with Quantity = existingItem.Quantity + 1 }
            cart |> List.map (fun item -> if item.Product.Id = product.Id then newItem else item)
        | None ->
            { Product = product; Quantity = 1 } :: cart

    let removeFromCart (productId: int) (cart: CartItem list) =
        match cart |> List.tryFind (fun item -> item.Product.Id = productId) with
        | Some item when item.Quantity > 1 ->
            let newItem = { item with Quantity = item.Quantity - 1 }
            cart |> List.map (fun i -> if i.Product.Id = productId then newItem else i)
        | Some _ ->
            cart |> List.filter (fun item -> item.Product.Id <> productId)
        | None -> cart

module Pricing =
    let calculateTotal (cart: CartItem list) =
        cart |> List.sumBy (fun item -> item.Product.Price * decimal item.Quantity)

module FileIO =
    type Receipt = { Date: DateTime; Items: CartItem list; Total: decimal }
    let private jsonOptions () = JsonSerializerOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)

    let saveReceipt (cart: CartItem list) (total: decimal) =
        let filePath = "receipts.json"
        let newReceipt = { Date = DateTime.Now; Items = cart; Total = total }
        let options = jsonOptions()
        let existing =
            if File.Exists(filePath) then
                let content = File.ReadAllText(filePath)
                if String.IsNullOrWhiteSpace(content) then "[]" else content
            else "[]"
        // deserialize to array safely
        let arr =
            try
                JsonSerializer.Deserialize<Receipt[]>(existing, options)
            with _ -> [||]
        let updated = Array.append arr [| newReceipt |]
        File.WriteAllText(filePath, JsonSerializer.Serialize(updated, options))

    let loadCart () =
        let filePath = "cart.json"
        try
            if File.Exists(filePath) then
                let json = File.ReadAllText(filePath)
                if String.IsNullOrWhiteSpace(json) then [] else JsonSerializer.Deserialize<CartItem list>(json)
            else []
        with _ -> []
    let saveCart (cart: CartItem list) =
        let filePath = "cart.json"
        let options = jsonOptions()
        File.WriteAllText(filePath, JsonSerializer.Serialize(cart, options))
