using Microsoft.CodeAnalysis;

namespace SourceGenerator.Records;

public record PropertyInfo(
            string Name,
            string TypeName,
            ITypeSymbol Type)
{
}
