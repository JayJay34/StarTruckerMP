using UnityEngine;

namespace StarTruckMP.Utilities
{
    public enum messageType
    {
        clientJoin,
        clientDisconnect,
        movementUpdate,
        chatMessage,
        updateSector,
        updateLivery
    }

    public struct playerInfo
    {
        public GameObject Player;
        public GameObject Truck;
        public movementTrans playerTrans;
        public movementTrans truckTrans;
        public string Name;
        public string sector;
        public bool seated;
        public string livery;
    }

    public struct movementTrans
    {
        public Vector3 Pos;
        public Vector3 Rot;
        public Vector3 Vel;
        public Vector3 AngVel;
    }

    internal class Utilities
    {
    }
}
