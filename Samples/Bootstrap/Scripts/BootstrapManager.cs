using Netcode.Transports.AzureWebPubSub;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

namespace AzureWebPubSubTransportSample
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        private const int _length = 4;
        private string _roomId;

        private void Awake()
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < _length; i++)
                stringBuilder.Append(characters[Random.Range(0, characters.Length)]);
            _roomId = stringBuilder.ToString();
            Debug.Log(_roomId);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 600, 300));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                GUILayout.Label("How to play: ");
                GUILayout.TextArea("Step 1: Duplicate to another 2 tabs in your browser. \n" +
              "One will be used as 'Host' to view the changes. The others are 'Client' to make changes\n" +
              "Step 2: Fill in the room name in the duplicated tab, from the very first tab\n" +
              "Step 3: Click 'Create Room' in the 'Host', you will see a random color ball in the center\n" +
              "Step 4: Click 'Join Room' in the 'Client', you will see a random color ball in the center\n" +
              "Step 5: Click 'Random Teleport' in the any tab, you will the balls' location are synchronized!\n" +
              "Step 6: Note that translation is only smooth in 'Client' tab\n" +
              "Step 7: Get more details from: https://aka.ms/webpubsub/unity-multiplayer-sample\n" +
              "Step 8: Learn how to use Azure Realtime Transport from: https://aka.ms/webpubsub/unity-multiplayer-transport");

                GUILayout.Label("Enter Room: ");
                _roomId = GUILayout.TextField(_roomId);
                var transport = networkManager.NetworkConfig.NetworkTransport as AzureWebPubSubTransport;
                if (transport.State == ConnectionState.Init)
                {
                    transport.SetConnectionData(_roomId);
                }

                if (GUILayout.Button($"Create Room"))
                {
                    networkManager.StartHost();
                }

                if (GUILayout.Button($"Join Room"))
                {
                    networkManager.StartClient();
                }
            }
            else
            {
                GUILayout.Label($"Mode: {(networkManager.IsHost ? "Host" : networkManager.IsServer ? "Server" : "Client")}");

                // "Random Teleport" button will only be shown to clients
                if (networkManager.IsClient)
                {
                    if (GUILayout.Button("Random Teleport"))
                    {
                        if (networkManager.LocalClient != null)
                        {
                            // Get `BootstrapPlayer` component from the player's `PlayerObject`
                            if (networkManager.LocalClient.PlayerObject.TryGetComponent(out BootstrapPlayer bootstrapPlayer))
                            {
                                // Invoke a `ServerRpc` from client-side to teleport player to a random position on the server-side
                                bootstrapPlayer.RandomTeleportServerRpc();
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"local client is null");
                        }
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}