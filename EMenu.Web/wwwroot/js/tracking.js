(function () {
  const trackingData = window.trackingPageData || {};
  const i18n = trackingData.i18n || {};
  const sessionId = Number(trackingData.sessionId || 0);
  const initialCheckoutRequested = trackingData.checkoutRequested === true;

  function t(key, fallback) {
    return i18n[key] || fallback;
  }

  function getStatusLabel(status) {
    return (
      {
        0: t("pending", "Pending"),
        1: t("preparing", "Preparing"),
        2: t("ready", "Ready"),
        3: t("served", "Served"),
        4: t("cancelled", "Cancelled"),
      }[status] || ""
    );
  }

  function loadStatus() {
    fetch("/api/order/status?sessionId=" + sessionId)
      .then((res) => res.json())
      .then((data) => renderStatus(data))
      .catch(() => renderStatus([]));
  }

  function renderStatus(items) {
    const container = document.getElementById("orderStatus");

    if (!container) {
      return;
    }

    container.innerHTML = "";

    if (!Array.isArray(items) || items.length === 0) {
      const emptyState = document.createElement("div");
      emptyState.className = "tracking-empty-state";
      emptyState.textContent = t(
        "waitingForItems",
        "Your items will appear here after the kitchen receives them.",
      );
      container.appendChild(emptyState);
      return;
    }

    items.forEach((item) => {
      const row = document.createElement("div");
      row.className = "order-item tracking-order-item";

      const name = document.createElement("strong");
      name.textContent = `${item.productName} x${item.quantity}`;

      const status = document.createElement("span");
      status.className = "tracking-status-pill";
      status.textContent = getStatusLabel(item.status);

      row.appendChild(name);
      row.appendChild(status);
      container.appendChild(row);
    });
  }

  function setCheckoutButtonState(isRequested) {
    const button = document.getElementById("callCheckoutButton");
    const state = document.getElementById("checkoutCallState");

    if (!button || !state) {
      return;
    }

    button.disabled = isRequested;
    button.textContent = isRequested
      ? t("checkoutRequestedButton", "Checkout requested")
      : t("callCheckout", "Call checkout");

    state.textContent = isRequested
      ? t(
          "checkoutRequestedMessage",
          "The staff has been notified and will come to your table shortly.",
        )
      : t(
          "checkoutHint",
          "Tap here when you are ready for the staff to bring your bill.",
        );
  }

  function requestCheckout() {
    fetch("/api/order/call-checkout?sessionId=" + encodeURIComponent(sessionId), {
      method: "POST",
      headers: window.emenu.getAntiforgeryHeaders(),
    })
      .then(async (res) => {
        if (!res.ok) {
          const message = await res.text();
          throw new Error(
            message || t("unableToCallCheckout", "Unable to call checkout"),
          );
        }

        setCheckoutButtonState(true);
      })
      .catch((err) => {
        alert(
          err.message || t("unableToCallCheckout", "Unable to call checkout"),
        );
      });
  }

  const callCheckoutButton = document.getElementById("callCheckoutButton");

  if (callCheckoutButton) {
    callCheckoutButton.addEventListener("click", requestCheckout);
  }

  setCheckoutButtonState(initialCheckoutRequested);
  loadStatus();

  const connection = new signalR.HubConnectionBuilder().withUrl("/orderHub").build();

  connection.start().catch((err) => console.error(err));

  connection.on("OrderStatusUpdated", function () {
    loadStatus();
  });

  connection.on("CheckoutRequested", function (payload) {
    if (Number(payload?.sessionId || 0) === sessionId) {
      setCheckoutButtonState(true);
    }
  });

  connection.on("CheckoutRequestCleared", function (payload) {
    if (Number(payload?.sessionId || 0) === sessionId) {
      setCheckoutButtonState(false);
    }
  });
})();
