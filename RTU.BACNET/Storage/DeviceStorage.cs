/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

namespace System.Net.BACnet.Storage;

/// <summary>
/// This is a basic example of a BACNet storage. This one is XML based. It has no fancy optimizing or anything.
/// </summary>
[Serializable]
public class DeviceStorage
{
    [XmlIgnore]
    public uint DeviceId { get; set; }

    public delegate void ChangeOfValueHandler(DeviceStorage sender, BACnetObjectId objectId, BACnetPropertyIds propertyId, uint arrayIndex, IList<BACnetValue> value);
    public event ChangeOfValueHandler ChangeOfValue;
    public delegate void ReadOverrideHandler(BACnetObjectId objectId, BACnetPropertyIds propertyId, uint arrayIndex, out IList<BACnetValue> value, out ErrorCodes status, out bool handled);
    public event ReadOverrideHandler ReadOverride;
    public delegate void WriteOverrideHandler(BACnetObjectId objectId, BACnetPropertyIds propertyId, uint arrayIndex, IList<BACnetValue> value, out ErrorCodes status, out bool handled);
    public event WriteOverrideHandler WriteOverride;

    public Object[] Objects { get; set; }

    public DeviceStorage()
    {
        DeviceId = (uint)new Random().Next();
        Objects = new Object[0];
    }

    public Property FindProperty(BACnetObjectId objectId, BACnetPropertyIds propertyId)
    {
        //liniear search
        var obj = FindObject(objectId);
        return FindProperty(obj, propertyId);
    }

    private static Property FindProperty(Object obj, BACnetPropertyIds propertyId)
    {
        //liniear search
        return obj?.Properties.FirstOrDefault(p => p.Id == propertyId);
    }

    private Object FindObject(BACnetObjectTypes objectType)
    {
        //liniear search
        return Objects.FirstOrDefault(obj => obj.Type == objectType);
    }

    public Object FindObject(BACnetObjectId objectId)
    {
        //liniear search
        return Objects.FirstOrDefault(obj => obj.Type == objectId.type && obj.Instance == objectId.instance);
    }

    public enum ErrorCodes
    {
        Good = 0,
        GenericError = -1,
        NotExist = -2,
        NotForMe = -3,
        WriteAccessDenied = -4,
        UnknownObject = -5,
        UnknownProperty = -6
    }

    public int ReadPropertyValue(BACnetObjectId objectId, BACnetPropertyIds propertyId)
    {
        if (ReadProperty(objectId, propertyId, Serialize.ASN1.BACNET_ARRAY_ALL, out IList<BACnetValue> value) != ErrorCodes.Good)
            return 0;

        if (value == null || value.Count < 1)
            return 0;

        return (int)Convert.ChangeType(value[0].Value, typeof(int));
    }

    public ErrorCodes ReadProperty(BACnetObjectId objectId, BACnetPropertyIds propertyId, uint arrayIndex, out IList<BACnetValue> value)
    {
        value = new BACnetValue[0];

        //wildcard device_id
        if (objectId.type == BACnetObjectTypes.OBJECT_DEVICE && objectId.instance >= Serialize.ASN1.BACNET_MAX_INSTANCE)
            objectId.instance = DeviceId;

        //overrides
        if (ReadOverride != null)
        {
            ReadOverride(objectId, propertyId, arrayIndex, out value, out ErrorCodes status, out bool handled);
            if (handled)
                return status;
        }

        //find in storage
        var obj = FindObject(objectId);
        if (obj == null)
            return ErrorCodes.UnknownObject;

        //object found now find property
        var p = FindProperty(objectId, propertyId);
        if (p == null)
            return ErrorCodes.NotExist;

        //get value ... check for array index
        if (arrayIndex == 0)
        {
            value = new[] { new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)p.BACnetValue.Count) };
        }
        else if (arrayIndex != Serialize.ASN1.BACNET_ARRAY_ALL)
        {
            value = new[] { p.BACnetValue[(int)arrayIndex - 1] };
        }
        else
        {
            value = p.BACnetValue;
        }

        return ErrorCodes.Good;
    }

    public void ReadPropertyMultiple(BACnetObjectId objectId, ICollection<BACnetPropertyReference> properties, out IList<BACnetPropertyValue> values)
    {
        var valuesRet = new List<BACnetPropertyValue>();

        foreach (var entry in properties)
        {
            var newEntry = new BACnetPropertyValue { property = entry };

            switch (ReadProperty(objectId, (BACnetPropertyIds)entry.propertyIdentifier, entry.propertyArrayIndex, out newEntry.value))
            {
                case ErrorCodes.UnknownObject:
                    newEntry.value = new[]
                    {
                            new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_ERROR,
                            new BACnetError(BACnetErrorClasses.ERROR_CLASS_OBJECT, BACnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT))
                        };
                    break;
                case ErrorCodes.NotExist:
                    newEntry.value = new[]
                    {
                            new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_ERROR,
                            new BACnetError(BACnetErrorClasses.ERROR_CLASS_PROPERTY, BACnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY))
                        };
                    break;
            }

            valuesRet.Add(newEntry);
        }

        values = valuesRet;
    }

    public bool ReadPropertyAll(BACnetObjectId objectId, out IList<BACnetPropertyValue> values)
    {
        //find
        var obj = FindObject(objectId);
        if (obj == null)
        {
            values = null;
            return false;
        }

        //build
        var propertyValues = new BACnetPropertyValue[obj.Properties.Length];
        for (var i = 0; i < obj.Properties.Length; i++)
        {
            var newEntry = new BACnetPropertyValue
            {
                property = new BACnetPropertyReference((uint)obj.Properties[i].Id, Serialize.ASN1.BACNET_ARRAY_ALL)
            };

            if (ReadProperty(objectId, obj.Properties[i].Id, Serialize.ASN1.BACNET_ARRAY_ALL, out newEntry.value) != ErrorCodes.Good)
            {
                var bacnetError = new BACnetError(BACnetErrorClasses.ERROR_CLASS_OBJECT, BACnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY);
                newEntry.value = new[] { new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_ERROR, bacnetError) };
            }

            propertyValues[i] = newEntry;
        }

        values = propertyValues;
        return true;
    }

    public void WritePropertyValue(BACnetObjectId objectId, BACnetPropertyIds propertyId, int value)
    {
        //get existing type
        if (ReadProperty(objectId, propertyId, Serialize.ASN1.BACNET_ARRAY_ALL, out IList<BACnetValue> readValues) != ErrorCodes.Good)
            return;

        if (readValues == null || readValues.Count == 0)
            return;

        //write
        WriteProperty(objectId, propertyId, Serialize.ASN1.BACNET_ARRAY_ALL, new[]
        {
                new BACnetValue(readValues[0].Tag, Convert.ChangeType(value, readValues[0].Value.GetType()))
            });
    }


    public void WriteProperty(BACnetObjectId objectId, BACnetPropertyIds propertyId, BACnetValue value)
    {
        WriteProperty(objectId, propertyId, Serialize.ASN1.BACNET_ARRAY_ALL, new[] { value });
    }

    public ErrorCodes WriteProperty(BACnetObjectId objectId, BACnetPropertyIds propertyId, uint arrayIndex, IList<BACnetValue> value, bool addIfNotExits = false)
    {
        //wildcard device_id
        if (objectId.type == BACnetObjectTypes.OBJECT_DEVICE && objectId.instance >= Serialize.ASN1.BACNET_MAX_INSTANCE)
            objectId.instance = DeviceId;

        //overrides
        if (WriteOverride != null)
        {
            WriteOverride(objectId, propertyId, arrayIndex, value, out ErrorCodes status, out bool handled);
            if (handled)
                return status;
        }

        //find
        var p = FindProperty(objectId, propertyId);
        if (p == null)
        {
            if (!addIfNotExits) return ErrorCodes.NotExist;

            //add obj
            var obj = FindObject(objectId);
            if (obj == null)
            {
                obj = new Object
                {
                    Type = objectId.type,
                    Instance = objectId.instance
                };
                var arr = Objects;
                Array.Resize(ref arr, arr.Length + 1);
                arr[arr.Length - 1] = obj;
                Objects = arr;
            }

            //add property
            p = new Property { Id = propertyId };
            var props = obj.Properties;
            Array.Resize(ref props, props.Length + 1);
            props[props.Length - 1] = p;
            obj.Properties = props;
        }

        //set type if needed
        if (p.Tag == BACnetApplicationTags.BACNET_APPLICATION_TAG_NULL && value != null)
        {
            foreach (var v in value)
            {
                if (v.Tag == BACnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                    continue;

                p.Tag = v.Tag;
                break;
            }
        }

        //write
        p.BACnetValue = value;

        //send event ... for subscriptions
        ChangeOfValue?.Invoke(this, objectId, propertyId, arrayIndex, value);

        return ErrorCodes.Good;
    }

    // Write PROP_PRESENT_VALUE or PROP_RELINQUISH_DEFAULT in an object with a 16 level PROP_PRIORITY_ARRAY (BACNET_APPLICATION_TAG_NULL)
    public ErrorCodes WriteCommandableProperty(BACnetObjectId objectId, BACnetPropertyIds propertyId, BACnetValue value, uint priority)
    {

        if (propertyId != BACnetPropertyIds.PROP_PRESENT_VALUE)
            return ErrorCodes.NotForMe;

        var presentvalue = FindProperty(objectId, BACnetPropertyIds.PROP_PRESENT_VALUE);
        if (presentvalue == null)
            return ErrorCodes.NotForMe;

        var relinquish = FindProperty(objectId, BACnetPropertyIds.PROP_RELINQUISH_DEFAULT);
        if (relinquish == null)
            return ErrorCodes.NotForMe;

        var outOfService = FindProperty(objectId, BACnetPropertyIds.PROP_OUT_OF_SERVICE);
        if (outOfService == null)
            return ErrorCodes.NotForMe;

        var array = FindProperty(objectId, BACnetPropertyIds.PROP_PRIORITY_ARRAY);
        if (array == null)
            return ErrorCodes.NotForMe;

        var errorcode = ErrorCodes.GenericError;

        try
        {
            // If PROP_OUT_OF_SERVICE=True, value is accepted as is : http://www.bacnetwiki.com/wiki/index.php?title=Priority_Array                 
            if ((bool)outOfService.BACnetValue[0].Value && propertyId == BACnetPropertyIds.PROP_PRESENT_VALUE)
            {
                WriteProperty(objectId, BACnetPropertyIds.PROP_PRESENT_VALUE, value);
                return ErrorCodes.Good;
            }

            IList<BACnetValue> valueArray = null;

            // Thank's to Steve Karg
            // The 135-2016 text:
            // 19.2.2 Application Priority Assignments
            // All commandable objects within a device shall be configurable to accept writes to all priorities except priority 6
            if (priority == 6)
                return ErrorCodes.WriteAccessDenied;

            // http://www.chipkin.com/changing-the-bacnet-present-value-or-why-the-present-value-doesn%E2%80%99t-change/
            // Write Property PROP_PRESENT_VALUE : A value is placed in the PROP_PRIORITY_ARRAY
            if (propertyId == BACnetPropertyIds.PROP_PRESENT_VALUE)
            {
                errorcode = ErrorCodes.Good;

                valueArray = array.BACnetValue;
                if (value.Value == null)
                    valueArray[(int)priority - 1] = new BACnetValue(null);
                else
                    valueArray[(int)priority - 1] = value;
                array.BACnetValue = valueArray;
            }

            // Look on the priority Array to find the first value to be set in PROP_PRESENT_VALUE
            if (errorcode == ErrorCodes.Good)
            {

                var done = false;
                for (var i = 0; i < 16; i++)
                {
                    if (valueArray[i].Value == null)
                        continue;

                    WriteProperty(objectId, BACnetPropertyIds.PROP_PRESENT_VALUE, valueArray[i]);
                    done = true;
                    break;
                }

                if (done == false)  // Nothing in the array : PROP_PRESENT_VALUE = PROP_RELINQUISH_DEFAULT
                {
                    var defaultValue = relinquish.BACnetValue;
                    WriteProperty(objectId, BACnetPropertyIds.PROP_PRESENT_VALUE, defaultValue[0]);
                }
            }
        }
        catch
        {
            errorcode = ErrorCodes.GenericError;
        }

        return errorcode;
    }

    public ErrorCodes[] WritePropertyMultiple(BACnetObjectId objectId, ICollection<BACnetPropertyValue> values)
    {
        return values
            .Select(v => WriteProperty(objectId, (BACnetPropertyIds)v.property.propertyIdentifier, v.property.propertyArrayIndex, v.value))
            .ToArray();
    }

    /// <summary>
    /// Store the class, as XML file
    /// </summary>
    /// <param name="path"></param>
    public void Save(string path)
    {
        var s = new XmlSerializer(typeof(DeviceStorage));
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        s.Serialize(fs, this);
    }

    /// <summary>
    /// Load XML values into class
    /// </summary>
    /// <param name="path">Embedded or external file</param>
    /// <param name="deviceId">Optional deviceId other than the one in the Xml file</param>
    /// <returns></returns>
    public static DeviceStorage Load(string path, uint? deviceId = null)
    {
        StreamReader textStreamReader;

        var assembly = Assembly.GetCallingAssembly();

        try
        {
            // check if the xml file is an embedded resource
            textStreamReader = new StreamReader(assembly.GetManifestResourceStream(path));
        }
        catch
        {
            // if not check the external file
            if (!File.Exists(path))
                throw new Exception("No AppSettings found");

            textStreamReader = new StreamReader(path);
        }

        var s = new XmlSerializer(typeof(DeviceStorage));

        using (textStreamReader)
        {
            var ret = (DeviceStorage)s.Deserialize(textStreamReader);

            //set device_id
            var obj = ret.FindObject(BACnetObjectTypes.OBJECT_DEVICE);
            if (obj != null)
                ret.DeviceId = obj.Instance;

            // use the deviceId in the Xml file or another one
            if (!deviceId.HasValue)
                return ret;

            ret.DeviceId = deviceId.Value;
            if (obj == null)
                return ret;

            // change the value
            obj.Instance = deviceId.Value;
            IList<BACnetValue> val = new[]
            {
                    new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, $"OBJECT_DEVICE:{deviceId.Value}")
                };

            ret.WriteProperty(new BACnetObjectId(BACnetObjectTypes.OBJECT_DEVICE,
                Serialize.ASN1.BACNET_MAX_INSTANCE), BACnetPropertyIds.PROP_OBJECT_IDENTIFIER, 1, val, true);

            return ret;
        }
    }
}
