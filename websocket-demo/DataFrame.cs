using System;
using System.Text;
using websocket_demo;

// Websocket DataFrame
static class DataFrame
{
    /*
     * See the following for good examples of what I'm doing here
     * https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers#exchanging_data_frames
     * https://stackoverflow.com/a/8125509
     * 
     */
    public static byte[] GeneratePayload(string dataToSend)
    {
        byte[] payloadBytes = Encoding.UTF8.GetBytes(dataToSend);

        int messageLength = payloadBytes.Length;
        byte[] headers = GenerateHeaders(Opcode.TEXT, messageLength);
        // Set FIN to 1
        // headers[0] = (byte) (headers[0] | 0b10000000);

        //Console.WriteLine(messageLength);
        //Console.WriteLine(headers[1]);

        //Console.WriteLine("Headers Print");
        //for (int i = 0; i < headers.Length; i++)
        //{
        //    Console.WriteLine(headers[i]);
        //}

        return combineArrays(headers, payloadBytes);
    }

    public static byte[] GenerateCloseFrame()
    {
        return GenerateHeaders(Opcode.CLOSE, 0);
    }

    private static byte[] GenerateHeaders(Opcode opcode, int messageLength)
    {
        byte[] headers = new byte[2];
        headers[0] = (byte)opcode;

        if (messageLength < 126)
        {
            headers[1] = (byte)((byte)messageLength & 0b01111111); // Mask is zero; set to payload length directly
        }
        else if (messageLength > 125 && messageLength < 65536)
        {
            headers[1] = 0b01111110; // Mask is zero, length is seven bits; set to 126
            byte[] payloadLength = BitConverter.GetBytes(Convert.ToUInt16(messageLength));
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadLength);
            }

            headers = combineArrays(headers, payloadLength);
        }
        else
        {
            headers[1] = 0b01111111; // Mask is zero, length is seven bits; set to 127
            byte[] payloadLength = BitConverter.GetBytes(Convert.ToUInt64(messageLength));
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadLength);
            }

            headers = combineArrays(headers, payloadLength);
        }

        return headers;
    }

    private static byte[] combineArrays(byte[] array1, byte[] array2)
    {
        byte[] combinedArray = new byte[array1.Length + array2.Length];
        for (int i = 0; i < array1.Length; i++)
        {
            combinedArray[i] = array1[i];
        }

        for (int i = 0; i < array2.Length; i++)
        {
            combinedArray[i + array1.Length] = array2[i];
        }

        return combinedArray;
    }
}