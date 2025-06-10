using System.Linq.Expressions;
using GtKram.Infrastructure.Persistence;

namespace GtKram.Infrastructure.Repositories;

internal struct WhereFieldPair<T>
{
    public string Field { get; init; }

    public object? Value { get; init; }

    public WhereFieldPair(Expression<Func<T, object?>> field, object? value)
    {
        Field = field.GetPropertyName();
        Value = value;
    }
}
