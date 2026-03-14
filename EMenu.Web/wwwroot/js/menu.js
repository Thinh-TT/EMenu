const params = new URLSearchParams(window.location.search);

const sessionId = params.get("sessionId");

if (sessionId) {
    const previousSessionId = sessionStorage.getItem("sessionId");

    if (previousSessionId && previousSessionId !== sessionId) {
        sessionStorage.removeItem("cart");
    }

    sessionStorage.setItem("sessionId", sessionId);
}

function addToOrder(productId) {
    const sessionId = sessionStorage.getItem("sessionId");

    if (!sessionId) {
        alert("Session not found");
        return;
    }

    fetch("/api/order/add-product?sessionId=" + sessionId + "&productId="
        + productId + "&quantity=1",
        {
            method: "POST"
        })
        .then(res => {
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
        .then(res => res.json())
        .then(data => {

            let list = document.getElementById("comboItems");

            list.innerHTML = "";

            data.forEach(item => {

                let li = document.createElement("li");

                li.innerText = item.name + " - $" + item.price;

                list.appendChild(li);

            });

            document.getElementById("comboModal")
                .style.display = "block";

        });

}

function closeCombo() {

    document.getElementById("comboModal")
        .style.display = "none";

}
