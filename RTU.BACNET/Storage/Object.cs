namespace System.Net.BACnet.Storage;

[Serializable]
public class Object
{
    [XmlAttribute]
    public BACnetObjectTypes Type { get; set; }

    [XmlAttribute]
    public uint Instance { get; set; }

    public Property[] Properties { get; set; }

    public Object()
    {
        Properties = new Property[0];
    }
}
