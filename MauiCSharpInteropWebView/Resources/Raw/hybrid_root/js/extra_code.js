var msgCount = 0;

function CoolJSLibrary(message) {
    return "Your #" + msgCount++ + " JS message is: " + message;
}

function Log(message) {
    var logArea = document.getElementById('messageLog');
    logArea.value += '\r\n' + message;

    // Scroll to end
    logArea.selectionStart = logArea.textLength;
    logArea.scrollTop = logArea.scrollHeight;
}
