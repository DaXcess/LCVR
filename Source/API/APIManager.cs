using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LCVR.Networking;

namespace LCVR.API
{
    internal static class APIManager
    {
        private static LCVRPlugin[] plugins;
        
        internal static void Initialize()
        {
            plugins = AccessTools.AllTypes().Where(type =>
            {
                if (type.GetCustomAttribute<LCVRPluginAttribute>() is null)
                    return false;

                if (type.GetConstructors().All(ctor => ctor.GetParameters().Length > 0))
                    return false;

                if (!typeof(LCVRPlugin).IsAssignableFrom(type))
                    return false;

                return true;
            }).Select(type => (LCVRPlugin)Activator.CreateInstance(type, BindingFlags.Public)).ToArray();
            
            plugins.Do(plugin => plugin.OnLoad());
        }

        internal static void OnConfigChanged()
        {
            plugins.Do(plugin => plugin.OnConfigChanged());
        }

        internal static void OnLobbyJoined()
        {
            plugins.Do(plugin => plugin.OnLobbyJoined());
        }

        internal static void OnLobbyLeft()
        {
            plugins.Do(plugin => plugin.OnLobbyLeft());
        }

        internal static void OnVRPlayerJoined(VRNetPlayer player)
        {
            plugins.Do(plugin => plugin.OnVRPlayerJoined(player));
        }

        internal static void OnVRPlayerLeft(VRNetPlayer player)
        {
            plugins.Do(plugin => plugin.OnVRPlayerLeft(player));
        }

        internal static void OnLocalPlayerDied()
        {
            plugins.Do(plugin => plugin.OnLocalPlayerDied());
        }

        internal static void OnVRPlayerDied(VRNetPlayer player)
        {
            plugins.Do(plugin => plugin.OnVRPlayerDied(player));
        }

        internal static void OnPauseMenuOpened()
        {
            plugins.Do(plugin => plugin.OnPauseMenuOpened());
        }

        internal static void OnPauseMenuClosed()
        {
            plugins.Do(plugin => plugin.OnPauseMenuClosed());
        }
    }

    public interface LCVRPlugin
    {
        /// <summary>
        /// Executed whenever the LCVR API is loaded. Can be used as an entrypoint to your plugin.
        /// </summary>
        void OnLoad()
        {
        }

        /// <summary>
        /// Executed whenever the user changes the configuration for LCVR using the Settings Manager.
        /// </summary>
        void OnConfigChanged()
        {
        }

        /// <summary>
        /// Executed whenever the local player joins a lobby.
        /// </summary>
        void OnLobbyJoined()
        {
        }

        /// <summary>
        /// Executed whenever the local player leaves a lobby.
        /// </summary>
        void OnLobbyLeft()
        {
        }

        /// <summary>
        /// Executed whenever a VR player has joined the lobby.
        /// </summary>
        /// <param name="player">The VR player that joined</param>
        void OnVRPlayerJoined(VRNetPlayer player)
        {
        }

        /// <summary>
        /// Executed whenever a VR player has left the lobby.
        /// </summary>
        /// <param name="player">The VR player that left</param>
        void OnVRPlayerLeft(VRNetPlayer player)
        {
        }

        /// <summary>
        /// Executed whenever the local player dies.
        /// </summary>
        void OnLocalPlayerDied()
        {
        }

        /// <summary>
        /// Executed whenever a VR player dies.
        /// </summary>
        void OnVRPlayerDied(VRNetPlayer player)
        {
        }

        /// <summary>
        /// Executed whenever the pause menu is opened.
        ///
        /// <i>This method only gets executed when playing in VR.</i>
        /// </summary>
        void OnPauseMenuOpened()
        {
        }

        /// <summary>
        /// Executed whenever the pause menu is closed.
        ///
        /// <i>This method only gets executed when playing in VR.</i>
        /// </summary>
        void OnPauseMenuClosed()
        {
        }
    }

    public class LCVRPluginAttribute : Attribute { }
}