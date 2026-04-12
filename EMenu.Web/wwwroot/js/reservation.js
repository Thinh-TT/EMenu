(function () {
    const forms = document.querySelectorAll(".js-reservation-form");

    if (!forms.length) {
        return;
    }

    forms.forEach(form => {
        const tableInput = form.querySelector("[name='tableId'], [name='TableId']");
        const reservationTimeInput = form.querySelector("[name='reservationTime'], [name='ReservationTime']");
        const feedback = form.querySelector(".js-conflict-feedback");
        const submitButton = form.querySelector("button[type='submit']");

        if (!tableInput || !reservationTimeInput || !feedback || !submitButton) {
            return;
        }

        let requestId = 0;

        const checkConflict = async () => {
            const tableId = tableInput.value;
            const reservationTime = reservationTimeInput.value;

            if (!tableId || !reservationTime) {
                feedback.textContent = "";
                submitButton.disabled = false;
                return;
            }

            requestId += 1;
            const currentRequestId = requestId;

            try {
                const response = await fetch(
                    `/api/reservation/check-conflict?tableId=${encodeURIComponent(tableId)}&reservationTime=${encodeURIComponent(reservationTime)}`
                );

                if (currentRequestId !== requestId) {
                    return;
                }

                if (!response.ok) {
                    const message = await response.text();
                    feedback.textContent = message || "Unable to validate reservation time.";
                    submitButton.disabled = false;
                    return;
                }

                const result = await response.json();

                if (result.hasConflict) {
                    feedback.textContent = "Selected table already has a reservation at this time.";
                    submitButton.disabled = true;
                    return;
                }

                feedback.textContent = "";
                submitButton.disabled = false;
            } catch {
                if (currentRequestId !== requestId) {
                    return;
                }

                feedback.textContent = "Unable to validate reservation time right now.";
                submitButton.disabled = false;
            }
        };

        tableInput.addEventListener("change", checkConflict);
        reservationTimeInput.addEventListener("change", checkConflict);
        reservationTimeInput.addEventListener("input", checkConflict);

        checkConflict();
    });
})();
