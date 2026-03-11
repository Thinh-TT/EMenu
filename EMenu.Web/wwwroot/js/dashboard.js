async function loadRevenue() {

    const response = await fetch("/api/dashboard/revenue");
    const revenue = await response.json();

    new Chart(document.getElementById("revenueChart"), {
        type: "bar",
        data: {
            labels: ["Today"],
            datasets: [{
                label: "Revenue",
                data: [revenue]
            }]
        }
    });
}

async function loadTopProducts() {

    const response = await fetch("/api/dashboard/top-products");
    const data = await response.json();

    const labels = data.map(x => x.product);
    const values = data.map(x => x.quantity);

    new Chart(document.getElementById("productChart"), {
        type: "pie",
        data: {
            labels: labels,
            datasets: [{
                data: values
            }]
        }
    });
}
async function loadOrdersToday() {

    const res = await fetch("/api/dashboard/orders-today");
    const data = await res.json();

    document.getElementById("ordersToday").innerText = data;

}

async function loadTablesInUse() {

    const res = await fetch("/api/dashboard/tables-in-use");
    const data = await res.json();

    document.getElementById("tablesInUse").innerText = data;

}

loadOrdersToday();
loadTablesInUse();
loadRevenue();
loadTopProducts();