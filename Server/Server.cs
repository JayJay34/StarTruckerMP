using Riptide;
using StarTruckMP.Encoding;
using StarTruckMP.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarTruckMP.StarTruckServer
{
    public class StarTruckServer
    {
        public static Server server = new Server();
        public static Dictionary<ushort, playerInfo> playerList = new Dictionary<ushort, playerInfo>();

        public static void FixedUpdate()
        {
            server.Update();
        }

        public static void Update()
        {
            if (UnityEngine.Input.GetKeyDown(StarTruckMP.hostKey.Value) && !server.IsRunning && !StarTruckClient.StarTruckClient.client.IsConnected)
            {
                StarTruckMP.Log.LogInfo($"Server Starting");
                server.Start(7777, 4);
                StarTruckClient.StarTruckClient.ConnectToServer("127.0.0.1:7777");
                server.ClientConnected += Server_ClientConnected;
                server.ClientDisconnected += Server_ClientDisconnected;
                server.MessageReceived += Server_MessageReceived;
            }
        }

        public static void Server_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            playerInfo currentPlayer;
            bool foundPlayer;
            switch (e.MessageId)
            {
                case (ushort)messageType.chatMessage:
                    StarTruckMP.Log.LogInfo(e.Message.GetString().ToString());
                    break;

                case (ushort)messageType.movementUpdate:
                    foundPlayer = playerList.TryGetValue(e.FromConnection.Id, out currentPlayer);

                    if (foundPlayer)
                    {
                        e.Message.GetUShort();
                        float[] playerTrans = e.Message.GetFloats();

                        Vector3 playerPos;
                        playerPos.x = playerTrans[0];
                        playerPos.y = playerTrans[1];
                        playerPos.z = playerTrans[2];

                        Vector3 playerRot;
                        playerRot.x = playerTrans[3];
                        playerRot.y = playerTrans[4];
                        playerRot.z = playerTrans[5];

                        Vector3 playerVel;
                        playerVel.x = playerTrans[6];
                        playerVel.y = playerTrans[7];
                        playerVel.z = playerTrans[8];

                        Vector3 playerAngVel;
                        playerAngVel.x = playerTrans[9];
                        playerAngVel.y = playerTrans[10];
                        playerAngVel.z = playerTrans[11];

                        bool isTruck = e.Message.GetBool();
                        bool inSeat = e.Message.GetBool();

                        if (isTruck)
                        {
                            currentPlayer.truckTrans.Pos = playerPos;
                            currentPlayer.truckTrans.Rot = playerRot;
                            currentPlayer.truckTrans.Vel = playerVel;
                            currentPlayer.truckTrans.AngVel = playerAngVel;

                            if (inSeat)
                            {
                                currentPlayer.playerTrans.Pos = playerPos;
                                currentPlayer.playerTrans.Rot = playerRot;
                                currentPlayer.playerTrans.Vel = playerVel;
                                currentPlayer.playerTrans.AngVel = playerAngVel;
                            }
                        }
                        else
                        {
                            currentPlayer.playerTrans.Pos = playerPos;
                            currentPlayer.playerTrans.Rot = playerRot;
                            currentPlayer.playerTrans.Vel = playerVel;
                            currentPlayer.playerTrans.AngVel = playerAngVel;
                        }

                        playerList[e.FromConnection.Id] = currentPlayer;
                        server.SendToAll(Messages.createMovementMessage(e.FromConnection.Id, playerPos, playerRot, playerVel, playerAngVel, isTruck, inSeat));
                    }
                    break;

                case (ushort)messageType.updateSector:
                    foundPlayer = playerList.TryGetValue(e.FromConnection.Id, out currentPlayer);

                    if (foundPlayer)
                    {
                        e.Message.GetUShort();
                        string newScene = e.Message.GetString();
                        currentPlayer.sector = newScene;
                        playerList[e.FromConnection.Id] = currentPlayer;

                        Message message = Message.Create(MessageSendMode.Reliable, (ushort)messageType.updateSector);
                        message.AddUShort(e.FromConnection.Id);
                        message.AddString(newScene);
                        server.SendToAll(message);
                    }
                    break;

                case (ushort)messageType.updateLivery:
                    foundPlayer = playerList.TryGetValue(e.FromConnection.Id, out currentPlayer);

                    if (foundPlayer)
                    {
                        e.Message.GetUShort();
                        string itemId = e.Message.GetString();
                        playerList[e.FromConnection.Id] = currentPlayer;
                        Message message = Message.Create(MessageSendMode.Unreliable, (ushort)messageType.updateLivery);
                        message.AddUShort(e.FromConnection.Id);
                        message.AddString(itemId);
                        server.SendToAll(message);
                    }
                    break;
            }
        }

        public static void Server_ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Client Disconnected: {e.Reason.ToString()}");

            Message message = Message.Create(MessageSendMode.Reliable, (ushort)messageType.clientDisconnect);
            message.AddUShort(e.Client.Id);
            server.SendToAll(message);

            playerList.Remove(e.Client.Id);
        }

        public static void Server_ClientConnected(object sender, ServerConnectedEventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Client Connected");
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)messageType.clientJoin);
            message.AddUShorts(playerList.Keys.ToArray<ushort>());
            foreach (var p in playerList.Values)
            {
                message.AddFloat(p.truckTrans.Pos.x);
                message.AddFloat(p.truckTrans.Pos.y);
                message.AddFloat(p.truckTrans.Pos.z);
                message.AddFloat(p.truckTrans.Rot.x);
                message.AddFloat(p.truckTrans.Rot.y);
                message.AddFloat(p.truckTrans.Rot.z);
                message.AddString(p.sector);
            }
            server.Send(message, e.Client);

            playerInfo newPlayer = new playerInfo();
            playerList.Add(e.Client.Id, newPlayer);
        }

    }
}
