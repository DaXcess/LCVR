using Dissonance.Integrations.Unity_NFGO;
using Dissonance.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LCVR.Networking
{
    /// <summary>
    /// Wrapper class for a SlaveClientCollection which is non-public inside Dissonance Voice
    /// </summary>
    internal class Peers(object instance)
    {
        private static readonly Type slaveClientConnectionType;
        private static readonly MethodInfo tryGetClientInfoByName;
        private static readonly MethodInfo tryGetClientInfoById;

        private readonly object instance = instance;

        public Dictionary<ushort, ClientInfo<NfgoConn?>> Clients
        {
            get => (Dictionary<ushort, ClientInfo<NfgoConn?>>)AccessTools.Field(instance.GetType(), "_clientsByPlayerId").GetValue(instance);
        }

        static Peers()
        {
            slaveClientConnectionType = AccessTools.TypeByName("Dissonance.Networking.Client.SlaveClientCollection`1").MakeGenericType(typeof(NfgoConn));
            tryGetClientInfoByName = AccessTools.Method(slaveClientConnectionType, "TryGetClientInfoByName");
            tryGetClientInfoById = AccessTools.Method(slaveClientConnectionType, "TryGetClientInfoById");
        }

        public bool TryGetClientInfoByName(string name, out ClientInfo<NfgoConn?> clientInfo)
        {
            clientInfo = null;

            var @params = new object[] { name, null };
            var value = (bool)tryGetClientInfoByName.Invoke(instance, @params);

            if (!value)
                return false;

            clientInfo = (ClientInfo<NfgoConn?>)@params[1];
            return true;
        }

        public bool TryGetClientInfoById(ushort clientId, out ClientInfo<NfgoConn?> clientInfo)
        {
            clientInfo = null;

            var @params = new object[] { clientId, null };
            var value = (bool)tryGetClientInfoById.Invoke(instance, @params);

            if (!value)
                return false;

            clientInfo = (ClientInfo<NfgoConn?>)@params[1];
            return true;
        }
    }
}
