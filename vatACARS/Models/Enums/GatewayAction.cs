namespace vatACARS.Models.Enums
{
    public enum Gateway
    {
        Authentication = 1,
        Identity = 2,
        InternalMessaging = 3,
        Station = 4,
        CPDLC = 5,
        Telex = 6,
        Administrative = 7,
        Miscellaneous = 8
    }

    public enum GatewayAction
    {
        Authenticate = 1,
        Reconnect = 2,
        Disconnect = 3,
        Heartbeat = 4,

        RegisterClient = 10,
        UpdateClientStatus = 11,
        Logout = 12,

        SendMessage = 20,
        ReceiveMessage = 21,
        MessageAcknowledgement = 22,

        AddStation = 30,
        RemoveStation = 31,
        UpdateStationStatus = 32,

        SendCPDLCMessage = 40,
        ReceiveCPDLCMessage = 41,
        CPDLCAcknowledgement = 42,
        CPDLCError = 43,

        SendTelexMessage = 50,
        ReceiveTelexMessage = 51,
        TelexAcknowledgement = 52,
        TelexError = 53,

        Broadcast = 60,
        KickUser = 61,
        ServerAnnouncement = 62,

        Error = 90,
        InvalidAction = 91,
        PermissionDenied = 92,
    }
}
