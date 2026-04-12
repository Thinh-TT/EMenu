const numberFormatter = new Intl.NumberFormat("vi-VN");
const currencyFormatter = new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
});

let importTrendChart;
let productChart;

function setText(id, value) {
    const element = document.getElementById(id);

    if (!element) {
        return;
    }

    element.innerText = value;
}

function formatCurrency(value) {
    return currencyFormatter.format(value ?? 0);
}

function formatNumber(value) {
    return numberFormatter.format(value ?? 0);
}

async function fetchJson(url) {
    const response = await fetch(url);

    if (!response.ok) {
        const message = await response.text();
        throw new Error(message || `Request failed: ${url}`);
    }

    return response.json();
}

async function loadSummary() {
    const summary = await fetchJson("/api/dashboard/summary");

    setText("revenueToday", formatCurrency(summary.todayRevenue));
    setText("ordersToday", formatNumber(summary.ordersToday));
    setText("tablesInUse", formatNumber(summary.tablesInUse));
    setText("lowStockCount", formatNumber(summary.lowStockCount));
    setText("importValueToday", formatCurrency(summary.importValueToday));
    setText("importValueMonth", formatCurrency(summary.importValueThisMonth));
    setText("reservationsToday", formatNumber(summary.reservationsToday));
    setText("staffHoursToday", `${formatNumber(summary.staffHoursToday)} h`);
}

async function loadImportTrend() {
    const trend = await fetchJson("/api/dashboard/import-trend?days=7");
    const labels = trend.map(item => item.label);
    const values = trend.map(item => item.totalValue);

    if (importTrendChart) {
        importTrendChart.destroy();
    }

    importTrendChart = new Chart(document.getElementById("importTrendChart"), {
        type: "line",
        data: {
            labels,
            datasets: [
                {
                    label: "Import Value",
                    data: values,
                    fill: true,
                    borderColor: "#1d4ed8",
                    backgroundColor: "rgba(29, 78, 216, 0.14)",
                    tension: 0.35,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }
            ]
        },
        options: {
            plugins: {
                tooltip: {
                    callbacks: {
                        label(context) {
                            return formatCurrency(context.parsed.y);
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback(value) {
                            return formatNumber(value);
                        }
                    }
                }
            }
        }
    });
}

async function loadTopProducts() {
    const products = await fetchJson("/api/dashboard/top-products");
    const labels = products.map(item => item.product);
    const values = products.map(item => item.quantity);

    if (productChart) {
        productChart.destroy();
    }

    productChart = new Chart(document.getElementById("productChart"), {
        type: "doughnut",
        data: {
            labels,
            datasets: [
                {
                    data: values,
                    backgroundColor: [
                        "#1d4ed8",
                        "#0284c7",
                        "#0ea5e9",
                        "#16a34a",
                        "#f59e0b"
                    ]
                }
            ]
        },
        options: {
            plugins: {
                legend: {
                    position: "bottom"
                }
            }
        }
    });
}

function renderList(listId, items, renderItem, emptyMessage) {
    const list = document.getElementById(listId);

    if (!list) {
        return;
    }

    list.innerHTML = "";

    if (!items || items.length === 0) {
        const empty = document.createElement("li");
        empty.className = "warning-list__item warning-list__item--empty";
        empty.innerText = emptyMessage;
        list.appendChild(empty);
        return;
    }

    items.forEach(item => {
        const li = document.createElement("li");
        li.className = "warning-list__item";
        li.innerHTML = renderItem(item);
        list.appendChild(li);
    });
}

async function loadAlerts() {
    const alerts = await fetchJson("/api/dashboard/alerts?lowStockLimit=5&clashLookaheadDays=7");

    renderList(
        "lowStockWarningList",
        alerts.lowStockWarnings,
        item => `<strong>${item.ingredientName}</strong><span>${formatNumber(item.stockQuantity)} ${item.unit} / Min ${formatNumber(item.minStock)} ${item.unit}</span>`,
        "No low-stock warning."
    );

    renderList(
        "reservationClashList",
        alerts.reservationClashes,
        item => `<strong>${item.tableName}</strong><span>${new Date(item.reservationTime).toLocaleString("vi-VN")} - ${item.conflictCount} reservations</span>`,
        "No reservation clash detected."
    );
}

async function initDashboard() {
    try {
        await Promise.all([
            loadSummary(),
            loadImportTrend(),
            loadTopProducts(),
            loadAlerts()
        ]);
    } catch (error) {
        console.error(error);
    }
}

initDashboard();
