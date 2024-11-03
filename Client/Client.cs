using Riptide;
using System;
using UnityEngine;
using StarTruckMP.Utilities;
using StarTruckMP.Encoding;
using System.Collections.Generic;
using System.Linq;

namespace StarTruckMP.StarTruckClient
{
    public class StarTruckClient
    {
        public static Client client = new Client();
        public static Dictionary<ushort, playerInfo> playerList = new Dictionary<ushort, playerInfo>();
        public static string currentSector = "none";
        public static movementTrans playerTrans = new movementTrans();
        public static movementTrans truckTrans = new movementTrans();
        public static bool inTruck = true;
        public static GameObject myPlayer = null;
        public static Rigidbody myPlayerRigid = null;
        public static GameObject myTruck = null;
        public static Rigidbody myTruckRigid = null;
        public static GameObject playerCam = null;
        public static FloatingOriginManager floatingOrigin = null;
        public static PlayerLocation playerLocation = null;
        public static Vector3 lookRot = Vector3.zero;
        public static GameObject spaceSuitObj = null;
        public static Material[] spaceSuitMats = null;

        public static void FixedUpdate()
        {
            client.Update();
        }

        public static void Update()
        {
            if (UnityEngine.Input.GetKeyDown(StarTruckMP.joinKey.Value) && !StarTruckServer.StarTruckServer.server.IsRunning)
            {
                if (!client.IsConnected)
                {
                    StarTruckMP.Log.LogInfo($"Client Connecting");
                    ConnectToServer(StarTruckMP.IPAddress.Value);
                }
                else
                {
                    StarTruckMP.Log.LogInfo($"Client Disconnecting");
                    client.Disconnect();
                }
            }
        }
        public static void ConnectToServer(string IPAddress)
        {
            var connection = client.Connect(IPAddress, 5);
            client.Connected += Client_Connected;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.MessageReceived += Client_MessageReceived;
            client.ClientConnected += Client_ClientConnected;
            client.ClientDisconnected += Client_ClientDisconnected;
            client.Disconnected += Client_Disconnected;
            myPlayer = GameObject.FindGameObjectWithTag("Player");
            playerCam = GameObject.Find("Main Camera");
            myTruck = GameObject.Find("StarTruck(Clone)");
            floatingOrigin = GameObject.Find("[FloatingOriginManager]").GetComponent<FloatingOriginManager>();
            myPlayerRigid = myPlayer.GetComponent<Rigidbody>();
            myTruckRigid = myTruck.GetComponent<Rigidbody>();
            playerLocation = myPlayer.GetComponent<PlayerLocation>();
            spaceSuitObj = myTruck.transform.Find("Interior").transform.Find("SpaceSuit_Root").transform.Find("SpaceSuit").GetChild(0).gameObject;
            spaceSuitMats = spaceSuitObj.GetComponent<MeshRenderer>().materials;
        }

        private static void Client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Disconnected from Server: {e.Reason.ToString()}");

            foreach (var player in playerList.Values) { GameObject.Destroy(player.Player); GameObject.Destroy(player.Truck); }
            ushort[] keys = playerList.Keys.ToArray<ushort>();
            foreach (var pId in keys) { playerList.Remove(pId); }
        }

        private static void Client_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Client disconnected from Server: {e.Id}");
        }

        public static void Client_Connected(object sender, EventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Connected to Server");
            OnArrivedAtSector();
        }

        private static void Client_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Client Connected: {e.Id}");
        }

        public static void Client_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            StarTruckMP.Log.LogInfo($"Connection Failed");
        }

        public static void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.MessageId == (ushort)messageType.clientJoin)
            {
                foreach (ushort id in e.Message.GetUShorts())
                {
                    Vector3 pPos = new Vector3(e.Message.GetFloat(), e.Message.GetFloat(), e.Message.GetFloat());
                    Vector3 pRot = new Vector3(e.Message.GetFloat(), e.Message.GetFloat(), e.Message.GetFloat());
                    string sector = e.Message.GetString();
                    playerList.Add(id, Messages.createPlayer(id, pPos, pRot, sector));
                } 
                SendMovement();
            }

            if (e.MessageId == (ushort)messageType.movementUpdate)
            {
                ushort playerId = e.Message.GetUShort();

                if (playerId != client.Id)
                {
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

                    playerInfo currentPlayer;
                    playerList.TryGetValue(playerId, out currentPlayer);
                    playerList[playerId] = currentPlayer;

                    if (isTruck)
                    {
                        Messages.updateMovement(currentPlayer.Truck, playerPos, playerRot, playerVel, playerAngVel);
                        currentPlayer.truckTrans.Pos = playerPos;
                        currentPlayer.truckTrans.Rot = playerRot;
                        currentPlayer.truckTrans.Vel = playerVel;
                        currentPlayer.truckTrans.AngVel = playerAngVel;

                        if (inSeat)
                        {
                            Messages.updateMovement(currentPlayer.Player, playerPos, playerRot, playerVel, playerAngVel);
                            currentPlayer.playerTrans.Pos = playerPos;
                            currentPlayer.playerTrans.Rot = playerRot;
                            currentPlayer.playerTrans.Vel = playerVel;
                            currentPlayer.playerTrans.AngVel = playerAngVel;
                        }
                    }
                    else
                    {
                        Messages.updateMovement(currentPlayer.Player, playerPos, playerRot, playerVel, playerAngVel);
                        currentPlayer.playerTrans.Pos = playerPos;
                        currentPlayer.playerTrans.Rot = playerRot;
                        currentPlayer.playerTrans.Vel = playerVel;
                        currentPlayer.playerTrans.AngVel = playerAngVel;
                    }
                }
            }

            if (e.MessageId == (ushort)messageType.clientDisconnect)
            {
                ushort clientId = e.Message.GetUShort();
                playerInfo clientInfo;
                playerList.TryGetValue(clientId, out clientInfo);

                GameObject.Destroy(clientInfo.Truck);
                GameObject.Destroy(clientInfo.Player);
                playerList.Remove(clientId);
            }

            if (e.MessageId == (ushort)messageType.updateSector)
            {
                ushort clientId = e.Message.GetUShort();
                if (clientId != client.Id) 
                {
                    playerInfo clientInfo;
                    playerList.TryGetValue(clientId, out clientInfo);
                    clientInfo.sector = e.Message.GetString();
                    playerList[clientId] = clientInfo;

                    RemoveFromSector(clientId, clientInfo);
                }
            }

            if (e.MessageId == (ushort)messageType.updateLivery)
            {
                ushort clientId = e.Message.GetUShort();
                if (clientId != client.Id)
                {
                    var livery = e.Message.GetString();
                    playerInfo clientInfo;
                    playerList.TryGetValue(clientId, out clientInfo);
                    clientInfo.livery = livery;
                    playerList[clientId] = clientInfo;
                    clientInfo.Truck.transform.GetChild(0).GetComponent<LiveryAndDamageApplierTruckExterior>().LoadAndApplyLiveryById(livery);
                }
            }
        }

        public static async void SendMovement()
        {
            while (client.IsConnected)
            {

                if (myTruck != null && playerLocation)
                {
                    if ((floatingOrigin.m_currentOrigin + myTruck.transform.position) != truckTrans.Pos || myTruck.transform.eulerAngles != truckTrans.Rot || myTruckRigid.velocity != truckTrans.Vel || myTruckRigid.angularVelocity != truckTrans.AngVel)
                    {
                        client.Send(Messages.createMovementMessage(client.Id, floatingOrigin.m_currentOrigin + myTruck.transform.position, myTruck.transform.eulerAngles, myTruckRigid.velocity, myTruckRigid.angularVelocity, true, false));
                        truckTrans.Pos = floatingOrigin.m_currentOrigin + myTruck.transform.position;
                        truckTrans.Rot = myTruck.transform.eulerAngles;
                        truckTrans.Vel = myTruckRigid.velocity;
                        truckTrans.AngVel = myTruckRigid.angularVelocity;
                    }
                }
                if (myPlayer != null && playerLocation != null)
                {
                    if (PlayerLocation.worldPosition != playerTrans.Pos || playerCam.transform.eulerAngles != playerTrans.Rot || myPlayerRigid.velocity != playerTrans.Vel || myPlayerRigid.angularVelocity != playerTrans.AngVel)
                    {
                        client.Send(Messages.createMovementMessage(client.Id, PlayerLocation.worldPosition + new Vector3(0,-1,0), playerCam.transform.eulerAngles, myPlayerRigid.velocity, myPlayerRigid.angularVelocity, false, false));
                        playerTrans.Pos = PlayerLocation.worldPosition;
                        playerTrans.Rot = playerCam.transform.eulerAngles;
                        playerTrans.Vel = myPlayerRigid.velocity;
                        playerTrans.AngVel = myPlayerRigid.angularVelocity;
                    }
                }
                await System.Threading.Tasks.Task.Delay(StarTruckMP.MoveUpdate.Value);
            }
                
        }

        public static void equipLivery(string livery)
        {
            if (client.IsConnected)
            {
                client.Send(Messages.updateLivery(client.Id, livery));
            }
        }

        public static void OnArrivedAtSector()
        {
            if (client.IsConnected)
            {
                currentSector = GameObject.Find("[Sector]").scene.name;
                client.Send(Messages.updateSector(client.Id, currentSector));
                StarTruckMP.Log.LogInfo($"Entered Sector: {currentSector}");

                foreach (var client in playerList)
                {
                    var cId = client.Key;
                    var c = client.Value;
                        
                    RemoveFromSector(cId, c);
                }
            }
        }

        public static void RemoveFromSector(ushort clientId, playerInfo clientInfo)
        {
            if (clientInfo.sector != currentSector)
            {
                GameObject.Destroy(clientInfo.Truck);
                GameObject.Destroy(clientInfo.Player);
            }
            else if (clientInfo.sector == currentSector && clientInfo.Truck == null)
            {
                playerInfo player = Messages.createPlayer(clientId, playerList[clientId].truckTrans.Pos, playerList[clientId].truckTrans.Pos, currentSector);
                clientInfo.Truck = player.Truck;
                clientInfo.Player = player.Player;
                playerList[clientId] = clientInfo;
            }
        }
    }
}
