namespace System.Net.BACnet.Helpers;

public static class BACnetValuesExtensions
{
    public static bool Has(this IList<BACnetPropertyValue> propertyValues, BACnetPropertyIds propertyId)
    {
        if (propertyValues.All(v => v.property.GetPropertyId() != propertyId))
            return false;

        return propertyValues
            .Where(v => v.property.GetPropertyId() == propertyId)
            .Any(v => !v.value.HasError());
    }

    public static bool HasError(this IList<BACnetPropertyValue> propertyValues, BACnetErrorCodes error)
    {
        return propertyValues
            .SelectMany(p => p.value)
            .Where(v => v.Tag == BACnetApplicationTags.BACNET_APPLICATION_TAG_ERROR)
            .Any(v => v.As<BACnetError>().error_code == error);
    }

    public static bool HasError(this IList<BACnetPropertyValue> propertyValues)
    {
        return propertyValues.Any(p => p.value.HasError());
    }

    public static bool HasError(this IList<BACnetValue> values)
    {
        return values.Any(v => v.Tag == BACnetApplicationTags.BACNET_APPLICATION_TAG_ERROR);
    }

    public static object Get(this IList<BACnetPropertyValue> propertyValues, BACnetPropertyIds propertyId)
    {
        return Get<object>(propertyValues, propertyId);
    }

    public static T Get<T>(this IList<BACnetPropertyValue> propertyValues, BACnetPropertyIds propertyId)
    {
        return GetMany<T>(propertyValues, propertyId).FirstOrDefault();
    }

    public static T[] GetMany<T>(this IList<BACnetPropertyValue> propertyValues, BACnetPropertyIds propertyId)
    {
        if (!propertyValues.Has(propertyId))
            return new T[0];

        var property = propertyValues.First(v => v.property.GetPropertyId() == propertyId);

        return property.property.propertyArrayIndex == ASN1.BACNET_ARRAY_ALL
            ? property.value.GetMany<T>()
            : new[] { property.value[(int)property.property.propertyArrayIndex].As<T>() };
    }

    public static T[] GetMany<T>(this IList<BACnetValue> values)
    {
        return values.Select(v => v.As<T>()).ToArray();
    }

    public static T Get<T>(this IList<BACnetValue> values)
    {
        return GetMany<T>(values).FirstOrDefault();
    }
}
