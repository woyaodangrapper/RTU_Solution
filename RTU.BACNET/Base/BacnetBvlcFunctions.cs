﻿namespace System.Net.BACnet;

public enum BACnetBvlcFunctions : byte
{
    BVLC_RESULT = 0,
    BVLC_WRITE_BROADCAST_DISTRIBUTION_TABLE = 1,
    BVLC_READ_BROADCAST_DIST_TABLE = 2,
    BVLC_READ_BROADCAST_DIST_TABLE_ACK = 3,
    BVLC_FORWARDED_NPDU = 4,
    BVLC_REGISTER_FOREIGN_DEVICE = 5,
    BVLC_READ_FOREIGN_DEVICE_TABLE = 6,
    BVLC_READ_FOREIGN_DEVICE_TABLE_ACK = 7,
    BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY = 8,
    BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK = 9,
    BVLC_ORIGINAL_UNICAST_NPDU = 10,
    BVLC_ORIGINAL_BROADCAST_NPDU = 11,
    MAX_BVLC_FUNCTION = 12
}
