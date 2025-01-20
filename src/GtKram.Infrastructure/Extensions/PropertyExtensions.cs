using System.Linq.Expressions;
using System.Reflection;

namespace GtKram.Infrastructure.Extensions;

public static class PropertyExtensions
{
    public static bool SetValue<T, TValue>(this T target, Expression<Func<T, TValue>> memberLamda, TValue value)
    {
        var memberSelectorExpression = memberLamda.Body as MemberExpression;
        if (memberSelectorExpression == null) throw new InvalidCastException($"{nameof(memberLamda)} has no body");

        var property = memberSelectorExpression.Member as PropertyInfo;
        if (property == null) throw new InvalidCastException($"{nameof(memberLamda)} has no member");

        var targetValue = property.GetValue(target);

        if (ReferenceEquals(targetValue, value))
        {
            return false;
        }

        if (!ReferenceEquals(null, targetValue) && !targetValue.Equals(value))
        {
            property.SetValue(target, value, null);
            return true;
        }

        if (!ReferenceEquals(null, value) && !value.Equals(targetValue))
        {
            property.SetValue(target, value, null);
            return true;
        }

        return false;
    }
}
