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

loadRevenue();
loadTopProducts();