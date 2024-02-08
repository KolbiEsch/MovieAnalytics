const inputFile = document.querySelector(".file");
const fileInfo = document.querySelector(".file-info");
const labelFile = document.querySelector(".label");

if (inputFile) {
    inputFile.addEventListener("change", function (e) {
        if (e.target.files.length == 0) {
            fileInfo.innerHTML = "No file chosen.";
            return;
        }
        let fileName = e.target.files[0].name;
        fileInfo.innerHTML = fileName;

    });

    inputFile.addEventListener("focus", () => {
        labelFile.classList.toggle("label--focused");
    });

    inputFile.addEventListener("focusout", () => {
        labelFile.classList.toggle("label--focused");
    });
}