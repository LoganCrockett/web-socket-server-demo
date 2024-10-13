let socket = new WebSocket("ws://localhost:80");
socket.onmessage = function (e) {
    console.log(e);
}
socket.onopen = function(e) {
    console.log("Sending")
    socket.send("MDN");
}