using Riptide;
using StarTruckMP.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarTruckMP.Encoding
{
    internal class Messages
    {
        public static playerInfo createPlayer(ushort playerId, Vector3 position, Vector3 rotation, string sector)
        {
            GameObject sectorGO = GameObject.Find("[Sector]");
            var myRigid = StarTruckClient.StarTruckClient.myTruck.GetComponent<Rigidbody>();

            //Spawn new Truck GameObject
            GameObject newTruck = new GameObject("RemoteTruck" + playerId);
            SceneManager.MoveGameObjectToScene(newTruck, sectorGO.scene);
            newTruck.transform.SetParent(null);
            var newRigid = newTruck.AddComponent<Rigidbody>();
            newRigid.useGravity = myRigid.useGravity;
            newRigid.drag = myRigid.drag;
            newRigid.angularDrag = myRigid.angularDrag;
            newRigid.mass = myRigid.mass;
            newRigid.centerOfMass = myRigid.centerOfMass;
            newRigid.detectCollisions = false;
            newRigid.isKinematic = myRigid.isKinematic;
            newRigid.maxAngularVelocity = myRigid.maxAngularVelocity;
            newRigid.maxDepenetrationVelocity = myRigid.maxDepenetrationVelocity;
            newRigid.inertiaTensor = myRigid.inertiaTensor;
            newRigid.inertiaTensorRotation = myRigid.inertiaTensorRotation;

            GameObject exteriorObj = GameObject.Find("Exterior");
            GameObject newExterior = GameObject.Instantiate(exteriorObj, Vector3.zero, Quaternion.EulerAngles(Vector3.zero), newTruck.transform);
            newExterior.name = "ClientExterior" + playerId;

            newExterior.transform.Find("StarTruck_Hatch").Find("Marker").gameObject.SetActive(false);
            Object.Destroy(newExterior.transform.Find("StarTruck_Hatch").GetChild(0).GetComponent<DoorAnimator>());
            Object.Destroy(newExterior.transform.Find("StarTruck_Hatch").GetChild(0).GetComponent<GameEventListener>());
            Object.Destroy(newExterior.transform.Find("StarTruck_Hatch").GetChild(0).GetComponent<EPOOutline.TargetStateListener>());
            newExterior.transform.Find("MonitorCameras").gameObject.SetActive(false);
            newExterior.transform.Find("PlayerSpawnMarker").gameObject.SetActive(false);
            newExterior.transform.Find("ThrusterCameraShakeController").gameObject.SetActive(false);
            var customization = newExterior.transform.GetComponent<CustomizationApplier>();
            var livDamApp = newExterior.transform.GetComponent<LiveryAndDamageApplierTruckExterior>();
            customization.m_linkedLiveryApplier = livDamApp;

            //Disable Truck Collision
            foreach (var item in newExterior.GetComponentsInChildren<Collider>())
            {
                item.enabled = false;
            }

            //Spawn new Player GameObject
            GameObject newPlayer = new GameObject("RemotePlayer" + playerId);
            SceneManager.MoveGameObjectToScene(newPlayer, sectorGO.scene);
            newPlayer.transform.SetParent(null);

            GameObject newSuit = GameObject.Instantiate(StarTruckClient.StarTruckClient.spaceSuitObj, Vector3.zero, Quaternion.EulerAngles(Vector3.zero), newPlayer.transform);
            newSuit.GetComponent<MeshRenderer>().materials = StarTruckClient.StarTruckClient.spaceSuitMats;
            newSuit.active = true;
            newSuit.name = "ClientSuit" + playerId;
            Object.Destroy(newSuit.transform.GetComponent<SpaceSuitController>());
            Object.Destroy(newSuit.transform.GetComponent<UnityEngine.CapsuleCollider>());
            Object.Destroy(newSuit.transform.GetComponent<OutlinableSetterUpper>());
            Object.Destroy(newSuit.transform.GetComponent<EPOOutline.Outlinable>());
            Object.Destroy(newSuit.transform.GetComponent<EPOOutline.TargetStateListener>());
            Object.Destroy(newSuit.transform.GetComponent<MaterialSwitcher>());
            Object.Destroy(newSuit.transform.GetComponent<InteractTarget>());
            Object.Destroy(newSuit.transform.GetComponent<DoorController>());
            myRigid = StarTruckClient.StarTruckClient.myPlayer.GetComponent<Rigidbody>();

            newRigid = newPlayer.AddComponent<Rigidbody>();
            newRigid.useGravity = myRigid.useGravity;
            newRigid.drag = myRigid.drag;
            newRigid.angularDrag = myRigid.angularDrag;
            newRigid.mass = myRigid.mass;
            newRigid.centerOfMass = myRigid.centerOfMass;
            newRigid.detectCollisions = false;
            newRigid.isKinematic = myRigid.isKinematic;
            newRigid.maxAngularVelocity = myRigid.maxAngularVelocity;
            newRigid.maxDepenetrationVelocity = myRigid.maxDepenetrationVelocity;
            newRigid.inertiaTensor = myRigid.inertiaTensor;
            newRigid.inertiaTensorRotation = myRigid.inertiaTensorRotation;

            playerInfo currentPlayer = new playerInfo();
            currentPlayer.Player = newPlayer;
            currentPlayer.Truck = newTruck;
            currentPlayer.sector = sector;

            return currentPlayer;
        }

        public static Message createMovementMessage(ushort playerId, Vector3 position, Vector3 rotation, Vector3 velocity, Vector3 angVel, bool isTruck, bool inSeat)
        {
            float[] playerTransform = { position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, velocity.x, velocity.y, velocity.z, angVel.x, angVel.y, angVel.z};

            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)messageType.movementUpdate);
            message.AddUShort(playerId);
            message.AddFloats(playerTransform);
            message.AddBool(isTruck);
            message.AddBool(inSeat);

            return message;
        }

        public static void updateMovement(GameObject playerObject, Vector3 position, Vector3 rotation, Vector3 velocity, Vector3 angVel)
        {
            if (playerObject != null)
            {
                playerObject.transform.position = position - StarTruckClient.StarTruckClient.floatingOrigin.m_currentOrigin;
                playerObject.transform.eulerAngles = rotation;
                playerObject.GetComponent<Rigidbody>().velocity = velocity; 
                playerObject.GetComponent<Rigidbody>().angularVelocity = angVel; 
                
            }
        }

        public static Message updateLivery(ushort playerId, string itemId)
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)messageType.updateLivery);
            message.AddUShort(playerId);
            message.AddString(itemId);

            return message;
        }

        public static Message updateSector(ushort playerId, string sector)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)messageType.updateSector);
            message.AddUShort(playerId);
            message.AddString(sector);

            return message;
        }
    }


}
