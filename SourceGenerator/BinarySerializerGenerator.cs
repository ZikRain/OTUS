using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SourceGenerator.Records;
using System.Collections.Immutable;
using System.Text;

namespace BinarySerializerGenerator;

[Generator]
public class BinarySerializerGenerator : IIncrementalGenerator
{

    #region Const
    const string _int = "int";
    const string _uint = "uint";
    const string _long = "long";
    const string _byte = "byte";
    const string _sbyte = "sbyte";
    const string _ulong = "ulong";
    const string _short = "short";
    const string _ushort = "ushort";
    const string _float = "float";
    const string _double = "double";
    const string _decimal = "decimal";
    const string _bool = "bool";
    const string _char = "char";
    const string _string = "string";
    const string _dateTime = "DateTime";

    private static readonly HashSet<string> _supportedTypes =
    [
        _int, _uint,
        _long, _ulong,
        _byte, _sbyte,
        _short,  _ushort,
        _float, _double, _decimal,
        _bool,
        _char,
        _string,
        _dateTime
    ];

    private static readonly string[] _attrNames =
        [
            "GenerateBinarySerializerAttribute",
            "GenerateBinarySerializer",
        ];

    #endregion

    #region Types
    private static string GetTypeName(ITypeSymbol type) =>
        type.SpecialType switch
        {
            SpecialType.System_Int32 => _int,
            SpecialType.System_UInt32 => _uint,
            SpecialType.System_Int64 => _long,
            SpecialType.System_UInt64 => _ulong,
            SpecialType.System_Int16 => _short,
            SpecialType.System_UInt16 => _ushort,
            SpecialType.System_Byte => _byte,
            SpecialType.System_SByte => _sbyte,
            SpecialType.System_Single => _float,
            SpecialType.System_Double => _double,
            SpecialType.System_Decimal => _decimal,
            SpecialType.System_Boolean => _bool,
            SpecialType.System_Char => _char,
            SpecialType.System_String => _string,
            SpecialType.System_DateTime => _dateTime,
            _ => type.ToDisplayString()
        };

    private static bool IsSupportedType(ITypeSymbol type) =>
        IsSupportedType(GetTypeName(type));

    private static bool IsSupportedType(string typeName) =>
        _supportedTypes.Contains(typeName);

    #endregion

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Находим все классы с атрибутом [GenerateBinarySerializer]
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)
            .Collect();

        // Объединяем с дополнительной информацией о компиляции
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations);

        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    // Проверяем, что это объявление класса с атрибутами
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0;

    private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // Получаем семантическую модель
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
            return null;

        // Проверяем наличие атрибута [GenerateBinarySerializer]
        var attribute = namedTypeSymbol.GetAttributes()
            .FirstOrDefault(attr => _attrNames.Contains(attr.AttributeClass?.Name) ||
                                    attr.AttributeClass?.ToDisplayString() == "BinarySerializerGenerator.GenerateBinarySerializerAttribute");

        if (attribute == null)
            return null;

        // Получаем все публичные свойства
        var properties = new List<PropertyInfo>();
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                !propertySymbol.IsStatic &&
                propertySymbol.SetMethod != null)
            {
                var propertyType = propertySymbol.Type;

                if (IsSupportedType(propertyType))
                {
                    properties.Add(new PropertyInfo(
                        propertySymbol.Name,
                        GetTypeName(propertyType),
                        propertySymbol.Type));
                }
            }
        }

        if (properties.Count == 0) return null;

        var namespaceName = namedTypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : namedTypeSymbol.ContainingNamespace.ToDisplayString();

        return new ClassInfo(
            namespaceName,
            namedTypeSymbol.Name,
            properties,
            classDecl.Modifiers.ToString());
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassInfo> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        foreach (var classInfo in classes)
        {
            var source = GenerateSerializer(classInfo);
            var fileName = $"{classInfo.ClassName}.{nameof(BinarySerializerGenerator)}.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
    }

    #region Strings
    private static string GetWriteMethodDesc(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_DateTime)
            return "Write(Int64) // DateTime.Ticks";

        switch (type.SpecialType)
        {
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                {
                    return $"Write({type.SpecialType.ToString().TrimStart("System_")})";
                }
            default: return "Write";
        }
    }

    private static string GetWriteMethod(PropertyInfo prop)
    {
        if (prop.Type.SpecialType == SpecialType.System_DateTime)
            return $"{prop.Name}.Ticks";

        return prop.Name;
    }

    // НОВЫЙ МЕТОД: Описание метода чтения
    private static string GetReadMethodDesc(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_DateTime)
            return "ReadInt64() // DateTime.Ticks";

        switch (type.SpecialType)
        {
            case SpecialType.System_Int32: return "ReadInt32()";
            case SpecialType.System_UInt32: return "ReadUInt32()";
            case SpecialType.System_Int64: return "ReadInt64()";
            case SpecialType.System_UInt64: return "ReadUInt64()";
            case SpecialType.System_Int16: return "ReadInt16()";
            case SpecialType.System_UInt16: return "ReadUInt16()";
            case SpecialType.System_Byte: return "ReadByte()";
            case SpecialType.System_SByte: return "ReadSByte()";
            case SpecialType.System_Single: return "ReadSingle()";
            case SpecialType.System_Double: return "ReadDouble()";
            case SpecialType.System_Decimal: return "ReadDecimal()";
            case SpecialType.System_Boolean: return "ReadBoolean()";
            case SpecialType.System_Char: return "ReadChar()";
            case SpecialType.System_String: return "ReadString()";
            case SpecialType.System_DateTime: return "ReadInt64() // DateTime.Ticks";
            default: return "ReadString()";
        }
    }

    // НОВЫЙ МЕТОД: Получение кода для чтения свойства
    private static string GetReadMethod(PropertyInfo prop)
    {
        if (prop.Type.SpecialType == SpecialType.System_DateTime)
            return $"DateTime.FromBinary(reader.ReadInt64())";

        switch (prop.Type.SpecialType)
        {
            case SpecialType.System_Int32: return "reader.ReadInt32()";
            case SpecialType.System_UInt32: return "reader.ReadUInt32()";
            case SpecialType.System_Int64: return "reader.ReadInt64()";
            case SpecialType.System_UInt64: return "reader.ReadUInt64()";
            case SpecialType.System_Int16: return "reader.ReadInt16()";
            case SpecialType.System_UInt16: return "reader.ReadUInt16()";
            case SpecialType.System_Byte: return "reader.ReadByte()";
            case SpecialType.System_SByte: return "reader.ReadSByte()";
            case SpecialType.System_Single: return "reader.ReadSingle()";
            case SpecialType.System_Double: return "reader.ReadDouble()";
            case SpecialType.System_Decimal: return "reader.ReadDecimal()";
            case SpecialType.System_Boolean: return "reader.ReadBoolean()";
            case SpecialType.System_Char: return "reader.ReadChar()";
            case SpecialType.System_String: return "reader.ReadString()";
            default: return "reader.ReadString()";
        }
    }

    private static readonly IEnumerable<string> _usingDirectives = new List<string>()
    {
        "System",
        "System.IO",
        "System.Threading",
        "System.Threading.Tasks"
    };

    private static string UsingDirectivesString => string.Join("\n", _usingDirectives.Select(x => $"using {x};"));

    private static string GenerateSerializer(ClassInfo classInfo)
    {
        var propertyCode = string.Join("\n        ",
            classInfo.Properties.Select(p => $"writer.Write({GetWriteMethod(p)}); // {GetWriteMethodDesc(p.Type)}"
        ));

        // Код для десериализации свойств
        var deserializePropertyCode = string.Join("\n        ",
            classInfo.Properties.Select(p => $"result.{p.Name} = {GetReadMethod(p)}; // {GetReadMethodDesc(p.Type)}"
        ));

        return $@"
{UsingDirectivesString}

{classInfo.NamespaceString}

//SOURCE GENERATED {nameof(BinarySerializerGenerator)}
//DON'T CHANGE

public partial class {classInfo.ClassName}
{{
    /// <summary>
    /// Сериализует текущий объект в бинарный поток
    /// </summary>
    public void SerializeToBinary(Stream stream)
    {{
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        if (!stream.CanWrite)
            throw new ArgumentException(""Stream must be writable"", nameof(stream));

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        
        // Сериализация свойств
        {propertyCode}
    }}

    /// <summary>
    /// Десериализует объект из бинарного потока
    /// </summary>
    public static {classInfo.ClassName} DeserializeFromBinary(Stream stream)
    {{
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        if (!stream.CanRead)
            throw new ArgumentException(""Stream must be readable"", nameof(stream));

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        
        var result = new {classInfo.ClassName}();
        
        // Десериализация свойств
        {deserializePropertyCode}
        
        return result;
    }}

    /// <summary>
    /// Конвертирует объект в массив байт
    /// </summary>
    public byte[] ToByteArray()
    {{
        using var ms = new MemoryStream();
        SerializeToBinary(ms);
        return ms.ToArray();
    }}

    /// <summary>
    /// Создает объект из массива байт
    /// </summary>
    public static {classInfo.ClassName} FromByteArray(byte[] data)
    {{
        if (data == null)
            throw new ArgumentNullException(nameof(data));
            
        if (data.Length == 0)
            throw new ArgumentException(""Data cannot be empty"", nameof(data));

        using var ms = new MemoryStream(data);
        return DeserializeFromBinary(ms);
    }}

    /// <summary>
    /// Асинхронно сериализует объект в поток
    /// </summary>
    public async Task SerializeToBinaryAsync(Stream stream, CancellationToken cancellationToken = default)
    {{
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        if (!stream.CanWrite)
            throw new ArgumentException(""Stream must be writable"", nameof(stream));

        using var ms = new MemoryStream();
        SerializeToBinary(ms);
        ms.Position = 0;
        await ms.CopyToAsync(stream, 81920, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }}

    /// <summary>
    /// Асинхронно десериализует объект из потока
    /// </summary>
    public static async Task<{classInfo.ClassName}> DeserializeFromBinaryAsync(Stream stream, CancellationToken cancellationToken = default)
    {{
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        if (!stream.CanRead)
            throw new ArgumentException(""Stream must be readable"", nameof(stream));

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, 81920, cancellationToken);
        ms.Position = 0;
        return DeserializeFromBinary(ms);
    }}
}}";
    }

    #endregion
}