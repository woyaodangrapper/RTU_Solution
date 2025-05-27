namespace System.Net.BACnet;

public struct BACnetCOVSubscription
{
    /* BACnetRecipientProcess */
    public BACnetAddress Recipient;
    public uint subscriptionProcessIdentifier;
    /* BACnetObjectPropertyReference */
    public BACnetObjectId monitoredObjectIdentifier;
    public BACnetPropertyReference monitoredProperty;
    /* BACnetCOVSubscription */
    public bool IssueConfirmedNotifications;
    public uint TimeRemaining;
    public float COVIncrement;
}
