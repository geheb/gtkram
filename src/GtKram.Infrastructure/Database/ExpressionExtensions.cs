using System.Linq.Expressions;

namespace GtKram.Infrastructure.Database;

public static class ExpressionExtensions
{
    public static string GetPropertyName<T>(this Expression<Func<T, object?>> field)
    {
        var expression = field.Body;
        while (true)
        {
            switch (expression)
            {
                case MemberExpression memberExpression:
                    return memberExpression.Member.Name;
                case UnaryExpression unaryExpression:
                    expression = unaryExpression.Operand;
                    continue;
            }
            throw new ArgumentException("Invalid expression.");
        }
    }
}
