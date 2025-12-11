// app.js — Integrated with backend API (/api/products, /api/cart, /api/receipt)

const productsEl = document.getElementById('products');
const cartItemsEl = document.getElementById('cartItems');
const totalEl = document.getElementById('total');
const searchInput = document.getElementById('search');
const checkoutBtn = document.getElementById('checkoutBtn');
const saveCartBtn = document.getElementById('saveCartBtn');

let products = [];
let displayedProducts = [];
let cart = [];

// localStorage key
const CART_KEY = 'simple_store_cart_v1';

// ----------------- helpers -----------------
const money = n => '$' + Number(n).toFixed(2);

function loadCartFromStorage(){
  try{
    const raw = localStorage.getItem(CART_KEY);
    cart = raw ? JSON.parse(raw) : [];
  }catch(e){
    console.error('Failed to parse saved cart:', e);
    cart = [];
  }
}

function saveCartToStorage(){
  try{
    localStorage.setItem(CART_KEY, JSON.stringify(cart));
  }catch(e){
    console.error('Failed to save cart to storage:', e);
  }
}

function calculateTotal(){
  return cart.reduce((s,i)=> s + (i.product.Price * i.quantity), 0);
}

// ----------------- CART UI -----------------
function renderCart(){
  cartItemsEl.innerHTML = '';
  if(cart.length === 0){
    cartItemsEl.innerHTML = '<div style="color:#6b7280">Cart is empty</div>';
  } else {
    cart.forEach((item, idx) => {
      const node = document.createElement('div');
      node.className = 'cart-item';
      node.innerHTML = `
        <div class="left">
          <div class="name">${escapeHtml(item.product.Name)}</div>
          <div class="small">${money(item.product.Price)} • x${item.quantity}</div>
        </div>
        <div class="actions">
          <div class="qty">
            <button data-idx="${idx}" data-op="dec">-</button>
            <span style="min-width:30px;text-align:center;display:inline-block">${item.quantity}</span>
            <button data-idx="${idx}" data-op="inc">+</button>
          </div>
          <button data-idx="${idx}" data-op="remove" class="btn outline" style="margin-left:8px">Remove</button>
        </div>
      `;
      cartItemsEl.appendChild(node);
    });
  }

  totalEl.textContent = money(calculateTotal());
  attachCartListeners();
}

function attachCartListeners(){
  cartItemsEl.querySelectorAll('button').forEach(btn=>{
    btn.onclick = () => {
      const idx = Number(btn.getAttribute('data-idx'));
      const op = btn.getAttribute('data-op');
      if(op === 'inc'){ cart[idx].quantity += 1; }
      else if(op === 'dec'){ cart[idx].quantity = Math.max(1, cart[idx].quantity - 1); }
      else if(op === 'remove'){ cart.splice(idx,1); }
      saveCartToStorage();
      renderCart();
    };
  });
}

function addToCart(product){
  const found = cart.find(x=> x.product.Id === product.Id);
  if(found) found.quantity += 1;
  else cart.push({ product, quantity: 1 });
  saveCartToStorage();
  renderCart();
}

// ----------------- PRODUCTS UI -----------------
function renderProducts(list){
  productsEl.innerHTML = '';
  if(!Array.isArray(list) || list.length === 0){
    productsEl.innerHTML = '<div style="color:#6b7280">No products found</div>';
    return;
  }
  list.forEach(p=>{
    const el = document.createElement('div');
    el.className = 'product';
    el.innerHTML = `
      <div class="meta">
        <div class="title">${escapeHtml(p.Name)}</div>
        <div class="cat">${escapeHtml(p.Category)}</div>
      </div>
      <div class="actions">
        <div style="margin-right:12px; font-weight:700">${money(p.Price)}</div>
        <button class="btn primary" data-id="${p.Id}">Add</button>
      </div>
    `;
    productsEl.appendChild(el);
  });

  productsEl.querySelectorAll('button').forEach(btn=>{
    btn.onclick = () => {
      const id = Number(btn.getAttribute('data-id'));
      const p = products.find(x=> x.Id === id);
      if(p) addToCart(p);
    };
  });
}

// ----------------- SEARCH -----------------
searchInput.addEventListener('input', (e)=>{
  const q = e.target.value.trim().toLowerCase();
  displayedProducts = products.filter(p =>
    p.Name.toLowerCase().includes(q) || p.Category.toLowerCase().includes(q)
  );
  renderProducts(displayedProducts);
});

// ----------------- NETWORK: API integration -----------------

async function fetchProductsFromApi(){
  try{
    const res = await fetch('/api/products', { cache: "no-store" });
    if(!res.ok) throw new Error(`Failed to fetch products: ${res.status}`);
    const data = await res.json();
    return data;
  }catch(err){
    console.warn('Falling back to local products.json fetch or local error:', err);
    // fallback: try relative file (works when served as static)
    try{
      const r2 = await fetch('./products.json', { cache: "no-store" });
      if(r2.ok) return await r2.json();
    }catch(e){ /* ignore */ }
    throw err;
  }
}

// Save cart to backend (POST /api/cart). Returns server response or throws.
async function saveCartToServer(){
  try{
    const res = await fetch('/api/cart', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(cart)
    });
    if(!res.ok) {
      const txt = await res.text().catch(()=>null);
      throw new Error(txt || `Server responded ${res.status}`);
    }
    return await res.text().catch(()=>null);
  }catch(err){
    throw err;
  }
}

// Send receipt to backend (POST /api/receipt). Returns server result or throws.
async function sendReceiptToServer(receipt){
  try{
    const res = await fetch('/api/receipt', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(receipt)
    });
    if(!res.ok) {
      const txt = await res.text().catch(()=>null);
      throw new Error(txt || `Server responded ${res.status}`);
    }
    return await res.text().catch(()=>null);
  }catch(err){
    throw err;
  }
}

// ----------------- ACTIONS: Checkout & Save -----------------
checkoutBtn.addEventListener('click', async ()=>{
  const total = calculateTotal();
  if(total === 0){
    alert('Cart is empty.');
    return;
  }

  const receipt = {
    date: (new Date()).toISOString(),
    items: cart.map(i => ({ id: i.product.Id, name: i.product.Name, price: i.product.Price, qty: i.quantity })),
    total: total
  };

  // Try save on server first, fallback to local download
  try{
    await sendReceiptToServer(receipt);
    // also offer download as local copy
    downloadJson(receipt, `receipt-${Date.now()}.json`);
    cart = [];
    saveCartToStorage();
    renderCart();
    alert('Receipt saved to server and downloaded.');
  }catch(err){
    console.warn('Failed to save receipt to server:', err);
    // fallback to local download only
    const doDownload = confirm('Saving to server failed. Download receipt locally instead?');
    if(doDownload){
      downloadJson(receipt, `receipt-${Date.now()}.json`);
      cart = [];
      saveCartToStorage();
      renderCart();
      alert('Receipt downloaded locally.');
    } else {
      alert('Receipt not saved.');
    }
  }
});

saveCartBtn.addEventListener('click', async ()=>{
  if(cart.length === 0){
    alert('Cart is empty.');
    return;
  }
  // Try saving to server; on failure offer local download
  try{
    await saveCartToServer();
    alert('Cart saved to server.');
  }catch(err){
    console.warn('Failed to save cart to server:', err);
    const doDownload = confirm('Saving to server failed. Download cart JSON locally instead?');
    if(doDownload){
      downloadJson(cart, `cart-${Date.now()}.json`);
      alert('Cart downloaded locally.');
    }
  }
});

// ----------------- UTIL: download JSON -----------------
function downloadJson(obj, filename){
  const blob = new Blob([JSON.stringify(obj, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url; a.download = filename;
  document.body.appendChild(a); a.click(); a.remove();
  URL.revokeObjectURL(url);
}

// simple HTML escaper for safety
function escapeHtml(s){
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

// ----------------- INIT -----------------
async function init(){
  loadCartFromStorage();

  try{
    const data = await fetchProductsFromApi();
    // ensure products are in expected shape (Id, Name, Price, Category)
    products = Array.isArray(data) ? data : [];
    displayedProducts = products.slice().sort((a,b)=> a.Id - b.Id);
    renderProducts(displayedProducts);
  }catch(err){
    console.error('Failed to load products:', err);
    productsEl.innerHTML = `<div style="color:#ef4444">Error loading products — check server or products.json</div>`;
  }

  renderCart();
}

init();
