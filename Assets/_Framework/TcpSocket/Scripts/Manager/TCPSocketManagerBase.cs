using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using System.Net.Sockets;
using System.Net;
using System;
using Google.Protobuf;
using static GamePacket;
using Ironcow.WebSocketPacket;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.IO;

public abstract class TCPSocketManagerBase<T> : MonoSingleton<T> where T : TCPSocketManagerBase<T>
{
    public bool useDNS = false;
    public Dictionary<PayloadOneofCase, Action<GamePacket>> _onRecv = new Dictionary<PayloadOneofCase, Action<GamePacket>>();

    public Queue<Packet> sendQueue = new Queue<Packet>();
    public Queue<Packet> receiveQueue = new Queue<Packet>();

    public string ip = "127.0.0.1";
    public int port = 3000;

    public Socket socket;
    public string version = "1.0.0";
    public int sequenceNumber = 1;

    byte[] recvBuff = new byte[1024];
    private byte[] remainBuffer = Array.Empty<byte>();

    public bool isConnected;
    bool isInit = false;
    /// <summary>
    /// ���÷������� �ش� Ŭ������ �ִ� �޼ҵ带 Payload�� ���� �̺�Ʈ ���
    /// </summary>
    protected void InitPackets()
    {
        if (isInit) return;
        var payloads = Enum.GetNames(typeof(PayloadOneofCase));
        var methods = GetType().GetMethods();
        foreach (var payload in payloads)
        {
            var val = (PayloadOneofCase)Enum.Parse(typeof(PayloadOneofCase), payload);
            var method = GetType().GetMethod(payload);
            if (method != null)
            {
                var action = (Action<GamePacket>)Delegate.CreateDelegate(typeof(Action<GamePacket>), this, method);
                _onRecv.Add(val, action);
            }
        }
        isInit = true;
    }

    /// <summary>
    /// ip, port �ʱ�ȭ �� ��Ŷ ó�� �޼ҵ� ���
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public TCPSocketManagerBase<T> Init(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
        InitPackets();
        return this;
    }

    /// <summary>
    /// ��ϵ� ip, port�� ���� ����
    /// send, receiveť �̺�Ʈ ���
    /// </summary>
    /// <param name="callback"></param>
    public async void Connect(UnityAction callback = null)
    {
        IPEndPoint endPoint;
        if (IPAddress.TryParse(ip, out IPAddress ipAddress))
        {
            endPoint = new IPEndPoint(ipAddress, port);
        }
        else
        {
            endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        }
        if (useDNS)
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHost.AddressList[0];

            endPoint = new IPEndPoint(ipAddress, port);
        }
        Debug.Log("Tcp Ip : " + ipAddress.MapToIPv4().ToString() + ", Port : " + port);
        socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(endPoint);
            isConnected = socket.Connected;
            OnReceive();
            StartCoroutine(OnSendQueue());
            StartCoroutine(OnReceiveQueue());
            StartCoroutine(Ping());
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    /// <summary>
    /// ������ �����͸� �޴� �޼ҵ�. �޾Ƽ� receiveQueue�� ���
    /// </summary>
    private async void OnReceive()
    {
        if(socket != null)
        {
            while (socket.Connected && isConnected)
            {
                try
                {
                    var recvByteLength = await socket.ReceiveAsync(recvBuff, SocketFlags.None); //socket.ReceiveAsync�� await�� ��� �� ���ο� �����͸� �ޱ� ������ ����Ѵ�.
                    if (!isConnected)
                    {
                        Debug.Log("Socket is disconnect");
                        break;
                    }
                    if (recvByteLength <= 0)
                    {
                        continue;
                    }

                    var newBuffer = new byte[remainBuffer.Length + recvByteLength];
                    Array.Copy(remainBuffer, 0, newBuffer, 0, remainBuffer.Length);
                    Array.Copy(recvBuff, 0, newBuffer, remainBuffer.Length, recvByteLength);

                    var processedLength = 0;
                    while (processedLength < newBuffer.Length)
                    {
                        if (newBuffer.Length - processedLength < 11)
                        {
                            break;
                        }

                        using var stream = new MemoryStream(newBuffer, processedLength, newBuffer.Length - processedLength);
                        using var reader = new BinaryReader(stream);

                        var typeBytes = reader.ReadBytes(2);
                        Array.Reverse(typeBytes);

                        var type = (PayloadOneofCase)BitConverter.ToInt16(typeBytes);
                        Debug.Log($"PacketType:{type}");

                        var versionLength = reader.ReadByte();
                        if (newBuffer.Length - processedLength < 11 + versionLength)
                        {
                            break;
                        }
                        var versionBytes = reader.ReadBytes(versionLength);
                        var version = BitConverter.ToString(versionBytes);

                        var sequenceBytes = reader.ReadBytes(4);
                        Array.Reverse(sequenceBytes);
                        var sequence = BitConverter.ToInt32(sequenceBytes);

                        var payloadLengthBytes = reader.ReadBytes(4);
                        Array.Reverse(payloadLengthBytes);
                        var payloadLength = BitConverter.ToInt32(payloadLengthBytes);

                        if (newBuffer.Length - processedLength < 11 + versionLength + payloadLength)
                        {
                            break;
                        }
                        var payloadBytes = reader.ReadBytes(payloadLength);

                        var totalLength = 11 + versionLength + payloadLength;
                        var packet = new Packet(type, version, sequence, payloadBytes);
                        receiveQueue.Enqueue(packet);
                        Debug.Log($"Enqueued Type: {type}|{receiveQueue.Count}");

                        processedLength += totalLength;
                    }

                    var remainLength = newBuffer.Length - processedLength;
                    if (remainLength > 0)
                    {
                        remainBuffer = new byte[remainLength];
                        Array.Copy(newBuffer, processedLength, remainBuffer, 0, remainLength);
                        break;
                    }

                    remainBuffer = Array.Empty<byte>();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.StackTrace}");
                }
            }
            if(socket != null && socket.Connected)
            {
                Debug.Log("���� ���ú� ���� �ٽ� ����");
                OnReceive();
            }
        }
    }
    /// <summary>
    /// �ܺο��� ���Ͽ� �޽����� ������ ȣ��
    /// GamePacket ���·� �޾� Packet Ŭ������ ���� sendQueue�� ����Ѵ�.
    /// </summary>
    /// <param name="gamePacket"></param>
    public void Send(GamePacket gamePacket)
    {
        if (socket == null) return;
        var byteArray = gamePacket.ToByteArray();
        var packet = new Packet(gamePacket.PayloadCase, version, sequenceNumber++, byteArray);
        sendQueue.Enqueue(packet);
    }

    /// <summary>
    /// sendQueue�� �����Ͱ� ���� �� ���Ͽ� ����
    /// </summary>
    /// <returns></returns>
    IEnumerator OnSendQueue()
    {
        while (true)
        {
            yield return new WaitUntil(() => sendQueue.Count > 0);
            var packet = sendQueue.Dequeue();

            var bytes = packet.ToByteArray();
            var sent = socket.Send(bytes, SocketFlags.None);
            Debug.Log($"Send Packet: {packet.type}, Sent bytes: {sent}");

            yield return new WaitForSeconds(0.01f);
        }
    }

    /// <summary>
    /// receiveQueue�� �����Ͱ� ���� �� ��Ŷ Ÿ�Կ� ���� �̺�Ʈ ȣ��
    /// </summary>
    /// <returns></returns>
    IEnumerator OnReceiveQueue()
    {
        while (true)
        {
            yield return new WaitUntil(() => receiveQueue.Count > 0);
            try
            {
                var packet = receiveQueue.Dequeue();
                Debug.Log("Receive Packet : " + packet.type.ToString());
                _onRecv[packet.type].Invoke(packet.gamePacket);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    /// <summary>
    /// �ı��� (���� �ı� ���� �ʴ´ٸ� �� ���� ��) ���� ���� ����
    /// </summary>
    private void OnDestroy()
    {
        Disconnect();
    }

    /// <summary>
    /// ���� ���� ����
    /// </summary>
    /// <param name="isReconnect"></param>
    public async void Disconnect(bool isReconnect = false)
    {
        StopAllCoroutines();
        if (isConnected)
        {
            this.isConnected = false;
            GamePacket packet = new GamePacket();
            packet.LoginRequest = new C2SLoginRequest();
            Send(packet);
            socket.Disconnect(isReconnect);
            if (isReconnect)
            {
                Connect();
            }
            else
            {
                if (SceneManager.GetActiveScene().name != "Main")
                {
                    await SceneManager.LoadSceneAsync("Main");
                }
                else
                {
                    UIManager.Hide<UITopBar>();
                    UIManager.Hide<UIGnb>();
                    await UIManager.Show<PopupLogin>();
                }
            }
        }
    }
    public IEnumerator Ping()
    {
        while (SocketManager.instance.isConnected)
        {
            yield return new WaitForSeconds(5);
            GamePacket packet = new GamePacket();
            packet.LoginResponse = new S2CLoginResponse();
            //SocketManager.instance.Send(packet);
        }
    }
}