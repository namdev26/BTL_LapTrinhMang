using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

class UdpRelayServer
{
    static void Main()
    {
        UdpClient server = new UdpClient(9000);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        Dictionary<IPEndPoint, int> clientIds = new Dictionary<IPEndPoint, int>();
        int nextId = 1;

        Console.WriteLine("🚦 Server listening on port 9000...");

        while (true)
        {
            byte[] data = server.Receive(ref remoteEP);
            string msg = Encoding.UTF8.GetString(data);

            // Nếu client mới kết nối
            if (!clientIds.ContainsKey(remoteEP))
            {
                clientIds[remoteEP] = nextId++;
                Console.WriteLine($"✅ New client {remoteEP} assigned ID {clientIds[remoteEP]}");

                // Gửi INIT về client đó
                string initMsg = $"{clientIds[remoteEP]}:INIT";
                byte[] initData = Encoding.UTF8.GetBytes(initMsg);
                server.Send(initData, initData.Length, remoteEP);
            }

            int playerId = clientIds[remoteEP];

            // Nếu chỉ gửi HELLO thì bỏ qua
            if (msg == "HELLO") continue;

            // Gói tin: "id:x,z"
            string msgWithId = $"{playerId}:{msg}";
            byte[] sendData = Encoding.UTF8.GetBytes(msgWithId);

            // Broadcast cho tất cả client
            foreach (var client in clientIds.Keys)
            {
                server.Send(sendData, sendData.Length, client);
            }
        }
    }
}
