using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ClientGroupScript
{

    public static Dictionary<ulong, string> clientToName = new Dictionary<ulong, string>();
    public static Dictionary<string, ulong> nameToClient = new Dictionary<string, ulong>();

    public static Dictionary<ulong, bool> clientIsReady = new Dictionary<ulong, bool>();

}
