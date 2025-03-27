namespace System.Net.BACnet;

public struct BACnetPropertyState
{
    public enum BACnetPropertyStateTypes
    {
        BOOLEAN_VALUE,
        BINARY_VALUE,
        EVENT_TYPE,
        POLARITY,
        PROGRAM_CHANGE,
        PROGRAM_STATE,
        REASON_FOR_HALT,
        RELIABILITY,
        STATE,
        SYSTEM_STATUS,
        UNITS,
        UNSIGNED_VALUE,
        LIFE_SAFETY_MODE,
        LIFE_SAFETY_STATE
    }

    public struct State
    {
        public bool boolean_value;
        public BACnetBinaryPv binaryValue;
        public BACnetEventTypes eventType;
        public BACnetPolarity polarity;
        public BACnetProgramRequest programChange;
        public BACnetProgramState programState;
        public BACnetProgramError programError;
        public BACnetReliability reliability;
        public BACnetEventStates state;
        public BACnetDeviceStatus systemStatus;
        public BACnetUnitsId units;
        public uint unsignedValue;
        public BACnetLifeSafetyModes lifeSafetyMode;
        public BACnetLifeSafetyStates lifeSafetyState;
    }

    public BACnetPropertyStateTypes tag;
    public State state;

    public override string ToString()
    {
        return $"{tag}:{state}";
    }
}
