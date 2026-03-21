let tableId = new URLSearchParams(window.location.search).get("tableId");

function loadBill() {
    fetch(`/api/bill/table?tableId=${tableId}`)
        .then(async res => {
            if (!res.ok) {
                const message = await res.text();
                throw new Error(message || "Unable to load bill");
            }

            return res.json();
        })
        .then(data => {
            let container = document.getElementById("billItems");

            container.innerHTML = "";

            data.items.forEach(item => {
                const unitPrice = item.unitPrice ?? item.price ?? 0;
                const total = item.total ?? (unitPrice * (item.quantity ?? 0));

                container.innerHTML += `
                    <tr>
                    <td>${item.productName}</td>
                    <td>${item.quantity}</td>
                    <td>${window.emenu.formatCurrency(unitPrice)}</td>
                    <td>${window.emenu.formatCurrency(total)}</td>
                    </tr>
                    `;
            });

            document.getElementById("billTotal").innerText =
                window.emenu.formatCurrency(data.totalAmount ?? data.total ?? 0);
        })
        .catch(err => {
            alert(err.message || "Unable to load bill");
        });
}

function checkout() {
    fetch(`/api/bill/checkout?tableId=${tableId}`, {
        method: "POST",
        headers: window.emenu.getAntiforgeryHeaders()
    })
        .then(async res => {
            if (!res.ok) {
                const message = await res.text();
                throw new Error(message || "Unable to checkout");
            }

            return res.text();
        })
        .then(() => {
            alert("Checkout complete");
            window.location = "/Table";
        })
        .catch(err => {
            alert(err.message || "Unable to checkout");
        });
}

loadBill();
