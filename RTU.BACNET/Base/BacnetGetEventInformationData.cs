namespace System.Net.BACnet;

public struct BACnetGetEventInformationData
{
    public BACnetObjectId objectIdentifier;
    public BACnetEventStates eventState;
    public BACnetBitString acknowledgedTransitions;
    public BACnetGenericTime[] eventTimeStamps;    //3
    public BACnetNotifyTypes notifyType;
    public BACnetBitString eventEnable;
    public uint[] eventPriorities;     //3
}
