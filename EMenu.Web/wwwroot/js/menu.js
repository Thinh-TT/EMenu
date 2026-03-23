const params = new URLSearchParams(window.location.search);

const sessionId = params.get("sessionId");
const hasValidSessionInUrl =
  sessionId && Number.isInteger(Number(sessionId)) && Number(sessionId) > 0;
const SESSION_NOT_FOUND_MESSAGE =
  "Session not found! Please start a new order. By scanning the QR code at the table.";

if (hasValidSessionInUrl) {
  const previousSessionId = sessionStorage.getItem("sessionId");

  if (previousSessionId && previousSessionId !== sessionId) {
    sessionStorage.removeItem("cart");
  }

  sessionStorage.setItem("sessionId", sessionId);
} else {
  // No session in URL means customer entered menu outside QR/session flow.
  sessionStorage.removeItem("sessionId");
  sessionStorage.removeItem("cart");
}

function addToOrder(productId) {
  const sessionId = sessionStorage.getItem("sessionId");

  if (!sessionId) {
    alert(SESSION_NOT_FOUND_MESSAGE);
    return;
  }

  fetch(
    "/api/order/add-product?sessionId=" +
      sessionId +
      "&productId=" +
      productId +
      "&quantity=1",
    {
      method: "POST",
      headers: window.emenu.getAntiforgeryHeaders(),
    },
  )
    .then((res) => {
      if (!res.ok) {
        throw new Error("Unable to add product");
      }

      alert("Added to order");
    })
    .catch(() => {
      alert("Unable to add product");
    });
}

function showCombo(comboId) {
  fetch("/Combo/GetItems?comboId=" + comboId)
    .then((res) => res.json())
    .then((data) => {
      let list = document.getElementById("comboItems");

      list.innerHTML = "";

      data.forEach((item) => {
        let li = document.createElement("li");

        li.innerText =
          item.name + " - " + window.emenu.formatCurrency(item.price);

        list.appendChild(li);
      });

      document.getElementById("comboModal").style.display = "block";
    });
}

function closeCombo() {
  document.getElementById("comboModal").style.display = "none";
}
