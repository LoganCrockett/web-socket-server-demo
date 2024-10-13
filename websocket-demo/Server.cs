using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Text.RegularExpressions;

// Folows the demo at https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server
class Server {
    private const String SERVER_IP_ADDRESS = "127.0.0.1";
    private const int SERVER_PORT = 80;
    public static void Main(String[] args) {
        TcpListener server = new TcpListener(IPAddress.Parse(SERVER_IP_ADDRESS), SERVER_PORT);

        server.Start();
        Console.WriteLine("Server has started on {0}:{1}. {2} Waiting for a new connection", SERVER_IP_ADDRESS, SERVER_PORT, Environment.NewLine);

        while (true) {
           TcpClient client = server.AcceptTcpClient();
           ThreadPool.QueueUserWorkItem((i) => {
            SpawnClient(client);
           });
        }
    }

    private static void SpawnClient(TcpClient client) {
        Console.WriteLine("A Client connected");

        NetworkStream stream = client.GetStream();

        // enter infinite loop to be able to handle every change in stream  
        while (true) {
            while (!stream.DataAvailable);
            // while (client.Available < 3);
            
            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, bytes.Length);

            // Handshake protocol
            String data = Encoding.UTF8.GetString(bytes);
            if (Regex.IsMatch(data, "^GET /socket", RegexOptions.IgnoreCase)) {
                Console.WriteLine("Handshaking with client");
                const string eol = "\r\n";

                byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 SWITCHING PROTOCOLS"
                + eol
                + "Connection: Upgrade" + eol
                + "Upgrade: websocket" + eol
                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                    System.Security.Cryptography.SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(
                            new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                        )
                    )
                ) + eol + eol
                );

                stream.WriteAsync(response, 0, response.Length);
            }
            else {
                Console.WriteLine("Begin message decoding");
                // Decode the message
                bool fin = (bytes[0] & 0b10000000) != 0;
                bool mask = (bytes[1] & 0b10000000) != 0; // Must be true, all messages from the client have to have this bit set
                int opcode = bytes[0] & 0b00001111; // expecting 1 - text message
                ulong offset = 2;
                ulong msglen = bytes[1] & (ulong)0b01111111;

                if (msglen == 126) {
                    // bytes are reversed because websocket will print them in Big-Endian, whereas
                    // BitConverter will want them arranged in little-endian on windows
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                }
                else if (msglen == 127) {
                    // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                    // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                    // websocket frame available through client.Available).
                    msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] },0);
                    offset = 10;
                }

                if (msglen == 0) {
                    Console.WriteLine("Message Len is zero");
                }
                else if (mask) {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] {bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]};
                    offset += 4;

                    for (ulong i = 0; i < msglen; ++i) {
                        decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);
                    }

                    string text = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine("{0}", text);

                    // Begin writing response
                    string toSend = "Hello from Server here is something that is hopefully 126 characters longHello from Server here is something that is hopefully longer and I will continue to write";
                    byte[] dataFrame = DataFrame.GeneratePayload(toSend);
                    stream.WriteAsync(dataFrame, 0, dataFrame.Length);
                    Console.WriteLine("Wrote to client");
                }
                else {
                    Console.WriteLine("Mask Bit not set");
                }

                Console.WriteLine();
            }
        }
    }
}
