(function () {
    const i18n = window.reservationI18n || {};

    function t(key, fallback) {
        return i18n[key] || fallback;
    }

    const form = document.querySelector(".js-reservation-form");
    if (!form) return;

    const checkBtn = document.getElementById("checkAvailabilityBtn");
    const submitBtn = document.getElementById("submitReservationBtn");
    const feedback = document.getElementById("availabilityFeedback");
    const pickerSection = document.getElementById("tablePickerSection");
    const pickerContainer = document.getElementById("tablePickerContainer");
    const selectedTableId = document.getElementById("selectedTableId");
    const selectedTableLabel = document.getElementById("selectedTableLabel");

    const nameInput = document.getElementById("CustomerName");
    const timeInput = document.getElementById("ReservationTime");
    const guestsInput = document.getElementById("NumberOfGuests");

    let availableTables = [];
    let currentSelection = null;

    // ---- Check Availability ----

    checkBtn.addEventListener("click", async () => {
        const customerName = nameInput.value.trim();
        const reservationTime = timeInput.value;
        const numberOfGuests = parseInt(guestsInput.value, 10);

        if (!customerName || !reservationTime) {
            feedback.textContent = t("pleaseEnterNameAndTime",
                "Please enter your name and reservation time.");
            return;
        }

        if (!numberOfGuests || numberOfGuests < 1) {
            feedback.textContent = t("pleaseEnterNameAndTime",
                "Please enter a valid number of guests.");
            return;
        }

        feedback.textContent = "";
        checkBtn.disabled = true;
        checkBtn.textContent = "...";

        try {
            const response = await fetch(
                `/api/reservation/available-tables?reservationTime=${encodeURIComponent(reservationTime)}&numberOfGuests=${encodeURIComponent(numberOfGuests)}`
            );

            if (!response.ok) {
                const msg = await response.text();
                feedback.textContent = msg || t("unknownError", "An error occurred while checking availability.");
                return;
            }

            availableTables = await response.json();
            renderTablePicker(availableTables);
            pickerSection.style.display = "block";
            pickerSection.scrollIntoView({ behavior: "smooth" });
            currentSelection = null;
            updateSubmitState();
        } catch (err) {
            feedback.textContent = t("unknownError", "An error occurred while checking availability.");
        } finally {
            checkBtn.disabled = false;
            checkBtn.textContent = t("checkAvailableTables", "Check Available Tables");
        }
    });

    // ---- Render Table Diagram ----

    function renderTablePicker(tables) {
        // Group by area
        const groups = new Map();
        tables.forEach(table => {
            const area = table.area || "Khác";
            if (!groups.has(area)) groups.set(area, []);
            groups.get(area).push(table);
        });

        if (tables.length === 0) {
            pickerContainer.innerHTML =
                `<div class="alert alert-light border">${t("noTablesAvailable", "No tables available for the selected time and guest count.")}</div>`;
            return;
        }

        let html = "";

        groups.forEach((areaTables, areaName) => {
            html += `
            <section class="table-area-section">
                <header class="table-area-header">
                    <h3 class="table-area-title">${escapeHtml(areaName)}</h3>
                    <span class="table-area-count">${areaTables.length} ${t("tables", "tables")}</span>
                </header>
                <div class="table-picker-grid">`;

            areaTables.forEach(table => {
                const statusClass = getStatusClass(table.currentStatus);
                const statusText = getStatusText(table.currentStatus);

                let cardClass = "table-picker-card";
                let clickAttr = "";
                let reasonHtml = "";

                if (table.isAvailable) {
                    cardClass += " table-picker-card--available";
                    clickAttr = `onclick="selectPickerTable(${table.tableId}, '${escapeHtml(table.tableName)}')"`;
                } else {
                    cardClass += " table-picker-card--unavailable";
                    reasonHtml = table.reason
                        ? `<div class="table-picker-card__reason">${escapeHtml(table.reason)}</div>`
                        : "";
                }

                html += `
                <div class="${cardClass}"
                     data-table-id="${table.tableId}"
                     ${clickAttr}>
                    <span class="status-pill ${statusClass}">${statusText}</span>
                    <h4>${escapeHtml(table.tableName)}</h4>
                    <p class="table-picker-card__capacity">${t("capacity", "Capacity")}: ${table.capacity}</p>
                    ${reasonHtml}
                    <div class="table-picker-card__radio">
                        <span class="table-picker-radio ${table.isAvailable ? "" : "table-picker-radio--disabled"}"></span>
                    </div>
                </div>`;
            });

            html += `
                </div>
            </section>`;
        });

        pickerContainer.innerHTML = html;
    }

    // ---- Table Selection ----

    window.selectPickerTable = function (tableId, tableName) {
        currentSelection = { tableId, tableName };

        // Update card states
        document.querySelectorAll(".table-picker-card").forEach(card => {
            const id = parseInt(card.getAttribute("data-table-id"), 10);
            card.classList.toggle("table-picker-card--selected", id === tableId);
        });

        updateSubmitState();
    };

    function updateSubmitState() {
        if (currentSelection) {
            selectedTableId.value = currentSelection.tableId;
            selectedTableLabel.textContent =
                `${t("table", "Table")}: ${currentSelection.tableName}`;
            submitBtn.disabled = false;
        } else {
            selectedTableId.value = "";
            selectedTableLabel.textContent =
                t("selectATable", "Select a table");
            submitBtn.disabled = true;
        }
    }

    // ---- Submit ----

    submitBtn.addEventListener("click", () => {
        if (!currentSelection) {
            feedback.textContent = t("selectATable", "Select a table");
            return;
        }

        if (!nameInput.value.trim() || !timeInput.value || !guestsInput.value) {
            feedback.textContent = t("pleaseEnterNameAndTime",
                "Please fill in all required fields.");
            return;
        }

        // The hidden input has the selected tableId, form will submit normally
        form.submit();
    });

    // ---- Helpers ----

    function getStatusClass(status) {
        switch (status) {
            case 0: return "status-pill--free";
            case 1: return "status-pill--busy";
            case 2: return "status-pill--reserved";
            default: return "";
        }
    }

    function getStatusText(status) {
        switch (status) {
            case 0: return t("available", "Available");
            case 1: return t("busy", "Busy");
            case 2: return t("reserved", "Reserved");
            default: return "";
        }
    }

    function escapeHtml(str) {
        const div = document.createElement("div");
        div.textContent = str;
        return div.innerHTML;
    }
})();
