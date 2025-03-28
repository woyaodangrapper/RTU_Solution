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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace System.Net.BACnet;

public delegate void MessageRecievedHandler(IBACnetTransport sender, byte[] buffer, int offset, int msgLength, BACnetAddress remoteAddress);

/// <summary>
/// BACnet network client or server
/// </summary>
public class BACnetClient : IDisposable
{

    private int _retries;
    private int _invokeId;

    private readonly LastSegmentAck _lastSegmentAck = new();
    private uint _writepriority;

    /// <summary>
    /// Dictionary of List of Tuples with sequence-number and byte[] per invoke-id
    /// TODO: invoke-id should be PER (remote) DEVICE!
    /// </summary>
    private Dictionary<byte, List<Tuple<byte, byte[]>>> _segmentsPerInvokeId = new();
    private ConcurrentDictionary<byte, object> _locksPerInvokeId = new();
    private Dictionary<byte, byte> _expectedSegmentsPerInvokeId = new();

    public const int DEFAULT_UDP_PORT = 0xBAC0;
    public const int DEFAULT_TIMEOUT = 1000;
    public const int DEFAULT_RETRIES = 3;

    public IBACnetTransport Transport { get; }
    public ushort VendorId { get; set; } = 260;
    public int Timeout { get; set; }
    public int TransmitTimeout { get; set; } = 30000;
    public BACnetMaxSegments MaxSegments { get; set; } = BACnetMaxSegments.MAX_SEG0;
    public byte ProposedWindowSize { get; set; } = 10;
    public bool ForceWindowSize { get; set; }
    public bool DefaultSegmentationHandling { get; set; } = true;
    public ILogger<BACnetClient> Log { get; }
    /// <summary>
    /// Used as the number of tentatives
    /// </summary>
    public int Retries
    {
        get => _retries;
        set => _retries = Math.Max(1, value);
    }

    public uint WritePriority
    {
        get => _writepriority;
        set { if (value < 17) _writepriority = value; }
    }

    // These members allows to access undecoded buffer by the application
    // layer, when the basic undecoding process is not really able to do the job
    // in particular with application_specific_encoding values
    public byte[] raw_buffer;
    public int raw_offset, raw_length;

    public class Segmentation
    {
        // ReSharper disable InconsistentNaming
        // was public before refactor so can't change this
        public EncodeBuffer buffer;
        public byte sequence_number;
        public byte window_size;
        public byte max_segments;
        // ReSharper restore InconsistentNaming
    }

    private class LastSegmentAck
    {
        private readonly ManualResetEvent _wait = new(false);
        private readonly object _lockObject = new();
        private BACnetAddress _address;
        private byte _invokeId;

        public byte SequenceNumber;
        public byte WindowSize;

        public void Set(BACnetAddress adr, byte invokeId, byte sequenceNumber, byte windowSize)
        {
            lock (_lockObject)
            {
                _address = adr;
                _invokeId = invokeId;
                SequenceNumber = sequenceNumber;
                WindowSize = windowSize;
                _wait.Set();
            }
        }

        public bool Wait(BACnetAddress adr, byte invokeId, int timeout)
        {
            Monitor.Enter(_lockObject);
            while (!adr.Equals(this._address) || this._invokeId != invokeId)
            {
                _wait.Reset();
                Monitor.Exit(_lockObject);
                if (!_wait.WaitOne(timeout)) return false;
                Monitor.Enter(_lockObject);
            }
            Monitor.Exit(_lockObject);
            _address = null;
            return true;
        }
    }

    public BACnetClient(int port = DEFAULT_UDP_PORT, int timeout = DEFAULT_TIMEOUT, int retries = DEFAULT_RETRIES)
        : this(new BACnetIpUdpProtocolTransport(port), timeout, retries)
    {
    }

    public BACnetClient(IBACnetTransport transport, int timeout = DEFAULT_TIMEOUT, int retries = DEFAULT_RETRIES, ILogger<BACnetClient>? logger = null)
    {
        Transport = transport;
        Timeout = timeout;
        Retries = retries;
        Log = logger ?? NullLogger<BACnetClient>.Instance;
    }

    public override bool Equals(object obj)
    {
        return Transport.Equals((obj as BACnetClient)?.Transport);
    }

    public override int GetHashCode()
    {
        return Transport.GetHashCode();
    }

    public override string ToString()
    {
        return Transport.ToString();
    }

    public EncodeBuffer GetEncodeBuffer(int startOffset)
    {
        return new EncodeBuffer(new byte[Transport.MaxBufferLength], startOffset);
    }

    public void Start()
    {
        Transport.Start();
        Transport.MessageRecieved += OnRecieve;
        Log.LogInformation("Started communication");
    }

    public delegate void ConfirmedServiceRequestHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length);
    public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;
    public delegate void ReadPropertyRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetObjectId objectId, BACnetPropertyReference property, BACnetMaxSegments maxSegments);
    public event ReadPropertyRequestHandler OnReadPropertyRequest;
    public delegate void ReadPropertyMultipleRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, IList<BACnetReadAccessSpecification> properties, BACnetMaxSegments maxSegments);
    public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
    public delegate void WritePropertyRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetObjectId objectId, BACnetPropertyValue value, BACnetMaxSegments maxSegments);
    public event WritePropertyRequestHandler OnWritePropertyRequest;
    public delegate void WritePropertyMultipleRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetObjectId objectId, ICollection<BACnetPropertyValue> values, BACnetMaxSegments maxSegments);
    public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;
    public delegate void AtomicWriteFileRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, bool isStream, BACnetObjectId objectId, int position, uint blockCount, byte[][] blocks, int[] counts, BACnetMaxSegments maxSegments);
    public event AtomicWriteFileRequestHandler OnAtomicWriteFileRequest;
    public delegate void AtomicReadFileRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, bool isStream, BACnetObjectId objectId, int position, uint count, BACnetMaxSegments maxSegments);
    public event AtomicReadFileRequestHandler OnAtomicReadFileRequest;
    public delegate void SubscribeCOVRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BACnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BACnetMaxSegments maxSegments);
    public event SubscribeCOVRequestHandler OnSubscribeCOV;
    public delegate void EventNotificationCallbackHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetEventNotificationData eventData, bool needConfirm);
    public event EventNotificationCallbackHandler OnEventNotify;
    public delegate void SubscribeCOVPropertyRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BACnetObjectId monitoredObjectIdentifier, BACnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement, BACnetMaxSegments maxSegments);
    public event SubscribeCOVPropertyRequestHandler OnSubscribeCOVProperty;
    public delegate void DeviceCommunicationControlRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, uint timeDuration, uint enableDisable, string password, BACnetMaxSegments maxSegments);
    public event DeviceCommunicationControlRequestHandler OnDeviceCommunicationControl;
    public delegate void ReinitializedRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetReinitializedStates state, string password, BACnetMaxSegments maxSegments);
    public event ReinitializedRequestHandler OnReinitializedDevice;
    public delegate void ReadRangeHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetObjectId objectId, BACnetPropertyReference property, BACnetReadRangeRequestTypes requestType, uint position, DateTime time, int count, BACnetMaxSegments maxSegments);
    public event ReadRangeHandler OnReadRange;
    public delegate void CreateObjectRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetObjectId objectId, ICollection<BACnetPropertyValue> values, BACnetMaxSegments maxSegments);
    public event CreateObjectRequestHandler OnCreateObjectRequest;
    public delegate void DeleteObjectRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, BACnetObjectId objectId, BACnetMaxSegments maxSegments);
    public event DeleteObjectRequestHandler OnDeleteObjectRequest;
    public delegate void GetAlarmSummaryOrEventInformationRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, bool getEvent, BACnetObjectId objectId, BACnetMaxAdpu maxApdu, BACnetMaxSegments max_segments);
    public event GetAlarmSummaryOrEventInformationRequestHandler OnGetAlarmSummaryOrEventInformation;
    public delegate void AlarmAcknowledgeRequestHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, uint ackProcessIdentifier, BACnetObjectId eventObjectIdentifier, uint eventStateAcked, string ackSource, BACnetGenericTime eventTimeStamp, BACnetGenericTime ackTimeStamp);
    public event AlarmAcknowledgeRequestHandler OnAlarmAcknowledge;

    protected void ProcessConfirmedServiceRequest(BACnetAddress address, BACnetPduTypes type, BACnetConfirmedServices service, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"ConfirmedServiceRequest {service}");

            raw_buffer = buffer;
            raw_length = length;
            raw_offset = offset;

            OnConfirmedServiceRequest?.Invoke(this, address, type, service, maxSegments, maxAdpu, invokeId, buffer, offset, length);

            //don't send segmented messages, if client don't want it
            if ((type & BACnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED) == 0)
                maxSegments = BACnetMaxSegments.MAX_SEG0;

            if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY && OnReadPropertyRequest != null)
            {
                int thsRejectReason;

                if ((thsRejectReason = Services.DecodeReadProperty(buffer, offset, length, out var objectId, out var property)) >= 0)
                {
                    OnReadPropertyRequest(this, address, invokeId, objectId, property, maxSegments);
                }
                else
                {
                    switch (thsRejectReason)
                    {
                        case -1:
                            SendConfirmedServiceReject(address, invokeId, BACnetRejectReason.MISSING_REQUIRED_PARAMETER);
                            break;
                        case -2:
                            SendConfirmedServiceReject(address, invokeId, BACnetRejectReason.INVALID_TAG);
                            break;
                        case -3:
                            SendConfirmedServiceReject(address, invokeId, BACnetRejectReason.TOO_MANY_ARGUMENTS);
                            break;
                    }
                    Log.LogWarning("Couldn't decode DecodeReadProperty");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
            {
                if (Services.DecodeWriteProperty(address, buffer, offset, length, out var objectId, out var value) >= 0)
                    OnWritePropertyRequest(this, address, invokeId, objectId, value, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    //SendConfirmedServiceReject(adr, invokeId, BACnetRejectReason.OTHER); 
                    Log.LogWarning("Couldn't decode DecodeWriteProperty");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
            {
                if (Services.DecodeReadPropertyMultiple(buffer, offset, length, out var properties) >= 0)
                    OnReadPropertyMultipleRequest(this, address, invokeId, properties, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DecodeReadPropertyMultiple");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
            {
                if (Services.DecodeWritePropertyMultiple(address, buffer, offset, length, out var objectId, out var values) >= 0)
                    OnWritePropertyMultipleRequest(this, address, invokeId, objectId, values, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DecodeWritePropertyMultiple");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
            {
                if (Services.DecodeCOVNotifyUnconfirmed(address, buffer, offset, length, out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier, out var monitoredObjectIdentifier, out var timeRemaining, out var values) >= 0)
                    OnCOVNotification(this, address, invokeId, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, true, values, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode COVNotify");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE && OnAtomicWriteFileRequest != null)
            {
                if (Services.DecodeAtomicWriteFile(buffer, offset, length, out var isStream, out var objectId, out var position, out var blockCount, out var blocks, out var counts) >= 0)
                    OnAtomicWriteFileRequest(this, address, invokeId, isStream, objectId, position, blockCount, blocks, counts, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode AtomicWriteFile");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE && OnAtomicReadFileRequest != null)
            {
                if (Services.DecodeAtomicReadFile(buffer, offset, length, out var isStream, out var objectId, out var position, out var count) >= 0)
                    OnAtomicReadFileRequest(this, address, invokeId, isStream, objectId, position, count, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode AtomicReadFile");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV && OnSubscribeCOV != null)
            {
                if (Services.DecodeSubscribeCOV(buffer, offset, length, out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime) >= 0)
                    OnSubscribeCOV(this, address, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode SubscribeCOV");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY && OnSubscribeCOVProperty != null)
            {
                if (Services.DecodeSubscribeProperty(buffer, offset, length, out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var monitoredProperty, out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime, out var covIncrement) >= 0)
                    OnSubscribeCOVProperty(this, address, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode SubscribeCOVProperty");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL && OnDeviceCommunicationControl != null)
            {
                // DAL
                if (Services.DecodeDeviceCommunicationControl(buffer, offset, length, out var timeDuration, out var enableDisable, out var password) >= 0)
                    OnDeviceCommunicationControl(this, address, invokeId, timeDuration, enableDisable, password, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DeviceCommunicationControl");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE && OnReinitializedDevice != null)
            {
                // DAL
                if (Services.DecodeReinitializeDevice(buffer, offset, length, out var state, out var password) >= 0)
                    OnReinitializedDevice(this, address, invokeId, state, password, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode ReinitializeDevice");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION && OnEventNotify != null) // F. Chaxel
            {
                if (Services.DecodeEventNotifyData(buffer, offset, length, out var eventData) >= 0)
                {
                    OnEventNotify(this, address, invokeId, eventData, true);
                }
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode confirmed Event/Alarm Notification");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE && OnReadRange != null)
            {
                if (Services.DecodeReadRange(buffer, offset, length, out var objectId, out var property, out var requestType, out var position, out var time, out var count) >= 0)
                    OnReadRange(this, address, invokeId, objectId, property, requestType, position, time, count, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode ReadRange");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT && OnCreateObjectRequest != null)
            {
                if (Services.DecodeCreateObject(address, buffer, offset, length, out var objectId, out var values) >= 0)
                    OnCreateObjectRequest(this, address, invokeId, objectId, values, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode CreateObject");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT && OnDeleteObjectRequest != null)
            {
                if (Services.DecodeDeleteObject(buffer, offset, length, out var objectId) >= 0)
                    OnDeleteObjectRequest(this, address, invokeId, objectId, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DecodeDeleteObject");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY && OnGetAlarmSummaryOrEventInformation != null)
            {
                // DAL -- added the core code required but since I couldn't test it we just reject this service
                // rejecting it shouldn't be too bad a thing since GetAlarmSummary has been retired anyway...
                // if someone needs it they can uncomment the related code and test.
#if false
                    BACnetObjectId objectId = default(BACnetObjectId);
                    objectId.Type = BACnetObjectTypes.MAX_BACNET_OBJECT_TYPE;
                    if (Services.DecodeAlarmSummaryOrEventRequest(buffer, offset, length, false, ref objectId) >= 0)
                    {
                        OnGetAlarmSummaryOrEventInformation(this, address, invokeId, false, objectId, maxAdpu, maxSegments);
                    }
                    else
                    {
                        // DAL
                        SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                        //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.LogWarning("Couldn't decode GetAlarmSummary");
                    }
#else
                SendConfirmedServiceReject(address, invokeId, BACnetRejectReason.RECOGNIZED_SERVICE); // should be unrecognized but this is the way it was spelled..
#endif
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION && OnGetAlarmSummaryOrEventInformation != null)
            {
                // DAL
                BACnetObjectId objectId = default;
                objectId.Type = BACnetObjectTypes.MAX_BACNET_OBJECT_TYPE;
                if (Services.DecodeAlarmSummaryOrEventRequest(buffer, offset, length, true, ref objectId) >= 0)
                {
                    OnGetAlarmSummaryOrEventInformation(this, address, invokeId, true, objectId, maxAdpu, maxSegments);
                }
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode GetEventInformation");
                }
            }
            else if (service == BACnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM && OnAlarmAcknowledge != null)
            {
                // DAL
                if (Services.DecodeAlarmAcknowledge(buffer, offset, length, out uint ackProcessIdentifier, out BACnetObjectId eventObjectIdentifier, out uint eventStateAcked, out string ackSource, out BACnetGenericTime eventTimeStamp, out BACnetGenericTime ackTimeStamp) >= 0)
                {
                    OnAlarmAcknowledge(this, address, invokeId, ackProcessIdentifier, eventObjectIdentifier, eventStateAcked, ackSource, eventTimeStamp, ackTimeStamp);
                }
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BACnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode AlarmAcknowledge");
                }
            }
            else
            {
                // DAL
                SendConfirmedServiceReject(address, invokeId, BACnetRejectReason.RECOGNIZED_SERVICE); // should be unrecognized but this is the way it was spelled..
                Log.LogDebug($"Confirmed service not handled: {service}");
            }
        }
        catch (Exception ex)
        {
            // DAL
            SendAbort(address, invokeId, BACnetAbortReason.OTHER);
            //ErrorResponse(address, service, invokeId, BACnetErrorClasses.ERROR_CLASS_DEVICE, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
            Log.LogError("Error in ProcessConfirmedServiceRequest", ex);
        }
    }

    public delegate void UnconfirmedServiceRequestHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetUnconfirmedServices service, byte[] buffer, int offset, int length);
    public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
    public delegate void WhoHasHandler(BACnetClient sender, BACnetAddress adr, int lowLimit, int highLimit, BACnetObjectId? objId, string objName);
    public event WhoHasHandler OnWhoHas;
    public delegate void IamHandler(BACnetClient sender, BACnetAddress adr, uint deviceId, uint maxAPDU, BACnetSegmentations segmentation, ushort vendorId);
    public event IamHandler OnIam;
    public delegate void WhoIsHandler(BACnetClient sender, BACnetAddress adr, int lowLimit, int highLimit);
    public event WhoIsHandler OnWhoIs;
    public delegate void TimeSynchronizeHandler(BACnetClient sender, BACnetAddress adr, DateTime dateTime, bool utc);
    public event TimeSynchronizeHandler OnTimeSynchronize;

    //used by both 'confirmed' and 'unconfirmed' notify
    public delegate void COVNotificationHandler(BACnetClient sender, BACnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BACnetObjectId initiatingDeviceIdentifier, BACnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool needConfirm, ICollection<BACnetPropertyValue> values, BACnetMaxSegments maxSegments);
    public event COVNotificationHandler OnCOVNotification;

    protected void ProcessUnconfirmedServiceRequest(BACnetAddress address, BACnetPduTypes type, BACnetUnconfirmedServices service, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug("UnconfirmedServiceRequest");
            OnUnconfirmedServiceRequest?.Invoke(this, address, type, service, buffer, offset, length);
            if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
            {
                if (Services.DecodeIamBroadcast(buffer, offset, out var deviceId, out var maxAdpu, out var segmentation, out var vendorId) >= 0)
                    OnIam(this, address, deviceId, maxAdpu, segmentation, vendorId);
                else
                    Log.LogWarning("Couldn't decode IamBroadcast");
            }
            else if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
            {
                if (Services.DecodeWhoIsBroadcast(buffer, offset, length, out var lowLimit, out var highLimit) >= 0)
                    OnWhoIs(this, address, lowLimit, highLimit);
                else
                    Log.LogWarning("Couldn't decode WhoIsBroadcast");
            }
            // added by thamersalek
            else if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS && OnWhoHas != null)
            {
                if (Services.DecodeWhoHasBroadcast(buffer, offset, length, out var lowLimit, out var highLimit, out var objId, out var objName) >= 0)
                    OnWhoHas(this, address, lowLimit, highLimit, objId, objName);
                else
                    Log.LogWarning("Couldn't decode WhoHasBroadcast");
            }
            else if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
            {
                if (Services.DecodeCOVNotifyUnconfirmed(address, buffer, offset, length, out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier, out var monitoredObjectIdentifier, out var timeRemaining, out var values) >= 0)
                    OnCOVNotification(this, address, 0, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, false, values, BACnetMaxSegments.MAX_SEG0);
                else
                    Log.LogWarning("Couldn't decode COVNotifyUnconfirmed");
            }
            else if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
            {
                if (Services.DecodeTimeSync(buffer, offset, length, out var dateTime) >= 0)
                    OnTimeSynchronize(this, address, dateTime, false);
                else
                    Log.LogWarning("Couldn't decode TimeSynchronize");
            }
            else if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
            {
                if (Services.DecodeTimeSync(buffer, offset, length, out var dateTime) >= 0)
                    OnTimeSynchronize(this, address, dateTime, true);
                else
                    Log.LogWarning("Couldn't decode TimeSynchronize");
            }
            else if (service == BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION && OnEventNotify != null) // F. Chaxel
            {
                if (Services.DecodeEventNotifyData(buffer, offset, length, out var eventData) >= 0)
                    OnEventNotify(this, address, 0, eventData, false);
                else
                    Log.LogWarning("Couldn't decode unconfirmed Event/Alarm Notification");
            }
            else
            {
                Log.LogDebug($"Unconfirmed service not handled: {service}");
                // SendUnConfirmedServiceReject(adr); ? exists ?
            }
        }
        catch (Exception ex)
        {
            Log.LogError("Error in ProcessUnconfirmedServiceRequest", ex);
        }
    }

    public delegate void SimpleAckHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength);
    public event SimpleAckHandler OnSimpleAck;

    protected void ProcessSimpleAck(BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received SimpleAck for {service}");
            OnSimpleAck?.Invoke(this, adr, type, service, invokeId, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError("Error in ProcessSimpleAck", ex);
        }
    }

    public delegate void ComplexAckHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length);
    public event ComplexAckHandler OnComplexAck;

    protected void ProcessComplexAck(BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received ComplexAck for {service}");
            OnComplexAck?.Invoke(this, adr, type, service, invokeId, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError($"Error in {nameof(ProcessComplexAck)}", ex);
        }
    }

    public delegate void ErrorHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, BACnetErrorClasses errorClass, BACnetErrorCodes errorCode, byte[] buffer, int offset, int length);
    public event ErrorHandler OnError;

    protected void ProcessError(BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            if (Services.DecodeError(buffer, offset, out var errorClass, out var errorCode) < 0)
            {
                Log.LogWarning("Couldn't decode received Error");
                return;
            }

            Log.LogDebug($"Received Error {errorClass} {errorCode}");
            OnError?.Invoke(this, adr, type, service, invokeId, errorClass, errorCode, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError($"Error in {nameof(ProcessError)}", ex);
        }
    }

    public delegate void AbortHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, byte invokeId, BACnetAbortReason reason, byte[] buffer, int offset, int length);
    public event AbortHandler OnAbort;

    protected void ProcessAbort(BACnetAddress adr, BACnetPduTypes type, byte invokeId, BACnetAbortReason reason, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received Abort, reason: {reason}");
            OnAbort?.Invoke(this, adr, type, invokeId, reason, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError("Error in ProcessAbort", ex);
        }
    }

    public delegate void RejectHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, byte invokeId, BACnetRejectReason reason, byte[] buffer, int offset, int length);
    public event RejectHandler OnReject;

    protected void ProcessReject(BACnetAddress adr, BACnetPduTypes type, byte invokeId, BACnetRejectReason reason, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received Reject, reason: {reason}");
            OnReject?.Invoke(this, adr, type, invokeId, reason, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError("Error in ProcessReject", ex);
        }
    }

    public delegate void SegmentAckHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length);
    public event SegmentAckHandler OnSegmentAck;

    protected void ProcessSegmentAck(BACnetAddress adr, BACnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug("Received SegmentAck");
            OnSegmentAck?.Invoke(this, adr, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError("Error in ProcessSegmentAck", ex);
        }
    }

    public delegate void SegmentHandler(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length);
    public event SegmentHandler OnSegment;

    private void ProcessSegment(BACnetAddress address, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, bool server, byte sequenceNumber, byte proposedWindowNumber, byte[] buffer, int offset, int length)
    {
        lock (_locksPerInvokeId.GetOrAdd(invokeId, () => new object()))
        {
            ProcessSegmentLocked(address, type, service, invokeId, maxSegments, maxAdpu, server, sequenceNumber,
                proposedWindowNumber, buffer, offset, length);
        }
    }

    private void ProcessSegmentLocked(BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service,
        byte invokeId, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, bool server, byte sequenceNumber,
        byte proposedWindowNumber, byte[] buffer, int offset, int length)
    {
        Log.LogTrace($@"Processing Segment #{sequenceNumber} of invoke-id #{invokeId}");

        if (!_segmentsPerInvokeId.ContainsKey(invokeId))
            _segmentsPerInvokeId[invokeId] = new List<Tuple<byte, byte[]>>();

        if (!_expectedSegmentsPerInvokeId.ContainsKey(invokeId))
            _expectedSegmentsPerInvokeId[invokeId] = byte.MaxValue;

        var moreFollows = (type & BACnetPduTypes.MORE_FOLLOWS) == BACnetPduTypes.MORE_FOLLOWS;

        if (!moreFollows)
            _expectedSegmentsPerInvokeId[invokeId] = (byte)(sequenceNumber + 1);

        //send ACK
        if (sequenceNumber % proposedWindowNumber == 0 || !moreFollows)
        {
            if (ForceWindowSize)
                proposedWindowNumber = ProposedWindowSize;

            SegmentAckResponse(adr, false, server, invokeId, sequenceNumber, proposedWindowNumber);
        }

        //Send on
        OnSegment?.Invoke(this, adr, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, buffer, offset, length);

        //default segment assembly. We run this seperately from the above handler, to make sure that it comes after!
        if (DefaultSegmentationHandling)
            PerformDefaultSegmentHandling(adr, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, buffer, offset, length);
    }

    /// <summary>
    /// This is a simple handling that stores all segments in memory and assembles them when done
    /// </summary>
    private void PerformDefaultSegmentHandling(BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length)
    {
        var segments = _segmentsPerInvokeId[invokeId];

        if (sequenceNumber == 0)
        {
            //copy buffer + encode new adpu header
            type &= ~BACnetPduTypes.SEGMENTED_MESSAGE;
            var confirmedServiceRequest = (type & BACnetPduTypes.PDU_TYPE_MASK) == BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST;
            var adpuHeaderLen = confirmedServiceRequest ? 4 : 3;

            var copy = new byte[length + adpuHeaderLen];
            Array.Copy(buffer, offset, copy, adpuHeaderLen, length);
            var encodedBuffer = new EncodeBuffer(copy, 0);

            if (confirmedServiceRequest)
                APDU.EncodeConfirmedServiceRequest(encodedBuffer, type, service, maxSegments, maxAdpu, invokeId);
            else
                APDU.EncodeComplexAck(encodedBuffer, type, service, invokeId);

            segments.Add(Tuple.Create(sequenceNumber, copy)); // doesn't include BVLC or NPDU
        }
        else
        {
            //copy only content part
            segments.Add(Tuple.Create(sequenceNumber, buffer.Skip(offset).Take(length).ToArray()));
        }

        //process when finished
        if (segments.Count < _expectedSegmentsPerInvokeId[invokeId])
            return;

        //assemble whole part
        var apduBuffer = segments.OrderBy(s => s.Item1).SelectMany(s => s.Item2).ToArray();
        segments.Clear();
        _expectedSegmentsPerInvokeId[invokeId] = byte.MaxValue;

        //process
        ProcessApdu(adr, type, apduBuffer, 0, apduBuffer.Length);
    }

    private void ProcessApdu(BACnetAddress adr, BACnetPduTypes type, byte[] buffer, int offset, int length)
    {
        switch (type & BACnetPduTypes.PDU_TYPE_MASK)
        {
            case BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                {
                    var apduHeaderLen = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out type, out var service);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessUnconfirmedServiceRequest(adr, type, service, buffer, offset, length);
                }
                break;

            case BACnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                {
                    var apduHeaderLen = APDU.DecodeSimpleAck(buffer, offset, out type, out var service, out var invokeId);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessSimpleAck(adr, type, service, invokeId, buffer, offset, length);
                }
                break;

            case BACnetPduTypes.PDU_TYPE_COMPLEX_ACK:
                {
                    var apduHeaderLen = APDU.DecodeComplexAck(buffer, offset, out type, out var service, out var invokeId,
                        out var sequenceNumber, out var proposedWindowNumber);

                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    if ((type & BACnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                    {
                        ProcessComplexAck(adr, type, service, invokeId, buffer, offset, length);
                    }
                    else
                    {
                        ProcessSegment(adr, type, service, invokeId, BACnetMaxSegments.MAX_SEG0, BACnetMaxAdpu.MAX_APDU50, false,
                            sequenceNumber, proposedWindowNumber, buffer, offset, length);
                    }
                }
                break;

            case BACnetPduTypes.PDU_TYPE_SEGMENT_ACK:
                {
                    var apduHeaderLen = APDU.DecodeSegmentAck(buffer, offset, out type, out var originalInvokeId,
                        out var sequenceNumber, out var actualWindowSize);

                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    _lastSegmentAck.Set(adr, originalInvokeId, sequenceNumber, actualWindowSize);
                    ProcessSegmentAck(adr, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
                }
                break;

            case BACnetPduTypes.PDU_TYPE_ERROR:
                {
                    var apduHeaderLen = APDU.DecodeError(buffer, offset, out type, out var service, out var invokeId);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessError(adr, type, service, invokeId, buffer, offset, length);
                }
                break;

            case BACnetPduTypes.PDU_TYPE_ABORT:
                {
                    var apduHeaderLen = APDU.DecodeAbort(buffer, offset, out type, out var invokeId, out var reason);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessAbort(adr, type, invokeId, reason, buffer, offset, length);
                }
                break;

            case BACnetPduTypes.PDU_TYPE_REJECT:
                {
                    var apduHeaderLen = APDU.DecodeReject(buffer, offset, out type, out var invokeId, out var reason);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessReject(adr, type, invokeId, reason, buffer, offset, length);
                }
                break;

            case BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                {
                    var apduHeaderLen = APDU.DecodeConfirmedServiceRequest(buffer, offset, out type, out var service,
                        out var maxSegments, out var maxAdpu, out var invokeId, out var sequenceNumber, out var proposedWindowNumber);

                    offset += apduHeaderLen;
                    length -= apduHeaderLen;

                    if ((type & BACnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                    {
                        ProcessConfirmedServiceRequest(adr, type, service, maxSegments, maxAdpu, invokeId, buffer, offset, length);
                    }
                    else
                    {
                        ProcessSegment(adr, type, service, invokeId, maxSegments, maxAdpu, true, sequenceNumber, proposedWindowNumber, buffer, offset, length);
                    }
                }
                break;

            default:
                Log.LogWarning($"Something else arrived: {type}");
                break;
        }
    }

    // DAL
    public void SendNetworkMessage(BACnetAddress adr, byte[] buffer, int bufLen, BACnetNetworkMessageTypes messageType, ushort vendorId = 0)
    {
        if (adr == null)
        {
            adr = Transport.GetBroadcastAddress();
        }
        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BACnetNpduControls.NetworkLayerMessage, adr, null, 255, messageType, vendorId);
        b.Add(buffer, bufLen);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }
    public void SendIAmRouterToNetwork(ushort[] networks)
    {
        var b = GetEncodeBuffer(0);
        for (int i = 0; i < networks.Length; i++)
        {
            ASN1.encode_unsigned16(b, networks[i]);
        }
        SendNetworkMessage(null, b.buffer, b.offset, BACnetNetworkMessageTypes.NETWORK_MESSAGE_I_AM_ROUTER_TO_NETWORK);
    }

    public void SendInitializeRoutingTableAck(BACnetAddress adr, ushort[] networks)
    {
        var b = GetEncodeBuffer(0);
        if (networks != null)
        {
            for (int i = 0; i < networks.Length; i++)
            {
                ASN1.encode_unsigned16(b, networks[i]);
            }
        }
        SendNetworkMessage(adr, b.buffer, b.offset, BACnetNetworkMessageTypes.NETWORK_MESSAGE_INIT_RT_TABLE_ACK);
    }
    public void SendRejectToNetwork(BACnetAddress adr, ushort[] networks)
    {
        var b = GetEncodeBuffer(0);
        /* Sending our DNET doesn't make a lot of sense, does it? */
        for (int i = 0; i < networks.Length; i++)
        {
            ASN1.encode_unsigned16(b, networks[i]);
        }
        SendNetworkMessage(adr, b.buffer, b.offset, BACnetNetworkMessageTypes.NETWORK_MESSAGE_REJECT_MESSAGE_TO_NETWORK);
    }
    public delegate void NetworkMessageHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, BACnetNetworkMessageTypes npduMessageType, byte[] buffer, int offset, int messageLength);
    public event NetworkMessageHandler OnNetworkMessage;
    public delegate void WhoIsRouterToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event WhoIsRouterToNetworkHandler OnWhoIsRouterToNetworkMessage;
    public delegate void IAmRouterToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event IAmRouterToNetworkHandler OnIAmRouterToNetworkMessage;
    public delegate void ICouldBeRouterToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event ICouldBeRouterToNetworkHandler OnICouldBeRouterToNetworkMessage;
    public delegate void RejectMessageToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event RejectMessageToNetworkHandler OnRejectMessageToNetworkMessage;
    public delegate void RouterBusyToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event RouterBusyToNetworkHandler OnRouterBusyToNetworkMessage;
    public delegate void RouterAvailableToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event RouterAvailableToNetworkHandler OnRouterAvailableToNetworkMessage;
    public delegate void InitRtTableToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event InitRtTableToNetworkHandler OnInitRtTableToNetworkMessage;
    public delegate void InitRtTableAckToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event InitRtTableAckToNetworkHandler OnInitRtTableAckToNetworkMessage;
    public delegate void EstablishConnectionToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event EstablishConnectionToNetworkHandler OnEstablishConnectionToNetworkMessage;
    public delegate void DisconnectConnectionToNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event DisconnectConnectionToNetworkHandler OnDisconnectConnectionToNetworkMessage;
    public delegate void UnrecognizedNetworkHandler(BACnetClient sender, BACnetAddress adr, BACnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event UnrecognizedNetworkHandler OnUnrecognizedNetworkMessage;

    private void ProcessNetworkMessage(BACnetAddress adr, BACnetNpduControls npduFunction, BACnetNetworkMessageTypes npduMessageType, byte[] buffer, int offset, int messageLength)
    {
        // DAL I don't want to make a generic router, but I do want to put in enough infrastructure
        // that I can build on it to route multiple devices in the normal bacnet way.
        OnNetworkMessage?.Invoke(this, adr, npduFunction, npduMessageType, buffer, offset, messageLength);
        switch (npduMessageType)
        {
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK:
                OnWhoIsRouterToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_I_AM_ROUTER_TO_NETWORK:
                OnIAmRouterToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_I_COULD_BE_ROUTER_TO_NETWORK:
                OnICouldBeRouterToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_REJECT_MESSAGE_TO_NETWORK:
                OnRejectMessageToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_ROUTER_BUSY_TO_NETWORK:
                OnRouterBusyToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_ROUTER_AVAILABLE_TO_NETWORK:
                OnRouterAvailableToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_INIT_RT_TABLE:
                OnInitRtTableToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_INIT_RT_TABLE_ACK:
                OnInitRtTableAckToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_ESTABLISH_CONNECTION_TO_NETWORK:
                OnEstablishConnectionToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BACnetNetworkMessageTypes.NETWORK_MESSAGE_DISCONNECT_CONNECTION_TO_NETWORK:
                OnDisconnectConnectionToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            default:
                /* An unrecognized message is bad; send an error response. */
                OnUnrecognizedNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
        }
    }

    private void OnRecieve(IBACnetTransport sender, byte[] buffer, int offset, int msgLength, BACnetAddress remoteAddress)
    {
        try
        {
            if (Transport == null)
                return; //we're disposed 

            if (msgLength <= 0)
                return;

            // parse
            var npduLen = NPDU.Decode(buffer, offset, out var npduFunction, out var destination, out var source, out _, out var npduMessageType, out _);

            // Modif FC
            remoteAddress.RoutedSource = source;

            // DAL
            remoteAddress.RoutedDestination = destination;

            if (npduLen <= 0)
                return;

            offset += npduLen;
            msgLength -= npduLen;

            if (msgLength < 0) // could be 0 for an already parsed
                return;

            if (npduFunction.HasFlag(BACnetNpduControls.NetworkLayerMessage))
            {
                Log.LogInformation("Network Layer message received");
                // DAL
                ProcessNetworkMessage(remoteAddress, npduFunction, npduMessageType, buffer, offset, msgLength);
                return;
            }

            if (msgLength <= 0)
                return;

            var apduType = APDU.GetDecodedType(buffer, offset);
            ProcessApdu(remoteAddress, apduType, buffer, offset, msgLength);
        }
        catch (Exception ex)
        {
            Log.LogError("Error in OnRecieve", ex);
        }
    }

    // Modif FC
    public void RegisterAsForeignDevice(string bbmdIp, short ttl, int port = DEFAULT_UDP_PORT)
    {
        try
        {
            var ep = new IPEndPoint(IPAddress.Parse(bbmdIp), port);
            var sent = false;

            switch (Transport)
            {
                case BACnetIpUdpProtocolTransport t:
                    sent = t.SendRegisterAsForeignDevice(ep, ttl);
                    break;

                case BACnetIpV6UdpProtocolTransport t:
                    sent = t.SendRegisterAsForeignDevice(ep, ttl);
                    break;
            }

            if (sent)
                Log.LogDebug($"Sending Register as a Foreign Device to {bbmdIp}");
            else
                Log.LogWarning("The given address do not match with the IP version");
        }
        catch (Exception ex)
        {
            Log.LogError("Error on RegisterAsForeignDevice (Wrong Transport, not IP ?)", ex);
        }
    }

    public void RemoteWhoIs(string bbmdIP, int port = DEFAULT_UDP_PORT, int lowLimit = -1, int highLimit = -1, BACnetAddress source = null)
    {
        try
        {
            var ep = new IPEndPoint(IPAddress.Parse(bbmdIP), port);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            var broadcast = Transport.GetBroadcastAddress();
            NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, broadcast, source);
            APDU.EncodeUnconfirmedServiceRequest(b, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
            Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

            var sent = false;

            switch (Transport)
            {
                case BACnetIpUdpProtocolTransport t:
                    sent = t.SendRemoteWhois(b.buffer, ep, b.offset);
                    break;

                case BACnetIpV6UdpProtocolTransport t:
                    sent = t.SendRemoteWhois(b.buffer, ep, b.offset);
                    break;
            }

            if (sent)
                Log.LogDebug($"Sending Remote Whois to {bbmdIP}");
            else
                Log.LogWarning("The given address do not match with the IP version");
        }
        catch (Exception ex)
        {
            Log.LogError("Error on Sending Whois to remote BBMD (Wrong Transport, not IP ?)", ex);
        }

    }

    public void WhoIs(int lowLimit = -1, int highLimit = -1, BACnetAddress receiver = null, BACnetAddress source = null)
    {
        if (receiver == null)
        {
            // _receiver could be an unicast @ : for direct acces 
            // usefull on BIP for a known IP:Port, unknown device Id
            receiver = Transport.GetBroadcastAddress();
            Log.LogDebug("Broadcasting WhoIs");
        }
        else
        {
            Log.LogDebug($"Sending WhoIs to {receiver}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, receiver, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
        Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    public void Iam(uint deviceId, BACnetSegmentations segmentation = BACnetSegmentations.SEGMENTATION_BOTH, BACnetAddress receiver = null, BACnetAddress source = null)
    {
        if (receiver == null)
        {
            receiver = Transport.GetBroadcastAddress();
            Log.LogDebug($"Broadcasting Iam {deviceId}");
        }
        else
        {
            Log.LogDebug($"Sending Iam {deviceId} to {receiver}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, receiver, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
        Services.EncodeIamBroadcast(b, deviceId, (uint)GetMaxApdu(), segmentation, VendorId);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    public void WhoHas(BACnetObjectId objId, int lowLimit = -1, int highLimit = -1, BACnetAddress receiver = null, BACnetAddress source = null)
    {
        WhoHasCore(objId, null, lowLimit, highLimit, receiver, source);
    }

    public void WhoHas(string objName, int lowLimit = -1, int highLimit = -1, BACnetAddress receiver = null, BACnetAddress source = null)
    {
        WhoHasCore(null, objName, lowLimit, highLimit, receiver, source);
    }

    private void WhoHasCore(BACnetObjectId? objId, string objName, int lowLimit, int highLimit, BACnetAddress receiver, BACnetAddress source)
    {
        if (receiver == null)
        {
            receiver = Transport.GetBroadcastAddress();
            Log.LogDebug($"Broadcasting WhoHas {objId?.ToString() ?? objName}");
        }
        else
        {
            Log.LogDebug($"Sending WhoHas {objId?.ToString() ?? objName} to {receiver}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, receiver, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS);
        Services.EncodeWhoHasBroadcast(b, lowLimit, highLimit, objId, objName);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    // ReSharper disable once InconsistentNaming
    public void IHave(BACnetObjectId deviceId, BACnetObjectId objId, string objName, BACnetAddress source = null)
    {
        Log.LogDebug($"Broadcasting IHave {objName} {objId}");

        var b = GetEncodeBuffer(Transport.HeaderLength);
        var broadcast = Transport.GetBroadcastAddress();
        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, broadcast, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_HAVE);
        Services.EncodeIhaveBroadcast(b, deviceId, objId, objName);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, broadcast, false, 0);
    }

    public void SendUnconfirmedEventNotification(BACnetAddress adr, BACnetEventNotificationData eventData, BACnetAddress source = null)
    {
        Log.LogDebug($"Sending Event Notification {eventData.eventType} {eventData.eventObjectIdentifier}");

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, adr, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);
        Services.EncodeEventNotifyUnconfirmed(b, eventData);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SendConfirmedServiceReject(BACnetAddress adr, byte invokeId, BACnetRejectReason reason)
    {
        Log.LogDebug($"Sending Service reject: {reason}");

        var b = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(b, BACnetPduTypes.PDU_TYPE_REJECT, (BACnetConfirmedServices)reason, invokeId);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SendAbort(BACnetAddress adr, byte invokeId, BACnetAbortReason reason)
    {
        // DAL
        Log.LogDebug($"Sending Service reject: {reason}");

        var b = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(b, BACnetPduTypes.PDU_TYPE_ABORT, (BACnetConfirmedServices)reason, invokeId);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SynchronizeTime(BACnetAddress adr, DateTime dateTime, BACnetAddress source = null)
    {
        Log.LogDebug($"Sending Time Synchronize: {dateTime} {dateTime.Kind.ToString().ToUpper()}");

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr, source);
        APDU.EncodeUnconfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, dateTime.Kind == DateTimeKind.Utc
                ? BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION
                : BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);
        Services.EncodeTimeSync(buffer, dateTime);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }


    public int GetMaxApdu()
    {
        return GetMaxApdu(Transport.MaxAdpuLength);
    }
    // DAL
    public int GetMaxApdu(BACnetMaxAdpu apduLength)
    {
        int maxAPDU;
        switch (apduLength)
        {
            case BACnetMaxAdpu.MAX_APDU1476:
                maxAPDU = 1476;
                break;
            case BACnetMaxAdpu.MAX_APDU1024:
                maxAPDU = 1024;
                break;
            case BACnetMaxAdpu.MAX_APDU480:
                maxAPDU = 480;
                break;
            case BACnetMaxAdpu.MAX_APDU206:
                maxAPDU = 206;
                break;
            case BACnetMaxAdpu.MAX_APDU128:
                maxAPDU = 128;
                break;
            case BACnetMaxAdpu.MAX_APDU50:
                maxAPDU = 50;
                break;
            default:
                throw new NotImplementedException();
        }

        //max udp payload IRL seems to differ from the expectations in BACnet
        //so we have to adjust it. (In order to fulfill the standard)
        const int maxNPDUHeaderLength = 4;       //usually it's '2', but it can also be more than '4'. Beware!
        return Math.Min(maxAPDU, Transport.MaxBufferLength - Transport.HeaderLength - maxNPDUHeaderLength);
    }

    public int GetFileBufferMaxSize()
    {
        //6 should be the max_apdu_header_length for Confirmed (with segmentation)
        //12 should be the max_atomic_write_file
        return GetMaxApdu() - 18;
    }

    public bool WriteFileRequest(BACnetAddress adr, BACnetObjectId objectId, ref int position, int count, byte[] fileBuffer, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginWriteFileRequest(adr, objectId, position, count, fileBuffer, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWriteFileRequest(result, out position, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginWriteFileRequest(BACnetAddress adr, BACnetObjectId objectId, int position, int count, byte[] fileBuffer, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending AtomicWriteFileRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAtomicWriteFile(buffer, true, objectId, position, 1, new[] { fileBuffer }, new[] { count });

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndWriteFileRequest(IAsyncResult result, out int position, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeAtomicWriteFileAcknowledge(res.Result, 0, res.Result.Length, out _, out position) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            position = -1;
        }

        res.Dispose();
    }

    public IAsyncResult BeginReadFileRequest(BACnetAddress adr, BACnetObjectId objectId, int position, uint count, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending AtomicReadFileRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        //encode
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAtomicReadFile(buffer, true, objectId, position, count);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReadFileRequest(IAsyncResult result, out uint count, out int position, out bool endOfFile, out byte[] fileBuffer, out int fileBufferOffset, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeAtomicReadFileAcknowledge(res.Result, 0, res.Result.Length, out endOfFile, out _, out position, out count, out fileBuffer, out fileBufferOffset) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            count = 0;
            endOfFile = true;
            position = -1;
            fileBufferOffset = -1;
            fileBuffer = new byte[0];
        }

        res.Dispose();
    }

    public bool ReadFileRequest(BACnetAddress adr, BACnetObjectId objectId, ref int position, ref uint count, out bool endOfFile, out byte[] fileBuffer, out int fileBufferOffset, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginReadFileRequest(adr, objectId, position, count, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadFileRequest(result, out count, out position, out endOfFile, out fileBuffer, out fileBufferOffset, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        position = -1;
        count = 0;
        fileBuffer = null;
        endOfFile = true;
        fileBufferOffset = -1;
        return false;
    }

    // Read range by postion
    public IAsyncResult BeginReadRangeRequest(BACnetAddress adr, BACnetObjectId objectId, uint idxBegin, uint quantity, bool waitForTransmit, byte invokeId = 0)
    {
        return BeginReadRangeRequestCore(adr, objectId, BACnetReadRangeRequestTypes.RR_BY_POSITION, DateTime.Now, idxBegin, quantity, waitForTransmit, invokeId);
    }

    // Read range by start time
    public IAsyncResult BeginReadRangeRequest(BACnetAddress adr, BACnetObjectId objectId, DateTime readFrom, uint quantity, bool waitForTransmit, byte invokeId = 0)
    {
        return BeginReadRangeRequestCore(adr, objectId, BACnetReadRangeRequestTypes.RR_BY_TIME, readFrom, 1, quantity, waitForTransmit, invokeId);
    }

    private IAsyncResult BeginReadRangeRequestCore(BACnetAddress adr, BACnetObjectId objectId, BACnetReadRangeRequestTypes bacnetReadRangeRequestTypes, DateTime readFrom, uint idxBegin, uint quantity, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending ReadRangeRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        //encode
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadRange(buffer, objectId, (uint)BACnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL, bacnetReadRangeRequestTypes, idxBegin, readFrom, (int)quantity);
        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // Fc
    public void EndReadRangeRequest(IAsyncResult result, out byte[] trendbuffer, out uint itemCount, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        itemCount = 0;
        trendbuffer = null;

        ex = res.Error;
        if (ex == null && !res.WaitForDone(40 * 1000))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            itemCount = Services.DecodeReadRangeAcknowledge(res.Result, 0, res.Result.Length, out trendbuffer);
            if (itemCount == 0)
                ex = new Exception("Decode");
        }

        res.Dispose();
    }

    // Fc
    public bool ReadRangeRequest(BACnetAddress adr, BACnetObjectId objectId, DateTime readFrom, ref uint quantity, out byte[] range, byte invokeId = 0)
    {
        return ReadRangeRequestCore(BACnetReadRangeRequestTypes.RR_BY_TIME, adr, objectId, 1, readFrom, ref quantity, out range, invokeId);
    }
    public bool ReadRangeRequest(BACnetAddress adr, BACnetObjectId objectId, uint idxBegin, ref uint quantity, out byte[] range, byte invokeId = 0)
    {
        return ReadRangeRequestCore(BACnetReadRangeRequestTypes.RR_BY_POSITION, adr, objectId, idxBegin, DateTime.Now, ref quantity, out range, invokeId);
    }

    private bool ReadRangeRequestCore(BACnetReadRangeRequestTypes requestType, BACnetAddress adr, BACnetObjectId objectId, uint idxBegin, DateTime readFrom, ref uint quantity, out byte[] range, byte invokeId = 0)
    {
        Func<IAsyncResult> getResult;
        uint quantityCopy = quantity;
        switch (requestType)
        {
            case BACnetReadRangeRequestTypes.RR_BY_TIME:
                getResult = () => BeginReadRangeRequest(adr, objectId, readFrom, quantityCopy, true, invokeId);
                break;

            case BACnetReadRangeRequestTypes.RR_BY_POSITION:
                getResult = () => BeginReadRangeRequest(adr, objectId, idxBegin, quantityCopy, true, invokeId);
                break;

            default:
                throw new NotImplementedException($"BACnetReadRangeRequestTypes-Type {requestType} not supported in {nameof(ReadRangeRequestCore)}!");
        }

        range = null;
        using (var result = getResult() as BACnetAsyncResult)
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadRangeRequest(result, out range, out quantity, out var ex); // quantity read could be less than demanded
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public bool SubscribeCOVRequest(BACnetAddress adr, BACnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginSubscribeCOVRequest(adr, objectId, subscribeId, cancel, issueConfirmedNotifications, lifetime, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndSubscribeCOVRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginSubscribeCOVRequest(BACnetAddress adr, BACnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending SubscribeCOVRequest {objectId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeSubscribeCOV(buffer, subscribeId, objectId, cancel, issueConfirmedNotifications, lifetime);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndSubscribeCOVRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // DAL
    public bool SendConfirmedEventNotificationRequest(BACnetAddress adr, BACnetEventNotificationData eventData, byte invokeId = 0, BACnetAddress source = null)
    {
        using (var result = (BACnetAsyncResult)BeginSendConfirmedEventNotificationRequest(adr, eventData, true, invokeId, source))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndSendConfirmedEventNotificationRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    // DAL
    public IAsyncResult BeginSendConfirmedEventNotificationRequest(BACnetAddress adr, BACnetEventNotificationData eventData, bool waitForTransmit, byte invokeId = 0, BACnetAddress source = null)
    {
        Log.LogDebug($"Sending Confirmed Event Notification {eventData.eventType} {eventData.eventObjectIdentifier}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr, source);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeEventNotifyConfirmed(buffer, eventData);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // DAL
    public void EndSendConfirmedEventNotificationRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool SubscribePropertyRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginSubscribePropertyRequest(adr, objectId, monitoredProperty, subscribeId, cancel, issueConfirmedNotifications, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndSubscribePropertyRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginSubscribePropertyRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending SubscribePropertyRequest {objectId}.{monitoredProperty}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeSubscribeProperty(buffer, subscribeId, objectId, cancel, issueConfirmedNotifications, 0, monitoredProperty, false, 0f);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndSubscribePropertyRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool ReadPropertyRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyIds propertyId, out IList<BACnetValue> valueList, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        using (var result = (BACnetAsyncResult)BeginReadPropertyRequest(adr, objectId, propertyId, true, invokeId, arrayIndex))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadPropertyRequest(result, out valueList, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        valueList = null;
        return false;
    }

    public Task<IList<BACnetValue>> ReadPropertyAsync(BACnetAddress address, BACnetObjectTypes objType, uint objInstance,
        BACnetPropertyIds propertyId, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        var objectId = new BACnetObjectId(objType, objInstance);
        return ReadPropertyAsync(address, objectId, propertyId, invokeId, arrayIndex);
    }

    public Task<IList<BACnetValue>> ReadPropertyAsync(BACnetAddress address, BACnetObjectId objectId,
        BACnetPropertyIds propertyId, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        return Task<IList<BACnetValue>>.Factory.StartNew(() =>
        {
            if (!ReadPropertyRequest(address, objectId, propertyId, out IList<BACnetValue> result, invokeId, arrayIndex))
                throw new Exception($"Failed to read property {propertyId} of {objectId} from {address}");

            return result;
        });
    }

    public IAsyncResult BeginReadPropertyRequest(BACnetAddress address, BACnetObjectId objectId, BACnetPropertyIds propertyId, bool waitForTransmit, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        Log.LogDebug($"Sending ReadPropertyRequest {objectId} {propertyId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, address.RoutedSource, address.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadProperty(buffer, objectId, (uint)propertyId, arrayIndex);

        //send
        var ret = new BACnetAsyncResult(this, address, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReadPropertyRequest(IAsyncResult result, out IList<BACnetValue> valueList, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeReadPropertyAcknowledge(res.Address, res.Result, 0, res.Result.Length, out _, out _, out valueList) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            valueList = null;
        }

        res.Dispose();
    }

    public bool WritePropertyRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyIds propertyId, IEnumerable<BACnetValue> valueList, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginWritePropertyRequest(adr, objectId, propertyId, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWritePropertyRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public bool WritePropertyMultipleRequest(BACnetAddress adr, BACnetObjectId objectId, ICollection<BACnetPropertyValue> valueList, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginWritePropertyMultipleRequest(adr, objectId, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWritePropertyRequest(result, out var ex); // Share the same with single write
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginWritePropertyRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyIds propertyId, IEnumerable<BACnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending WritePropertyRequest {objectId} {propertyId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeWriteProperty(buffer, objectId, (uint)propertyId, ASN1.BACNET_ARRAY_ALL, _writepriority, valueList);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public IAsyncResult BeginWritePropertyMultipleRequest(BACnetAddress adr, BACnetObjectId objectId, ICollection<BACnetPropertyValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending WritePropertyMultipleRequest {objectId}");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        //BACnetNpduControls.PriorityNormalMessage 
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);

        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeWritePropertyMultiple(buffer, objectId, valueList);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndWritePropertyRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // By Chritopher Günter : Write multiple properties on multiple objects
    public bool WritePropertyMultipleRequest(BACnetAddress adr, ICollection<BACnetReadAccessResult> valueList, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginWritePropertyMultipleRequest(adr, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWritePropertyRequest(result, out var ex); // Share the same with single write
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginWritePropertyMultipleRequest(BACnetAddress adr, ICollection<BACnetReadAccessResult> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        var objectIds = string.Join(", ", valueList.Select(v => v.objectIdentifier));
        Log.LogDebug($"Sending WritePropertyMultipleRequest {objectIds}");

        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        //BACnetNpduControls.PriorityNormalMessage 
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);

        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeWriteObjectMultiple(buffer, valueList);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public bool ReadPropertyMultipleRequest(BACnetAddress address, BACnetObjectId objectId, IList<BACnetPropertyReference> propertyIdAndArrayIndex, out IList<BACnetReadAccessResult> values, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginReadPropertyMultipleRequest(address, objectId, propertyIdAndArrayIndex, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadPropertyMultipleRequest(result, out values, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        values = null;
        return false;
    }

    public Task<IList<BACnetPropertyValue>> ReadPropertyMultipleAsync(BACnetAddress address,
        BACnetObjectTypes objType, uint objInstance, params BACnetPropertyIds[] propertyIds)
    {
        var objectId = new BACnetObjectId(objType, objInstance);
        return ReadPropertyMultipleAsync(address, objectId, propertyIds);
    }

    public Task<IList<BACnetPropertyValue>> ReadPropertyMultipleAsync(BACnetAddress address,
        BACnetObjectId objectId, params BACnetPropertyIds[] propertyIds)
    {
        var propertyReferences = propertyIds.Select(p =>
            new BACnetPropertyReference((uint)p, ASN1.BACNET_ARRAY_ALL));

        return Task<IList<BACnetPropertyValue>>.Factory.StartNew(() =>
        {
            if (!ReadPropertyMultipleRequest(address, objectId, propertyReferences.ToList(), out var result))
                throw new Exception($"Failed to read multiple properties of {objectId} from {address}");

            return result.Single().values;
        });
    }

    public IAsyncResult BeginReadPropertyMultipleRequest(BACnetAddress adr, BACnetObjectId objectId, IList<BACnetPropertyReference> propertyIdAndArrayIndex, bool waitForTransmit, byte invokeId = 0)
    {
        var propertyIds = string.Join(", ", propertyIdAndArrayIndex.Select(v => (BACnetPropertyIds)v.propertyIdentifier));
        Log.LogDebug($"Sending ReadPropertyMultipleRequest {objectId} {propertyIds}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadPropertyMultiple(buffer, objectId, propertyIdAndArrayIndex);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // Another way to read multiple properties on multiples objects, if supported by devices
    public bool ReadPropertyMultipleRequest(BACnetAddress address, IList<BACnetReadAccessSpecification> properties, out IList<BACnetReadAccessResult> values, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginReadPropertyMultipleRequest(address, properties, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadPropertyMultipleRequest(result, out values, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        values = null;
        return false;
    }

    public IAsyncResult BeginReadPropertyMultipleRequest(BACnetAddress adr, IList<BACnetReadAccessSpecification> properties, bool waitForTransmit, byte invokeId = 0)
    {
        var objectIds = string.Join(", ", properties.Select(v => v.objectIdentifier));
        Log.LogDebug($"Sending ReadPropertyMultipleRequest {objectIds}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadPropertyMultiple(buffer, properties);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReadPropertyMultipleRequest(IAsyncResult result, out IList<BACnetReadAccessResult> values, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeReadPropertyMultipleAcknowledge(res.Address, res.Result, 0, res.Result.Length, out values) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            values = null;
        }

        res.Dispose();
    }

    private BACnetPduTypes PduConfirmedServiceRequest()
    {
        return MaxSegments != BACnetMaxSegments.MAX_SEG0
            ? BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | BACnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED
            : BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST;
    }

    // By Christopher Günter
    public bool CreateObjectRequest(BACnetAddress adr, BACnetObjectId objectId, ICollection<BACnetPropertyValue> valueList = null, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginCreateObjectRequest(adr, objectId, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndCreateObjectRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginCreateObjectRequest(BACnetAddress adr, BACnetObjectId objectId, ICollection<BACnetPropertyValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending CreateObjectRequest");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BACnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeCreateProperty(buffer, objectId, valueList);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndCreateObjectRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool DeleteObjectRequest(BACnetAddress adr, BACnetObjectId objectId, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginDeleteObjectRequest(adr, objectId, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndDeleteObjectRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }

        return false;
    }

    public IAsyncResult BeginDeleteObjectRequest(BACnetAddress adr, BACnetObjectId objectId, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending DeleteObjectRequest");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        //NPDU.Encode(b, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply , adr.RoutedSource);

        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        ASN1.encode_application_object_id(buffer, objectId.type, objectId.instance);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndDeleteObjectRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool AddListElementRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyReference reference, IList<BACnetValue> valueList, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginAddListElementRequest(adr, objectId, reference, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {

                if (result.WaitForDone(Timeout))
                {
                    EndAddListElementRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        //values = null;
        return false;
    }

    public bool RemoveListElementRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyReference reference, IList<BACnetValue> valueList, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginRemoveListElementRequest(adr, objectId, reference, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndAddListElementRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        //values = null;
        return false;
    }

    public IAsyncResult BeginRemoveListElementRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyReference reference, IList<BACnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending RemoveListElementRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAddListElement(buffer, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public IAsyncResult BeginAddListElementRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyReference reference, IList<BACnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending AddListElementRequest {objectId} {(BACnetPropertyIds)reference.propertyIdentifier}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAddListElement(buffer, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndAddListElementRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // Fc
    // Read or Write without APDU Data encoding nor Decoding (just Request type, Object id and Property id)
    // Data is given by the caller starting with the Tag 3 (or maybe another one), and ending with it
    // return buffer start also with the Tag 3
    public bool RawEncodedDecodedPropertyConfirmedRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyIds propertyId, BACnetConfirmedServices serviceId, ref byte[] inOutBuffer, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginRawEncodedDecodedPropertyConfirmedRequest(adr, objectId, propertyId, serviceId, inOutBuffer, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndRawEncodedDecodedPropertyConfirmedRequest(result, serviceId, out inOutBuffer, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        inOutBuffer = null;
        return false;
    }

    // Fc
    public IAsyncResult BeginRawEncodedDecodedPropertyConfirmedRequest(BACnetAddress adr, BACnetObjectId objectId, BACnetPropertyIds propertyId, BACnetConfirmedServices serviceId, byte[] inOutBuffer, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending RawEncodedRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), serviceId, MaxSegments, Transport.MaxAdpuLength, invokeId);

        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, (byte)propertyId);

        // No content encoding to do
        if (inOutBuffer != null)
            buffer.Add(inOutBuffer, inOutBuffer.Length);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // Fc
    public void EndRawEncodedDecodedPropertyConfirmedRequest(IAsyncResult result, BACnetConfirmedServices serviceId, out byte[] inOutBuffer, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        inOutBuffer = null;

        if (ex == null)
        {
            if (serviceId == BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY)
            {
                //decode
                const int offset = 0;
                var buffer = res.Result;

                ex = new Exception("Decode");

                if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                    return;
                var len = 1;
                len += ASN1.decode_object_id(buffer, offset + len, out BACnetObjectTypes _, out _);
                /* Tag 1: Property ID */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
                if (tagNumber != 1)
                    return;
                len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out _);

                inOutBuffer = new byte[buffer.Length - len];
                Array.Copy(buffer, len, inOutBuffer, 0, inOutBuffer.Length);

                ex = null;
            }
        }

        res.Dispose();
    }

    public bool DeviceCommunicationControlRequest(BACnetAddress adr, uint timeDuration, uint enableDisable, string password, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginDeviceCommunicationControlRequest(adr, timeDuration, enableDisable, password, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndDeviceCommunicationControlRequest(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginDeviceCommunicationControlRequest(BACnetAddress adr, uint timeDuration, uint enableDisable, string password, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending DeviceCommunicationControlRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeDeviceCommunicationControl(buffer, timeDuration, enableDisable, password);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndDeviceCommunicationControlRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // FChaxel
    public bool GetAlarmSummaryOrEventRequest(BACnetAddress adr, bool getEvent, ref IList<BACnetGetEventInformationData> alarms, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginGetAlarmSummaryOrEventRequest(adr, getEvent, alarms, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndGetAlarmSummaryOrEventRequest(result, getEvent, ref alarms, out var moreEvent, out var ex);
                    if (ex != null)
                        return false;
                    return !moreEvent || GetAlarmSummaryOrEventRequest(adr, getEvent, ref alarms);
                }

                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public Task<IList<BACnetGetEventInformationData>> GetEventsAsync(BACnetAddress address, byte invokeId = 0)
    {
        IList<BACnetGetEventInformationData> result = new List<BACnetGetEventInformationData>();

        return Task<IList<BACnetGetEventInformationData>>.Factory.StartNew(() =>
        {
            if (!GetAlarmSummaryOrEventRequest(address, true, ref result, invokeId))
                throw new Exception($"Failed to get events from {address}");

            return result;
        });
    }

    public IAsyncResult BeginGetAlarmSummaryOrEventRequest(BACnetAddress adr, bool getEvent, IList<BACnetGetEventInformationData> alarms, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending Alarm summary request");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);

        var service = getEvent
            ? BACnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION
            : BACnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY;

        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), service, MaxSegments, Transport.MaxAdpuLength, invokeId);

        // Get Next, never true if GetAlarmSummary is usee
        if (alarms.Count != 0)
            ASN1.encode_context_object_id(buffer, 0, alarms[alarms.Count - 1].objectIdentifier.type, alarms[alarms.Count - 1].objectIdentifier.instance);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndGetAlarmSummaryOrEventRequest(IAsyncResult result, bool getEvent, ref IList<BACnetGetEventInformationData> alarms, out bool moreEvent, out Exception ex)
    {
        moreEvent = false;
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            if (Services.DecodeAlarmSummaryOrEvent(res.Result, 0, res.Result.Length, getEvent, ref alarms, out moreEvent) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            ex = new Exception("Service not available");
        }

        res.Dispose();
    }
    // DAL
    public void GetAlarmSummaryOrEventInformationResponse(BACnetAddress adr, bool getEvent, byte invoke_id, Segmentation segmentation, BACnetGetEventInformationData[] data, bool more_events)
    {
        // 'getEvent' is not currently used.   Can be used if ever implementing GetAlarmSummary.
        // response could be segmented
        // but if you don't want it segmented (which would be normal usage)
        // you have to compute the message data and the 'more' flag
        // outside this function.
        HandleSegmentationResponse(adr, invoke_id, segmentation, (o) =>
        {
            SendComplexAck(adr, invoke_id, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, (b) =>
            {
                Services.EncodeGetEventInformationAcknowledge(b, data, more_events);
            });
        });
    }

    // FChaxel
    public bool AlarmAcknowledgement(BACnetAddress adr, BACnetObjectId objId, BACnetEventStates eventState, string ackText, BACnetGenericTime evTimeStamp, BACnetGenericTime ackTimeStamp, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginAlarmAcknowledgement(adr, objId, eventState, ackText, evTimeStamp, ackTimeStamp, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndAlarmAcknowledgement(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginAlarmAcknowledgement(BACnetAddress adr, BACnetObjectId objId, BACnetEventStates eventState, string ackText, BACnetGenericTime evTimeStamp, BACnetGenericTime ackTimeStamp, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending AlarmAcknowledgement");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAlarmAcknowledge(buffer, 57, objId, (uint)eventState, ackText, evTimeStamp, ackTimeStamp);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndAlarmAcknowledgement(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (!res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");
    }

    public bool ReinitializeRequest(BACnetAddress adr, BACnetReinitializedStates state, string password, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginReinitializeRequest(adr, state, password, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReinitializeRequest(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginReinitializeRequest(BACnetAddress adr, BACnetReinitializedStates state, string password, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending ReinitializeRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReinitializeDevice(buffer, state, password);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReinitializeRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public IAsyncResult BeginConfirmedNotify(BACnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BACnetObjectId monitoredObjectIdentifier, uint timeRemaining, IList<BACnetPropertyValue> values, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending Notify (confirmed)");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeCOVNotifyConfirmed(buffer, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);

        //send
        var ret = new BACnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndConfirmedNotify(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (!res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");
    }

    public bool Notify(BACnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BACnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool issueConfirmedNotifications, IList<BACnetPropertyValue> values)
    {
        if (!issueConfirmedNotifications)
        {
            Log.LogDebug("Sending Notify (unconfirmed)");
            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
            APDU.EncodeUnconfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
            Services.EncodeCOVNotifyUnconfirmed(buffer, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);
            // Modif F. Chaxel

            var sendbytes = Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
            return sendbytes == buffer.offset;
        }

        using (var result = (BACnetAsyncResult)BeginConfirmedNotify(adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values, true))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndConfirmedNotify(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }

        return false;
    }

    public bool LifeSafetyOperationRequest(BACnetAddress address, BACnetObjectId objectId, string requestingSrc, BACnetLifeSafetyOperations operation, byte invokeId = 0)
    {
        using (var result = (BACnetAsyncResult)BeginLifeSafetyOperationRequest(address, objectId, 0, requestingSrc, operation, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndLifeSafetyOperationRequest(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginLifeSafetyOperationRequest(BACnetAddress address, BACnetObjectId objectId, uint processId, string requestingSrc, BACnetLifeSafetyOperations operation, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending {ToTitleCase(operation)} {objectId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage | BACnetNpduControls.ExpectingReply, address.RoutedSource, address.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeLifeSafetyOperation(buffer, processId, requestingSrc, (uint)operation, objectId);

        //send
        var ret = new BACnetAsyncResult(this, address, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndLifeSafetyOperationRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BACnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public static byte GetSegmentsCount(BACnetMaxSegments maxSegments)
    {
        switch (maxSegments)
        {
            case BACnetMaxSegments.MAX_SEG0:
                return 0;
            case BACnetMaxSegments.MAX_SEG2:
                return 2;
            case BACnetMaxSegments.MAX_SEG4:
                return 4;
            case BACnetMaxSegments.MAX_SEG8:
                return 8;
            case BACnetMaxSegments.MAX_SEG16:
                return 16;
            case BACnetMaxSegments.MAX_SEG32:
                return 32;
            case BACnetMaxSegments.MAX_SEG64:
                return 64;
            case BACnetMaxSegments.MAX_SEG65:
                return 0xFF;
            default:
                throw new Exception("Not an option");
        }
    }

    public static BACnetMaxSegments GetSegmentsCount(byte maxSegments)
    {
        if (maxSegments == 0)
            return BACnetMaxSegments.MAX_SEG0;
        if (maxSegments <= 2)
            return BACnetMaxSegments.MAX_SEG2;
        if (maxSegments <= 4)
            return BACnetMaxSegments.MAX_SEG4;
        if (maxSegments <= 8)
            return BACnetMaxSegments.MAX_SEG8;
        if (maxSegments <= 16)
            return BACnetMaxSegments.MAX_SEG16;
        if (maxSegments <= 32)
            return BACnetMaxSegments.MAX_SEG32;
        if (maxSegments <= 64)
            return BACnetMaxSegments.MAX_SEG64;

        return BACnetMaxSegments.MAX_SEG65;
    }

    public Segmentation GetSegmentBuffer(BACnetMaxSegments maxSegments)
    {
        if (maxSegments == BACnetMaxSegments.MAX_SEG0)
            return null;

        return new Segmentation
        {
            buffer = GetEncodeBuffer(Transport.HeaderLength),
            max_segments = GetSegmentsCount(maxSegments),
            window_size = ProposedWindowSize
        };
    }

    private EncodeBuffer EncodeSegmentHeader(BACnetAddress adr, byte invokeId, Segmentation segmentation, BACnetConfirmedServices service, bool moreFollows)
    {
        EncodeBuffer buffer;
        var isSegmented = false;
        if (segmentation == null)
            buffer = GetEncodeBuffer(Transport.HeaderLength);
        else
        {
            buffer = segmentation.buffer;
            isSegmented = segmentation.sequence_number > 0 | moreFollows;
        }
        buffer.Reset(Transport.HeaderLength);

        //encode
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);

        //set segments limits
        buffer.max_offset = buffer.offset + GetMaxApdu();
        var apduHeader = APDU.EncodeComplexAck(buffer, BACnetPduTypes.PDU_TYPE_COMPLEX_ACK | (isSegmented ? BACnetPduTypes.SEGMENTED_MESSAGE | BACnetPduTypes.SERVER : 0) | (moreFollows ? BACnetPduTypes.MORE_FOLLOWS : 0), service, invokeId, segmentation?.sequence_number ?? 0, segmentation?.window_size ?? 0);
        buffer.min_limit = (GetMaxApdu() - apduHeader) * (segmentation?.sequence_number ?? 0);

        return buffer;
    }

    private bool EncodeSegment(BACnetAddress adr, byte invokeId, Segmentation segmentation, BACnetConfirmedServices service, out EncodeBuffer buffer, Action<EncodeBuffer> apduContentEncode)
    {
        //encode (regular)
        buffer = EncodeSegmentHeader(adr, invokeId, segmentation, service, false);
        apduContentEncode(buffer);

        var moreFollows = (buffer.result & EncodeResult.NotEnoughBuffer) > 0;
        if (segmentation != null && moreFollows)
        {
            //reencode in segmented
            EncodeSegmentHeader(adr, invokeId, segmentation, service, true);
            apduContentEncode(buffer);
            return true;
        }

        if (moreFollows)
            return true;

        return segmentation != null && segmentation.sequence_number > 0;
    }

    /// <summary>
    /// Handle the segmentation of several too hugh response (if it's accepted by the client) 
    /// used by ReadRange, ReadProperty, ReadPropertyMultiple & ReadFile responses
    /// </summary>
    private void HandleSegmentationResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, Action<Segmentation> transmit)
    {
        // send first
        transmit(segmentation);

        if (segmentation == null || segmentation.buffer.result == EncodeResult.Good)
            return;

        // start new thread to handle the segment sequence (if required)
        ThreadPool.QueueUserWorkItem(o =>
        {
            var oldMaxInfoFrames = Transport.MaxInfoFrames;
            Transport.MaxInfoFrames = segmentation.window_size; // increase max_info_frames, to increase throughput. This might be against 'standard'

            while (true)
            {
                var moreFollows = (segmentation.buffer.result & EncodeResult.NotEnoughBuffer) > 0;

                // wait for segmentACK
                if ((segmentation.sequence_number - 1) % segmentation.window_size == 0 || !moreFollows)
                {
                    if (!WaitForAllTransmits(TransmitTimeout))
                    {
                        Log.LogWarning("Transmit timeout");
                        break;
                    }

                    var currentNumber = segmentation.sequence_number;

                    if (!WaitForSegmentAck(adr, invokeId, segmentation, Timeout))
                    {
                        Log.LogWarning("Didn't get segmentACK");
                        break;
                    }

                    if (segmentation.sequence_number != currentNumber)
                    {
                        Log.LogDebug("Oh, a retransmit");
                        moreFollows = true;
                    }
                }
                else
                {
                    // a negative segmentACK perhaps
                    var currentNumber = segmentation.sequence_number;
                    WaitForSegmentAck(adr, invokeId, segmentation, 0); // don't wait

                    if (segmentation.sequence_number != currentNumber)
                        Log.LogDebug("Oh, a retransmit");
                }

                if (moreFollows)
                    // lock (m_lockObject) transmit(segmentation);
                    transmit(segmentation);
                else
                    break;
            }

            Transport.MaxInfoFrames = oldMaxInfoFrames;
        });
    }

    private void SendComplexAck(BACnetAddress adr, byte invokeId, Segmentation segmentation, BACnetConfirmedServices service, Action<EncodeBuffer> apduContentEncode)
    {
        Log.LogDebug($"Sending {ToTitleCase(service)}");

        //encode
        if (EncodeSegment(adr, invokeId, segmentation, service, out var buffer, apduContentEncode))
        {
            //client doesn't support segments
            if (segmentation == null)
            {
                Log.LogInformation("Segmentation denied");
                // DAL
                SendAbort(adr, invokeId, BACnetAbortReason.SEGMENTATION_NOT_SUPPORTED);
                //ErrorResponse(adr, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                buffer.result = EncodeResult.Good;     //don't continue the segmentation
                return;
            }

            //first segment? validate max segments
            if (segmentation.sequence_number == 0)  //only validate first segment
            {
                if (segmentation.max_segments != 0xFF && segmentation.buffer.offset > segmentation.max_segments * (GetMaxApdu() - 5))      //5 is adpu header
                {
                    Log.LogInformation("Too much segmenation");
                    // DAL
                    SendAbort(adr, invokeId, BACnetAbortReason.APDU_TOO_LONG);
                    //ErrorResponse(adr, service, invokeId, BACnetErrorClasses.ERROR_CLASS_SERVICES, BACnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                    buffer.result = EncodeResult.Good;     //don't continue the segmentation
                    return;
                }
                Log.LogDebug("Segmentation required");
            }

            //increment before ack can do so (race condition)
            unchecked { segmentation.sequence_number++; }
            ;
        }

        //send
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.GetLength() - Transport.HeaderLength, adr, false, 0);
    }

    public void ReadPropertyResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, BACnetObjectId objectId, BACnetPropertyReference property, IEnumerable<BACnetValue> value)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, b =>
            {
                Services.EncodeReadPropertyAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, value);
            });
        });
    }

    public void CreateObjectResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, BACnetObjectId objectId)
    {
        SendComplexAck(adr, invokeId, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, b =>
        {
            Services.EncodeCreateObjectAcknowledge(b, objectId);
        });
    }

    public void ReadPropertyMultipleResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, IList<BACnetReadAccessResult> values)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, b =>
            {
                Services.EncodeReadPropertyMultipleAcknowledge(b, values);
            });
        });
    }

    public void ReadRangeResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, BACnetObjectId objectId, BACnetPropertyReference property, BACnetResultFlags status, uint itemCount, byte[] applicationData, BACnetReadRangeRequestTypes requestType, uint firstSequenceNo)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, b =>
            {
                Services.EncodeReadRangeAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, BACnetBitString.ConvertFromInt((uint)status), itemCount, applicationData, requestType, firstSequenceNo);
            });
        });
    }

    public void ReadFileResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, int position, uint count, bool endOfFile, byte[] fileBuffer)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, b =>
            {
                Services.EncodeAtomicReadFileAcknowledge(b, true, endOfFile, position, 1, new[] { fileBuffer }, new[] { (int)count });
            });
        });
    }

    public void WriteFileResponse(BACnetAddress adr, byte invokeId, Segmentation segmentation, int position)
    {
        SendComplexAck(adr, invokeId, segmentation, BACnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, b =>
        {
            Services.EncodeAtomicWriteFileAcknowledge(b, true, position);
        });
    }

    public void ErrorResponse(BACnetAddress adr, BACnetConfirmedServices service, byte invokeId, BACnetErrorClasses errorClass, BACnetErrorCodes errorCode)
    {
        Log.LogDebug($"Sending ErrorResponse for {service}: {errorCode}");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(buffer, BACnetPduTypes.PDU_TYPE_ERROR, service, invokeId);
        Services.EncodeError(buffer, errorClass, errorCode);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SimpleAckResponse(BACnetAddress adr, BACnetConfirmedServices service, byte invokeId)
    {
        Log.LogDebug($"Sending SimpleAckResponse for {service}");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeSimpleAck(buffer, BACnetPduTypes.PDU_TYPE_SIMPLE_ACK, service, invokeId);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SegmentAckResponse(BACnetAddress adr, bool negative, bool server, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize)
    {
        Log.LogDebug("Sending SegmentAckResponse");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BACnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeSegmentAck(buffer, BACnetPduTypes.PDU_TYPE_SEGMENT_ACK | (negative ? BACnetPduTypes.NEGATIVE_ACK : 0) | (server ? BACnetPduTypes.SERVER : 0), originalInvokeId, sequenceNumber, actualWindowSize);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public bool WaitForAllTransmits(int timeout)
    {
        return Transport.WaitForAllTransmits(timeout);
    }

    public bool WaitForSegmentAck(BACnetAddress adr, byte invokeId, Segmentation segmentation, int timeout)
    {
        if (!_lastSegmentAck.Wait(adr, invokeId, timeout))
            return false;

        segmentation.sequence_number = (byte)((_lastSegmentAck.SequenceNumber + 1) % 256);
        segmentation.window_size = _lastSegmentAck.WindowSize;
        return true;
    }

    private static string ToTitleCase(object obj)
    {
        var cultureTextInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
        return cultureTextInfo.ToTitleCase($"{obj}".ToLower());
    }

    public void Dispose()
    {
        Transport.Dispose();
    }
}
