function SaveData(key, data) {
    localStorage.setItem(key, data);
}

function GetData(key) {
    return localStorage.getItem(key);
}

function RemoveData(key) {
    localStorage.removeItem(key);
}

function VideoMute() {


    try {
        document.getElementById("myVideo").muted = true;
    }
    catch { }
}

function PlaySound(keyname) {

    document.getElementById(keyname).play();
}
function StopSound(keyname) {
    document.getElementById(keyname).stop();
}

function IsTextToSpeech(texto) {
    if ('speechSynthesis' in window) {
        try {
            var msg = new SpeechSynthesisUtterance();
            msg.text = texto;
            msg.volume = 1; // From 0 to 1
            msg.rate = 1; // From 0.1 to 10
            msg.pitch = 1;
            msg.lang = 'es-ES';
            window.speechSynthesis.speak(msg);
            return true;
        } catch { }
    }
    return false;
}