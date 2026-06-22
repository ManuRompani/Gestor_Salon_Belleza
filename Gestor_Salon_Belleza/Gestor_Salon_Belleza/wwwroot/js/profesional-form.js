document.addEventListener("DOMContentLoaded", function () {
    const profesionalForm = document.getElementById("profesionalForm");

    if (!profesionalForm) {
        return;
    }

    // Estos valores vienen desde los atributos data-* del form.
    // Razor los carga según el modo actual de la vista.
    const validarServicios = profesionalForm.dataset.validarServicios === "true";
    const usarRecorte = profesionalForm.dataset.usarRecorte === "true";

    if (validarServicios) {
        configurarValidacionServicios(profesionalForm);
    }

    if (usarRecorte) {
        configurarRecorteImagen();
    }
});

function configurarValidacionServicios(profesionalForm) {
    profesionalForm.addEventListener("submit", function (event) {
        const serviciosSeleccionados = document.querySelectorAll(
            'input[name="IdsServiciosSeleccionados"]:checked'
        );

        const error = document.getElementById("serviciosFrontError");

        // Si no seleccionó ningún servicio, frenamos el submit.
        if (serviciosSeleccionados.length === 0) {
            event.preventDefault();

            if (error) {
                error.textContent = "Debe seleccionar al menos un servicio.";
            }

            return;
        }

        // Si está todo bien, limpiamos el mensaje de error.
        if (error) {
            error.textContent = "";
        }
    });
}

function configurarRecorteImagen() {
    let cropper;

    const cropFileInput = document.getElementById("ImagenArchivo");
    const cropModal = document.getElementById("cropModal");
    const cropImage = document.getElementById("cropImage");
    const cropConfirmBtn = document.getElementById("cropConfirmBtn");
    const cropCancelBtn = document.getElementById("cropCancelBtn");

    if (!cropFileInput || !cropModal || !cropImage || !cropConfirmBtn || !cropCancelBtn) {
        return;
    }

    function actualizarPreview(url) {
        let preview = document.querySelector(".professional-current-image");
        let previewBox = document.querySelector(".professional-current-image-box");

        // Si no existía preview previa, la creamos dinámicamente.
        if (!preview) {
            previewBox = document.createElement("div");
            previewBox.className = "professional-current-image-box";

            preview = document.createElement("img");
            preview.className = "professional-current-image";
            preview.alt = "Imagen del profesional";

            previewBox.appendChild(preview);
            cropFileInput.parentNode.insertBefore(previewBox, cropFileInput);
        }

        preview.src = url;
        previewBox.style.display = "";
    }

    function cerrarModal() {
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }

        cropModal.style.display = "none";
    }

    cropFileInput.addEventListener("change", function () {
        const file = this.files?.[0];

        if (!file) {
            return;
        }

        const reader = new FileReader();

        reader.onload = function (e) {
            cropImage.src = e.target.result;
            cropModal.style.display = "flex";

            if (cropper) {
                cropper.destroy();
            }

            // Inicializa CropperJS sobre la imagen seleccionada.
            cropper = new Cropper(cropImage, {
                aspectRatio: 1,
                viewMode: 1,
                autoCropArea: 1,
                background: false
            });
        };

        reader.readAsDataURL(file);
    });

    cropConfirmBtn.addEventListener("click", function () {
        if (!cropper) {
            return;
        }

        const canvas = cropper.getCroppedCanvas({
            width: 400,
            height: 400
        });

        canvas.toBlob(function (blob) {
            const originalFile = cropFileInput.files[0];

            const croppedFile = new File(
                [blob],
                originalFile.name,
                { type: "image/jpeg" }
            );

            // Reemplaza el archivo original por el archivo recortado.
            const dt = new DataTransfer();
            dt.items.add(croppedFile);
            cropFileInput.files = dt.files;

            actualizarPreview(canvas.toDataURL("image/jpeg"));

            cerrarModal();
        }, "image/jpeg", 0.92);
    });

    cropCancelBtn.addEventListener("click", function () {
        cropFileInput.value = "";
        cerrarModal();
    });

    cropModal.addEventListener("click", function (e) {
        if (e.target === cropModal) {
            cropFileInput.value = "";
            cerrarModal();
        }
    });
}