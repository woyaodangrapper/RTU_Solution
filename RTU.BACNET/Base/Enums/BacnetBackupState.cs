namespace System.Net.BACnet;

public enum BACnetBackupState
{
    IDLE = 0,
    PREPARING_FOR_BACKUP = 1,
    PREPARING_FOR_RESTORE = 2,
    PERFORMING_A_BACKUP = 3,
    PERFORMING_A_RESTORE = 4
}
