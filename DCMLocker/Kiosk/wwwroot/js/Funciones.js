
window.document.addEventListener("click", function () {
    document.getElementsByClassName("inputfocus")[0].focus();
});

function SaveData(key, data) {
    localStorage.setItem(key, data);
}

function GetData(key) {


    return localStorage.getItem(key);
}

function RemoveData(key) {
    localStorage.removeItem(key);
}

function SetFocusToElement(element)
{
   
    element.focus();
}