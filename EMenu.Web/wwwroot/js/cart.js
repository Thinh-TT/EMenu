const customerMenuData = window.customerMenuData || {};
const menuI18n = customerMenuData.i18n || {};

function t(key, fallback) {
  return menuI18n[key] || fallback;
}

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
    t(
      "sessionNotFound",
      "Session not found! Please start a new order. By scanning the QR code at the table.",
    ),
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
    alert(t("cartIsEmpty", "Cart is empty"));
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

      alert(t("orderPlaced", "Order placed!"));

      sessionStorage.removeItem("cart");
      closeCartDrawer();
      updateCartUI();

      window.location.href = "/OrderPage/Tracking?sessionId=" + sessionId;
    })
    .catch((err) => {
      console.error(err);
      alert(err.message || t("unableToPlaceOrder", "Unable to place order"));
    });
}

function updateCartUI() {
  let cart = getCart();
  let container = document.getElementById("cartItems");

  if (!container) return;

  container.innerHTML = "";

  let total = 0;
  let totalItems = 0;

  cart.forEach((item, index) => {
    totalItems += item.quantity;
    total += item.price * item.quantity;

    const itemElement = document.createElement("div");
    itemElement.className = "cart-item";

    const copyElement = document.createElement("div");
    copyElement.className = "cart-item__copy";

    const nameElement = document.createElement("b");
    nameElement.textContent = item.name;
    copyElement.appendChild(nameElement);

    const priceElement = document.createElement("span");
    priceElement.className = "cart-item__price";
    priceElement.textContent = window.emenu.formatCurrency(
      item.price * item.quantity,
    );
    copyElement.appendChild(priceElement);

    const controlsElement = document.createElement("div");
    controlsElement.className = "cart-controls";

    const decreaseButton = createCartActionButton(
      "-",
      t("decreaseQuantity", "Decrease quantity"),
      () => decreaseQty(index),
    );
    controlsElement.appendChild(decreaseButton);

    const quantityElement = document.createElement("span");
    quantityElement.textContent = item.quantity;
    controlsElement.appendChild(quantityElement);

    const increaseButton = createCartActionButton(
      "+",
      t("increaseQuantity", "Increase quantity"),
      () => increaseQty(index),
    );
    controlsElement.appendChild(increaseButton);

    const removeButton = createCartActionButton(
      "x",
      t("removeItem", "Remove item"),
      () => removeItem(index),
    );
    removeButton.classList.add("remove-btn");
    controlsElement.appendChild(removeButton);

    itemElement.appendChild(copyElement);
    itemElement.appendChild(controlsElement);
    container.appendChild(itemElement);
  });

  if (cart.length === 0) {
    const emptyState = document.createElement("div");
    emptyState.className = "cart-empty-state";

    const title = document.createElement("strong");
    title.textContent = t("emptyCartTitle", "Your cart is empty.");
    emptyState.appendChild(title);

    const hint = document.createElement("p");
    hint.textContent = t(
      "emptyCartHint",
      "Add a few dishes to review them here before sending your order to the kitchen.",
    );
    emptyState.appendChild(hint);

    container.appendChild(emptyState);
  }

  document.getElementById("cartTotal").innerText =
    window.emenu.formatCurrency(total);

  const cartBadge = document.getElementById("cartBadge");

  if (cartBadge) {
    cartBadge.hidden = totalItems <= 0;
    cartBadge.textContent = totalItems;
  }

  const countLabel = document.getElementById("cartCountLabel");

  if (countLabel) {
    countLabel.textContent = t("itemCount", "{0} item(s)").replace(
      "{0}",
      totalItems,
    );
  }
}

function createCartActionButton(text, ariaLabel, onClick) {
  const button = document.createElement("button");
  button.type = "button";
  button.textContent = text;
  button.setAttribute("aria-label", ariaLabel);
  button.addEventListener("click", onClick);
  return button;
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

function setCartDrawerState(isOpen) {
  const drawer = document.getElementById("cartDrawer");
  const backdrop = document.getElementById("cartBackdrop");
  const fab = document.getElementById("cartFab");

  if (!drawer || !backdrop || !fab) {
    return;
  }

  drawer.classList.toggle("is-open", isOpen);
  drawer.setAttribute("aria-hidden", (!isOpen).toString());

  backdrop.hidden = !isOpen;
  fab.setAttribute("aria-expanded", isOpen.toString());

  document.body.classList.toggle("cart-drawer-open", isOpen);
}

function closeCartDrawer() {
  setCartDrawerState(false);
}

function openCartDrawer() {
  setCartDrawerState(true);
}

function toggleCartDrawer() {
  const drawer = document.getElementById("cartDrawer");
  const isOpen = drawer?.classList.contains("is-open");

  setCartDrawerState(!isOpen);
}

window.closeCartDrawer = closeCartDrawer;
window.openCartDrawer = openCartDrawer;
window.toggleCartDrawer = toggleCartDrawer;

document.addEventListener("DOMContentLoaded", function () {
  updateCartUI();
});

document.addEventListener("keydown", function (event) {
  if (event.key === "Escape") {
    closeCartDrawer();
  }
});
