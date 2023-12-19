function SaveData(key, data) {
    localStorage.setItem(key, data);
}

function GetData(key) {


    return localStorage.getItem(key);
}

function RemoveData(key) {
    localStorage.removeItem(key);
}

window.downloadFile = function (content, fileName) {
    const blob = new Blob([content], { type: "text/plain" });
    const link = document.createElement("a");
    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};