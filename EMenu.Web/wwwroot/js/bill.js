let tableId = new URLSearchParams(window.location.search).get("tableId");

function loadBill() {

    fetch(`/api/bill/table?tableId=${tableId}`)
        .then(res => res.json())
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

        });

}

function checkout() {

    fetch(`/api/bill/checkout?tableId=${tableId}`, {
        method: "POST"
    })
        .then(res => res.text())
        .then(data => {

            alert("Checkout complete");

            window.location = "/Table";

        });

}

loadBill();