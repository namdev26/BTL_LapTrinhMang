using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CarNetwork : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject redCarPrefab;
    public GameObject yellowCarPrefab;

    [Header("Network")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 9000;

    private UdpClient udpClient;
    private IPEndPoint remoteEP;

    private int myId = -1;
    private Transform myCar;

    private Dictionary<int, Transform> opponentCars = new Dictionary<int, Transform>();

    void Start()
    {
        udpClient = new UdpClient();
        udpClient.Connect(serverIP, serverPort);
        remoteEP = new IPEndPoint(IPAddress.Any, 0);

        // Gửi HELLO để nhận ID từ server
        byte[] hello = Encoding.UTF8.GetBytes("HELLO");
        udpClient.Send(hello, hello.Length);

        Thread t = new Thread(ReceiveData);
        t.IsBackground = true;
        t.Start();
    }

    void Update()
    {
        if (myCar == null) return;

        // gửi vị trí lên server
        string msg = $"{myCar.position.x},{myCar.position.z},{myCar.rotation.eulerAngles.y}";
        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length);
    }

    void ReceiveData()
    {
        while (true)
        {
            byte[] data = udpClient.Receive(ref remoteEP);
            string msg = Encoding.UTF8.GetString(data);

            string[] split = msg.Split(':');
            if (split.Length != 2) continue;

            int playerId = int.Parse(split[0]);
            string payload = split[1];

            // INIT → server gán ID
            if (payload == "INIT")
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (myId == -1 && myCar == null)
                    {
                        myId = playerId;

                        GameObject prefab = (myId == 1) ? redCarPrefab : yellowCarPrefab;
                        myCar = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity).transform;

                        myCar.tag = "Player";
                        myCar.GetComponent<PrometeoCarController>().isLocalPlayer = true; // chỉ xe mình mới nhận input

                        // Gán camera follow
                        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                        if (cam != null) cam.SetTarget(myCar);

                        Debug.Log($"🚗 Spawned my car ID={myId}");
                    }
                });
                continue;
            }

            // Update vị trí & rotation
            string[] coords = payload.Split(',');
            if (coords.Length != 3) continue;

            float x = float.Parse(coords[0]);
            float z = float.Parse(coords[1]);
            float rotY = float.Parse(coords[2]);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (playerId != myId)
                {
                    if (!opponentCars.ContainsKey(playerId))
                    {
                        GameObject prefab = (playerId == 1) ? redCarPrefab : yellowCarPrefab;
                        Transform opp = Instantiate(prefab, new Vector3(x, 0, z), Quaternion.Euler(0, rotY, 0)).transform;

                        opponentCars[playerId] = opp;
                        opp.tag = "Opponent";

                        // tắt input cho Opponent
                        PrometeoCarController cc = opp.GetComponent<PrometeoCarController>();
                        if (cc != null) cc.isLocalPlayer = false;

                        Debug.Log($"👥 Spawned opponent {playerId}");
                    }

                    opponentCars[playerId].position = new Vector3(x, opponentCars[playerId].position.y, z);
                    opponentCars[playerId].rotation = Quaternion.Euler(0, rotY, 0);
                }
            });
        }
    }
}
