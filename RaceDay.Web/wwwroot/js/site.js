(function () {
    "use strict";

    // Confirm destructive actions (data-confirm on a form).
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (form && form.dataset && form.dataset.confirm) {
            if (!window.confirm(form.dataset.confirm)) {
                e.preventDefault();
            }
        }
    });

    // Event pickers submit on change so the page follows the selection.
    document.querySelectorAll("select[data-autosubmit]").forEach(function (select) {
        select.addEventListener("change", function () {
            if (select.form) select.form.submit();
        });
    });

    // Image inputs: check type and size before the upload leaves the browser.
    document.querySelectorAll('input[type="file"][data-max-bytes]').forEach(function (input) {
        input.addEventListener("change", function () {
            var file = input.files && input.files[0];
            input.setCustomValidity("");
            var old = input.parentElement.querySelector(".js-file-error");
            if (old) old.remove();
            if (!file) return;

            var allowed = ["image/jpeg", "image/png", "image/webp"];
            var max = parseInt(input.dataset.maxBytes, 10);
            var problem = "";
            if (allowed.indexOf(file.type) === -1) problem = "Upload a JPG, PNG or WebP image.";
            else if (file.size > max) problem = "The image is larger than 5 MB. Choose a smaller file.";

            if (problem) {
                input.value = "";
                var msg = document.createElement("span");
                msg.className = "field-error js-file-error";
                msg.setAttribute("role", "alert");
                msg.textContent = problem;
                input.insertAdjacentElement("afterend", msg);
            }
        });
    });

    // Category form: show the age fields or the distance field to match the chosen type.
    var typeSelect = document.querySelector("[data-category-type]");
    if (typeSelect) {
        var sync = function () {
            var isAge = typeSelect.value === "Age";
            document.querySelectorAll("[data-age-field]").forEach(function (el) { el.style.display = isAge ? "" : "none"; });
            document.querySelectorAll("[data-distance-field]").forEach(function (el) { el.style.display = isAge ? "none" : ""; });
        };
        typeSelect.addEventListener("change", sync);
        sync();
    }
})();
