(function () {
    const TABLE_STATUS_AVAILABLE = 0;
    const TABLE_STATUS_OCCUPIED = 1;
    const TABLE_STATUS_RESERVED = 2;
    const tableData = window.tableManagementData || { tables: [], actor: "Anonymous" };
    const i18n = tableData.i18n || {};
    const checkoutRequests = new Map(
        (tableData.checkoutRequests || []).map(request => [
            Number.parseInt(request.tableId, 10),
            request
        ])
    );

    function t(key, fallback) {
        return i18n[key] || fallback;
    }

    applyCheckoutRequestState();

    window.openTable = function (tableId) {
        fetch(`/api/session/start?tableId=${tableId}&customerId=1`, {
            method: "POST",
            headers: window.emenu.getAntiforgeryHeaders()
        })
            .then(async res => {
                if (!res.ok) {
                    throw new Error(await readErrorMessage(res));
                }

                return res.json();
            })
            .then(data => {
                alert(t("sessionStarted", "Session started"));

                window.location =
                    `/Menu?tableId=${tableId}&sessionId=${data.orderSessionID}`;
            })
            .catch(err => {
                alert(err.message || t("unableToStartSession", "Unable to start session"));
            });
    };

    window.orderTable = function (tableId) {
        const activeSessions = tableData.activeSessions || [];
        const entry = activeSessions.find(s => s.tableId === tableId);

        if (!entry || !entry.sessionId) {
            alert(t("sessionNotFound", "No active session found for this table."));
            return;
        }

        window.location = `/Menu?tableId=${tableId}&sessionId=${entry.sessionId}`;
    };

    window.endTable = function (tableId) {
        fetch(`/api/session/end?tableId=${tableId}`, {
            method: "POST",
            headers: window.emenu.getAntiforgeryHeaders()
        })
            .then(async res => {
                if (!res.ok) {
                    throw new Error(await readErrorMessage(res));
                }

                alert(t("sessionEnded", "Session ended"));
                location.reload();
            })
            .catch(err => {
                alert(err.message || t("unableToEndSession", "Unable to end session"));
            });
    };

    window.openBill = function (tableId) {
        window.location = `/Table/Bill?tableId=${tableId}`;
    };

    window.openTransferModal = function (sourceTableId) {
        openTableActionModal("transfer", sourceTableId);
    };

    window.openMergeModal = function (sourceTableId) {
        openTableActionModal("merge", sourceTableId);
    };

    window.closeTableActionModal = function () {
        const modal = getModalElement();

        if (!modal) {
            return;
        }

        modal.style.display = "none";
        modal.setAttribute("aria-hidden", "true");
        resetActionForm();
    };

    window.confirmTableAction = function () {
        const actionType = getValue("tableActionType");
        const sourceTableId = Number.parseInt(getValue("sourceTableId"), 10);
        const targetTableId = Number.parseInt(getValue("targetTableSelect"), 10);

        if (!actionType || Number.isNaN(sourceTableId) || Number.isNaN(targetTableId)) {
            alert(t("chooseValidTargetTable", "Please choose a valid target table."));
            return;
        }

        const endpoint =
            actionType === "transfer"
                ? "/api/session/transfer"
                : "/api/session/merge";

        fetch(endpoint, {
            method: "POST",
            headers: window.emenu.getAntiforgeryHeaders({
                "Content-Type": "application/json"
            }),
            body: JSON.stringify({
                sourceTableId,
                targetTableId,
                actor: tableData.actor || "Anonymous"
            })
        })
            .then(async res => {
                if (!res.ok) {
                    throw new Error(await readErrorMessage(res));
                }

                return res.json();
            })
            .then(result => {
                const successTemplate = actionType === "transfer"
                    ? t("transferSuccessful", "Transfer successful. Moved orders: {0}")
                    : t("mergeSuccessful", "Merge successful. Moved orders: {0}");
                alert(successTemplate.replace("{0}", result.movedOrderCount));
                location.reload();
            })
            .catch(err => {
                alert(err.message || t("unableToCompleteTableAction", "Unable to complete table action"));
            });
    };

    document.addEventListener("keydown", event => {
        if (event.key !== "Escape") {
            return;
        }

        const modal = getModalElement();

        if (modal && modal.style.display === "block") {
            window.closeTableActionModal();
        }
    });

    const modal = getModalElement();

    if (modal) {
        modal.addEventListener("click", event => {
            if (event.target === modal) {
                window.closeTableActionModal();
            }
        });
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .build();

    connection.start()
        .catch(err => console.error(err));

    connection.on("CheckoutRequested", payload => {
        const tableId = Number.parseInt(payload?.tableId ?? payload?.tableID, 10);

        if (Number.isNaN(tableId)) {
            return;
        }

        checkoutRequests.set(tableId, payload);
        applyCheckoutRequestState();
    });

    connection.on("CheckoutRequestCleared", payload => {
        const tableId = Number.parseInt(payload?.tableId ?? payload?.tableID, 10);

        if (Number.isNaN(tableId)) {
            return;
        }

        checkoutRequests.delete(tableId);
        applyCheckoutRequestState();
    });

    function openTableActionModal(actionType, sourceTableId) {
        const sourceTable = tableData.tables.find(table => table.id === sourceTableId);

        if (!sourceTable) {
            alert(t("sourceTableNotFound", "Source table not found."));
            return;
        }

        const selectableTargets = tableData.tables.filter(table => {
            if (table.id === sourceTableId) {
                return false;
            }

            if (table.status === TABLE_STATUS_RESERVED) {
                return false;
            }

            if (actionType === "transfer") {
                return table.status === TABLE_STATUS_AVAILABLE;
            }

            return table.status === TABLE_STATUS_AVAILABLE ||
                table.status === TABLE_STATUS_OCCUPIED;
        });

        if (selectableTargets.length === 0) {
            alert(t("noValidTargetTable", "No valid target table for this action."));
            return;
        }

        setValue("tableActionType", actionType);
        setValue("sourceTableId", sourceTableId);
        renderTargets(selectableTargets);

        const actionText = actionType === "transfer"
            ? t("transfer", "Transfer")
            : t("merge", "Merge");
        const title = document.getElementById("tableActionTitle");
        const description = document.getElementById("tableActionDescription");

        if (title) {
            title.textContent = `${actionText} ${t("tableAction", "Table Action")}`;
        }

        if (description) {
            const template = actionType === "transfer"
                ? t("transferDescription", "Transfer from {0}. Select a valid target table below.")
                : t("mergeDescription", "Merge from {0}. Select a valid target table below.");
            description.textContent = template.replace("{0}", sourceTable.name);
        }

        const modalElement = getModalElement();

        if (modalElement) {
            modalElement.style.display = "block";
            modalElement.setAttribute("aria-hidden", "false");
        }
    }

    function renderTargets(tables) {
        const selectElement = document.getElementById("targetTableSelect");

        if (!selectElement) {
            return;
        }

        const options = tables
            .map(table => {
                const statusText = table.status === TABLE_STATUS_OCCUPIED
                    ? t("busy", "Busy")
                    : t("available", "Available");

                return `<option value="${table.id}">${table.name} (${statusText})</option>`;
            })
            .join("");

        selectElement.innerHTML = options;
    }

    function getModalElement() {
        return document.getElementById("tableActionModal");
    }

    function getValue(elementId) {
        const element = document.getElementById(elementId);

        if (!element) {
            return "";
        }

        return element.value || "";
    }

    function setValue(elementId, value) {
        const element = document.getElementById(elementId);

        if (!element) {
            return;
        }

        element.value = value;
    }

    function resetActionForm() {
        setValue("tableActionType", "");
        setValue("sourceTableId", "");

        const selectElement = document.getElementById("targetTableSelect");

        if (selectElement) {
            selectElement.innerHTML = "";
        }
    }

    function applyCheckoutRequestState() {
        document.querySelectorAll("[data-table-card]").forEach(card => {
            const tableId = Number.parseInt(card.getAttribute("data-table-id"), 10);

            if (Number.isNaN(tableId)) {
                return;
            }

            const hasCheckoutRequest = checkoutRequests.has(tableId);
            const requestElement = card.querySelector("[data-checkout-request]");
            const billButton = card.querySelector("[data-bill-button]");

            card.classList.toggle("table-box--checkout-requested", hasCheckoutRequest);

            if (requestElement) {
                requestElement.hidden = !hasCheckoutRequest;
            }

            if (billButton) {
                billButton.classList.toggle("table-bill-button--highlight", hasCheckoutRequest);
            }
        });
    }

    async function readErrorMessage(response) {
        const contentType = response.headers.get("content-type") || "";

        if (contentType.includes("application/json")) {
            try {
                const payload = await response.json();

                if (typeof payload === "string" && payload) {
                    return payload;
                }

                if (payload && payload.message) {
                    return payload.message;
                }

                if (payload && payload.title) {
                    return payload.title;
                }
            } catch {
            }
        }

        const text = await response.text();
        return text || t("requestFailed", "Request failed.");
    }
})();
