function openTable(tableId) {

    fetch(`/api/session/start?tableId=${tableId}&customerId=1`, {

        method: "POST"

    })
        .then(res => res.json())
        .then(data => {

            alert("Session started");

            window.location = `/Menu?tableId=${tableId}`;

        });

}

function endTable(tableId) {

    fetch(`/api/session/end?tableId=${tableId}`, {
        method: "POST"
    })
        .then(res => res.text())
        .then(data => {

            alert("Session ended");

            location.reload();

        });

}