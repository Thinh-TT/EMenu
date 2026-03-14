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
                container.innerHTML += `
                    <tr>
                    <td>${item.productName}</td>
                    <td>${item.quantity}</td>
                    <td>$${item.price}</td>
                    <td>$${item.total}</td>
                    </tr>
                    `;
            });

            document.getElementById("billTotal").innerText = data.total;
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
