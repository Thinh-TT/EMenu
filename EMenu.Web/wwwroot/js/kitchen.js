loadOrders();

function loadOrders() {

    fetch("/api/kitchen/pending")
        .then(res => res.json())
        .then(data => renderOrders(data));

}

function renderOrders(items) {

    let container = document.getElementById("ordersContainer");

    container.innerHTML = "";

    let orders = {};
    //setInterval(loadStatus, 3000);
    // group theo orderID
    items.forEach(item => {

        if (!orders[item.orderID]) {
            orders[item.orderID] = [];
        }

        orders[item.orderID].push(item);

    });

    for (let orderID in orders) {

        let orderItems = orders[orderID];

        let html = `
<div class="order-card">

<h4>Order #${orderID}</h4>
<hr>
`;
      
        orderItems.forEach(i => {

            let buttonText = "";
            let nextStatus = 0;

            if (i.status == 0) {
                buttonText = "Preparing";
                nextStatus = 1;
            }

            else if (i.status == 1) {
                buttonText = "Ready";
                nextStatus = 2;
            }

            else if (i.status == 2) {
                buttonText = "Served";
                nextStatus = 3;
            }

            else {
                buttonText = "Done";
            }
            let statusClass = "";
            

            if (i.status == 0) statusClass = "pending";
            if (i.status == 1) statusClass = "preparing";
            if (i.status == 2) statusClass = "ready";

            html += `
<div class="order-item">

<span>${i.productName} x${i.quantity}</span>

<button class="status-btn ${statusClass}"
onclick="updateStatus(${i.orderProductID},${nextStatus})">

${buttonText}

</button>

</div>
`;

        });

        html += `</div>`;

        container.innerHTML += html;

    }

}

function updateStatus(orderProductID, status) {

    console.log("clicked", orderProductID, status);

    fetch("/api/kitchen/update-status?orderProductId=" + orderProductID + "&status=" + status, {
        method: "PUT"
    })
        .then(() => loadOrders());

}

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/orderHub")
    .build();

connection.on("OrderStatusUpdated", function () {

    console.log("SignalR update received");

    loadOrders();

});
connection.start()
    .then(() => console.log("SignalR connected"))
    .catch(err => console.error(err));