function getCart() {
  let cart = sessionStorage.getItem("cart");

  if (!cart) return [];

  return JSON.parse(cart);
}

function getSessionId() {
  return sessionStorage.getItem("sessionId");
}

function ensureValidSession() {
  const sessionId = Number(getSessionId());

  if (Number.isInteger(sessionId) && sessionId > 0) {
    return true;
  }

  alert(
    "Session not found! Please start a new order. By scanning the QR code at the table.",
  );
  return false;
}

function saveCart(cart) {
  sessionStorage.setItem("cart", JSON.stringify(cart));
}

function addToCart(productId, name, price) {
  if (!ensureValidSession()) {
    return;
  }

  let cart = getCart();
  let item = cart.find((x) => x.productId === productId);

  if (item) {
    item.quantity++;
  } else {
    cart.push({
      productId: productId,
      name: name,
      price: price,
      quantity: 1,
    });
  }

  saveCart(cart);
  updateCartUI();
}

function submitOrder() {
  let cart = getCart();
  let sessionId = sessionStorage.getItem("sessionId");

  if (cart.length === 0) {
    alert("Cart is empty");
    return;
  }

  if (!ensureValidSession()) {
    return;
  }

  fetch("/api/order/submit?sessionId=" + sessionId, {
    method: "POST",
    headers: window.emenu.getAntiforgeryHeaders({
      "Content-Type": "application/json",
    }),
    body: JSON.stringify(cart),
  })
    .then(async (res) => {
      if (!res.ok) {
        const message = await res.text();
        throw new Error(message || "Unable to place order");
      }

      alert("Order placed!");

      sessionStorage.removeItem("cart");

      window.location.href = "/OrderPage/Tracking?sessionId=" + sessionId;
    })
    .catch((err) => {
      console.error(err);
      alert(err.message || "Unable to place order");
    });
}

function updateCartUI() {
  let cart = getCart();
  let container = document.getElementById("cartItems");

  if (!container) return;

  container.innerHTML = "";

  let total = 0;

  cart.forEach((item, index) => {
    total += item.price * item.quantity;

    container.innerHTML += `
        <div class="cart-item">

            <b>${item.name}</b>

            <div class="cart-controls">

                <button onclick="decreaseQty(${index})">-</button>

                <span>${item.quantity}</span>

                <button onclick="increaseQty(${index})">+</button>

                <button onclick="removeItem(${index})"
                        class="remove-btn">
                    x
                </button>

            </div>

            <span>${window.emenu.formatCurrency(item.price * item.quantity)}</span>

        </div>
        `;
  });

  document.getElementById("cartTotal").innerText =
    window.emenu.formatCurrency(total);
}

function increaseQty(index) {
  let cart = getCart();
  cart[index].quantity++;
  saveCart(cart);
  updateCartUI();
}

function decreaseQty(index) {
  let cart = getCart();
  cart[index].quantity--;

  if (cart[index].quantity <= 0) {
    cart.splice(index, 1);
  }

  saveCart(cart);
  updateCartUI();
}

function removeItem(index) {
  let cart = getCart();
  cart.splice(index, 1);
  saveCart(cart);
  updateCartUI();
}

document.addEventListener("DOMContentLoaded", function () {
  updateCartUI();
});
