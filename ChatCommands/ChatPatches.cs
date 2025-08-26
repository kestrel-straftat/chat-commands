using System.Collections.Generic;
using HarmonyLib;
using HeathenEngineering.DEMO;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Input = UnityEngine.Input;
using Object = UnityEngine.Object;

namespace ChatCommands;

public static class ChatPatches
{
    public const string c_commandPrefix = "/";
    public const KeyCode c_commandHotkey = KeyCode.Slash;

    private static LobbyManager m_lobbyManager;
    private static GameObject m_messageTemplate;
    private static Transform m_messageRoot;
    private static List<IChatMessage> m_chatMessages;
    
    public static void SendChatMessage(string message) => m_lobbyManager.Lobby.SendChatMessage(message);

    public static void ClearChat() {
        foreach (var message in m_chatMessages) {
            try {
                var obj = message.GameObject;
                Object.Destroy(obj);
            }
            catch {
                // ignored
            }
        }
        m_chatMessages.Clear();
    }

    public static void SendSystemMessage(object message, bool showUsername = true, TextAlignmentOptions alignment = TextAlignmentOptions.TopRight) {
        var messageInstantiated = Object.Instantiate(m_messageTemplate, m_messageRoot);
        messageInstantiated.transform.SetAsLastSibling();
        messageInstantiated.GetComponent<VerticalLayoutGroup>().padding.right = 0;

        if (messageInstantiated.GetComponent<BasicChatMessage>() is { } messageComponent) {
            var usernameText = messageComponent.username.GetComponent<TextMeshProUGUI>();
            usernameText.alignment = alignment;
            var messageText = messageComponent.message;
            messageText.alignment = alignment;
            usernameText.text = "SYSTEM";
            messageText.text = message.ToString();
            if (!showUsername) messageComponent.username.gameObject.SetActive(false);
            
            // hide avatar
            messageComponent.IsExpanded = false;
            m_chatMessages.Add(messageComponent);
        }
        else {
            // oops, clean up after ourselves so there isnt an empty message in chat
            Object.Destroy(messageInstantiated);
            Plugin.Logger.LogError($"Failed to send system message \"{message}\" in chat~ null BasicChatMessage.");
        }
        
        //// need to also check here as these messages arent sent to server and so
        //// don't trigger HandleChatMessage, where the normal check takes place.
        //while (m_chatMessages.Count > maxMessages) {
        //    Object.Destroy(m_chatMessages[0].GameObject);
        //    m_chatMessages.RemoveAt(0);
        //}
    }

    
    [HarmonyPatch(typeof(LobbyChatUILogic))]
    internal static class LobbyChatUILogicPatch
    {
        // must be postfix as lobbymanager is init'd in start
        [HarmonyPatch("Start")]
        [HarmonyPostfix] 
        public static void ModifyPanel(
            GameObject ___theirChatTemplate, Transform ___messageRoot,
            List<IChatMessage> ___chatMessages, GameObject ___chatPanel,
            LobbyManager ___lobbyManager) {
            
            m_lobbyManager =  ___lobbyManager;
            m_messageTemplate = ___theirChatTemplate;
            m_messageRoot = ___messageRoot;
            m_chatMessages = ___chatMessages;
        }

        // Intercept messages before they're sent to the server and
        // redirect them to the command evaluator if they have the prefix.
        [HarmonyPatch("OnSendChatMessage")]
        [HarmonyPrefix]
        public static bool DetectCommand(TMP_InputField ___inputField) {
            var text = ___inputField.text;
            if (!text.StartsWith(c_commandPrefix)) return true;
            Evaluator.Instance.Evaluate(text.Remove(0, c_commandPrefix.Length));
            return false;
        }
    }
    
    // Open chat and fill in the command prefix automatically on the command hotkey.
    [HarmonyPatch(typeof(MatchChat))]
    internal static class MatchChatPatch
    {
        private static bool m_activeLast = true;
        
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void OpenOnCommandPrefix(GameObject ___ChatBox, TMP_InputField ___inputLine) {
            if (Input.GetKeyDown(c_commandHotkey) && !___ChatBox.activeSelf) {
                ___ChatBox.SetActive(true);
                ___inputLine.text = c_commandPrefix;
                ___inputLine.MoveToEndOfLine(false, true);
            }
            
            // make sure the chat box is cleared whenever it closes 
            var activeCurrent = ___ChatBox.activeInHierarchy;
            if (!activeCurrent && m_activeLast) {
                ___inputLine.text = string.Empty;
            }
            m_activeLast = activeCurrent;
        }

    }
}