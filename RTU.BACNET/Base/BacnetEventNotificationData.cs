namespace System.Net.BACnet;

public struct BACnetEventNotificationData
{
    public uint processIdentifier;
    public BACnetObjectId initiatingObjectIdentifier;
    public BACnetObjectId eventObjectIdentifier;
    public BACnetGenericTime timeStamp;
    public uint notificationClass;
    public byte priority;
    public BACnetEventTypes eventType;
    public string messageText;       /* OPTIONAL - Set to NULL if not being used */
    public BACnetNotifyTypes notifyType;
    public bool ackRequired;
    public BACnetEventStates fromState;
    public BACnetEventStates toState;

    /*
     ** Each of these structures in the union maps to a particular eventtype
     ** Based on BACnetNotificationParameters
     */

    /*
     ** EVENT_CHANGE_OF_BITSTRING
     */
    public BACnetBitString changeOfBitstring_referencedBitString;
    public BACnetBitString changeOfBitstring_statusFlags;
    /*
     ** EVENT_CHANGE_OF_STATE
     */
    public BACnetPropertyState changeOfState_newState;
    public BACnetBitString changeOfState_statusFlags;
    /*
     ** EVENT_CHANGE_OF_VALUE
     */
    public BACnetBitString changeOfValue_changedBits;
    public float changeOfValue_changeValue;
    public BACnetCOVTypes? changeOfValue_tag;
    public BACnetBitString changeOfValue_statusFlags;
    /*
     ** EVENT_COMMAND_FAILURE
     */
    public uint commandFailure_commandValue;
    public BACnetBitString commandFailure_statusFlags;
    public uint commandFailure_feedbackValue;
    /*
     ** EVENT_FLOATING_LIMIT
     */
    public float floatingLimit_referenceValue;
    public BACnetBitString floatingLimit_statusFlags;
    public float floatingLimit_setPointValue;
    public float floatingLimit_errorLimit;
    /*
     ** EVENT_OUT_OF_RANGE
     */
    public float outOfRange_exceedingValue;
    public BACnetBitString outOfRange_statusFlags;
    public float outOfRange_deadband;
    public float outOfRange_exceededLimit;
    /*
     ** EVENT_CHANGE_OF_LIFE_SAFETY
     */
    public BACnetLifeSafetyStates? changeOfLifeSafety_newState;
    public BACnetLifeSafetyModes? changeOfLifeSafety_newMode;
    public BACnetBitString changeOfLifeSafety_statusFlags;
    public BACnetLifeSafetyOperations? changeOfLifeSafety_operationExpected;
    /*
     ** EVENT_EXTENDED
     **
     ** Not Supported!
     */
    /*
     ** EVENT_BUFFER_READY
     */
    public BACnetDeviceObjectPropertyReference bufferReady_bufferProperty;
    public uint bufferReady_previousNotification;
    public uint bufferReady_currentNotification;
    /*
     ** EVENT_UNSIGNED_RANGE
     */
    public uint unsignedRange_exceedingValue;
    public BACnetBitString unsignedRange_statusFlags;
    public uint unsignedRange_exceededLimit;
    /*
     ** EVENT_EXTENDED
     */
    public uint extended_vendorId;
    public uint extended_eventType;
    public object[] extended_parameters;
    /*
     ** EVENT_CHANGE_OF_RELIABILITY
     */
    public BACnetReliability changeOfReliability_reliability;
    public BACnetBitString changeOfReliability_statusFlags;
    public BACnetPropertyValue[] changeOfReliability_propertyValues;

    public override string ToString()
    {
        return $"initiatingObject: {initiatingObjectIdentifier}, eventObject: {eventObjectIdentifier}, "
             + $"eventType: {eventType}, notifyType: {notifyType}, timeStamp: {timeStamp}, "
             + $"fromState: {fromState}, toState: {toState}"
             + (notifyType != BACnetNotifyTypes.NOTIFY_ACK_NOTIFICATION ? $", {GetEventDetails()}" : "");
    }

    private string GetEventDetails()
    {
        switch (eventType)
        {
            case BACnetEventTypes.EVENT_CHANGE_OF_BITSTRING:
                return $"referencedBitString: {changeOfBitstring_referencedBitString}, statusFlags: {changeOfBitstring_statusFlags}";

            case BACnetEventTypes.EVENT_CHANGE_OF_STATE:
                return $"newState: {changeOfState_newState}, statusFlags: {changeOfState_statusFlags}";

            case BACnetEventTypes.EVENT_CHANGE_OF_VALUE:
                return $"changedBits: {changeOfValue_changedBits}, changeValue: {changeOfValue_changeValue}, "
                       + $"tag: {changeOfValue_tag}, statusFlags: {changeOfValue_statusFlags}";

            case BACnetEventTypes.EVENT_FLOATING_LIMIT:
                return $"referenceValue: {floatingLimit_referenceValue}, statusFlags: {floatingLimit_statusFlags}, "
                       + $"setPointValue: {floatingLimit_setPointValue}, errorLimit: {floatingLimit_errorLimit}";

            case BACnetEventTypes.EVENT_OUT_OF_RANGE:
                return $"exceedingValue: {outOfRange_exceedingValue}, statusFlags: {outOfRange_statusFlags}, "
                       + $"deadband: {outOfRange_deadband}, exceededLimit: {outOfRange_exceededLimit}";

            case BACnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                return $"newState: {changeOfLifeSafety_newState}, newMode: {changeOfLifeSafety_newMode}, "
                       +
                       $"statusFlags: {changeOfLifeSafety_statusFlags}, operationExpected: {changeOfLifeSafety_operationExpected}";

            case BACnetEventTypes.EVENT_BUFFER_READY:
                return $"bufferProperty: {bufferReady_bufferProperty}, previousNotification: {bufferReady_previousNotification}, "
                       + $"currentNotification: {bufferReady_currentNotification}";

            case BACnetEventTypes.EVENT_UNSIGNED_RANGE:
                return $"exceedingValue: {unsignedRange_exceedingValue}, statusFlags: {unsignedRange_statusFlags}, "
                       + $"exceededLimit: {unsignedRange_exceededLimit}";

            case BACnetEventTypes.EVENT_EXTENDED:
                return $"vendorId: {extended_vendorId}, extendedEventType: {extended_eventType}, parameters: [{extended_parameters?.Length ?? 0}]";

            case BACnetEventTypes.EVENT_CHANGE_OF_RELIABILITY:
                var properties = string.Join(", ", changeOfReliability_propertyValues?.Select(p => $"{p.property}"));
                return $"reliability: {changeOfReliability_reliability}, statusFlags: {changeOfReliability_statusFlags}, properties: [{properties}]";

            default:
                return "no details";
        }
    }
};
