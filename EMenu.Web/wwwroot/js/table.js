function openTable(tableId) {

    fetch(`/api/session/start?tableId=${tableId}&customerId=1`, {

        method: "POST"

    })
        .then(async res => {
            if (!res.ok) {
                const message = await res.text();
                throw new Error(message || "Unable to start session");
            }

            return res.json();
        })
        .then(data => {

            alert("Session started");

            window.location =
                `/Menu?tableId=${tableId}&sessionId=${data.orderSessionID}`;

        })
        .catch(err => {
            alert(err.message || "Unable to start session");
        });

}

function endTable(tableId) {

    fetch(`/api/session/end?tableId=${tableId}`, {
        method: "POST"
    })
        .then(async res => {
            if (!res.ok) {
                const message = await res.text();
                throw new Error(message || "Unable to end session");
            }

            alert("Session ended");

            location.reload();

        })
        .catch(err => {
            alert(err.message || "Unable to end session");
        });

}
