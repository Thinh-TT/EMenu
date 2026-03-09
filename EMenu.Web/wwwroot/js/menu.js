const params = new URLSearchParams(window.location.search);

const sessionId = params.get("sessionId");

if (sessionId) {
    sessionStorage.setItem("sessionId", sessionId);
}

function addToOrder(productId) {

    fetch("/api/order/add-product?orderId=1&productId="
        + productId + "&quantity=1",
        {
            method: "POST"
        })
        .then(res => res.json())
        .then(data => {

            alert("Added to order")

        })
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