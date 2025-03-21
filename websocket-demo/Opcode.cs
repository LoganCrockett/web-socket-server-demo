﻿namespace websocket_demo
{
    public enum Opcode : byte
    {
        TEXT = 0b00000001,
        CLOSE = 0b10001000, // Set FIN to 1 for close
        PING = 0b10001001, // Set FIN to 1 for Ping
        PONG = 0b10001010, // Set FIN to 1 for Pong
    }
}
