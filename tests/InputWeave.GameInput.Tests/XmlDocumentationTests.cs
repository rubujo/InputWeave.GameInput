using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class XmlDocumentationTests
{
    [TestMethod]
    public void PublicApiXmlDocumentationIsComplete()
    {
        Assembly assembly = typeof(GameInputClient).Assembly;
        IReadOnlyDictionary<string, XElement> members = LoadXmlDocumentation(assembly);

        foreach (Type type in assembly.GetExportedTypes().Where(static item => item.Namespace?.StartsWith("InputWeave.GameInput", StringComparison.Ordinal) == true))
        {
            if (IsCompilerGenerated(type))
            {
                continue;
            }

            XElement typeDocumentation = RequireDocumentedMember(members, "T:" + GetXmlTypeName(type));
            RequireNonEmptyElement(typeDocumentation, "summary", type.FullName ?? type.Name);
            if (ValidateDelegateDocumentation(type, typeDocumentation))
            {
                continue;
            }

            ValidateFields(type, members);
            ValidateProperties(type, members);
            ValidateConstructors(type, members);
            ValidateMethods(type, members);
        }
    }

    private static IReadOnlyDictionary<string, XElement> LoadXmlDocumentation(Assembly assembly)
    {
        string xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
        if (!File.Exists(xmlPath))
        {
            xmlPath = FindRepoFile("src/InputWeave.GameInput/bin/Release/net10.0-windows/InputWeave.GameInput.xml");
        }

        XDocument document = XDocument.Load(xmlPath);
        return document
            .Descendants("member")
            .Where(static item => item.Attribute("name") is not null)
            .ToDictionary(static item => item.Attribute("name")!.Value, StringComparer.Ordinal);
    }

    private static bool ValidateDelegateDocumentation(Type type, XElement typeDocumentation)
    {
        if (!typeof(Delegate).IsAssignableFrom(type))
        {
            return false;
        }

        MethodInfo invoke = type.GetMethod("Invoke") ?? throw new InvalidOperationException($"{type.FullName} 缺少 Invoke。");
        ValidateParameterDocumentation(typeDocumentation, invoke.GetParameters(), type.FullName ?? type.Name);
        if (invoke.ReturnType != typeof(void))
        {
            RequireNonEmptyElement(typeDocumentation, "returns", type.FullName ?? type.Name);
        }

        return true;
    }

    private static void ValidateFields(Type type, IReadOnlyDictionary<string, XElement> members)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (field.IsSpecialName)
            {
                continue;
            }

            XElement documentation = RequireDocumentedMember(members, "F:" + GetXmlTypeName(type) + "." + field.Name);
            RequireNonEmptyElement(documentation, "summary", field.Name);
        }
    }

    private static void ValidateProperties(Type type, IReadOnlyDictionary<string, XElement> members)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!HasPublicOrProtectedAccessor(property))
            {
                continue;
            }

            XElement documentation = RequireDocumentedMember(members, "P:" + GetXmlTypeName(type) + "." + property.Name);
            RequireNonEmptyElement(documentation, "summary", property.Name);
        }
    }

    private static void ValidateConstructors(Type type, IReadOnlyDictionary<string, XElement> members)
    {
        foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!IsPublicOrProtected(constructor))
            {
                continue;
            }

            ParameterInfo[] parameters = constructor.GetParameters();
            XElement documentation = RequireDocumentedMember(members, "M:" + GetXmlTypeName(type) + ".#ctor", parameters);
            RequireNonEmptyElement(documentation, "summary", constructor.Name);
            ValidateParameterDocumentation(documentation, parameters, constructor.Name);
        }
    }

    private static void ValidateMethods(Type type, IReadOnlyDictionary<string, XElement> members)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName || !IsPublicOrProtected(method))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            XElement documentation = RequireDocumentedMember(members, "M:" + GetXmlTypeName(type) + "." + method.Name, parameters);
            RequireNonEmptyElement(documentation, "summary", method.Name);
            ValidateParameterDocumentation(documentation, parameters, method.Name);
            if (method.ReturnType != typeof(void))
            {
                RequireNonEmptyElement(documentation, "returns", method.Name);
            }
        }
    }

    private static bool HasPublicOrProtectedAccessor(PropertyInfo property)
    {
        return property
            .GetAccessors(nonPublic: true)
            .Any(IsPublicOrProtected);
    }

    private static bool IsCompilerGenerated(MemberInfo member)
    {
        return member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            || member.Name.Contains('<', StringComparison.Ordinal);
    }

    private static bool IsPublicOrProtected(MethodBase method)
    {
        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }

    private static XElement RequireDocumentedMember(IReadOnlyDictionary<string, XElement> members, string idPrefix, IReadOnlyList<ParameterInfo>? parameters = null)
    {
        if ((parameters is null || parameters.Count == 0) && members.TryGetValue(idPrefix, out XElement? exact))
        {
            return exact;
        }

        XElement? overload = members
            .Where(item => item.Key.StartsWith(idPrefix + "(", StringComparison.Ordinal))
            .Select(static item => item.Value)
            .FirstOrDefault(item => HasParameterDocumentation(item, parameters ?? []));

        overload ??= members
            .Where(item => item.Key.StartsWith(idPrefix + "(", StringComparison.Ordinal))
            .Select(static item => item.Value)
            .FirstOrDefault();

        Assert.IsNotNull(overload, $"XML 文件缺少 {idPrefix}。");
        return overload;
    }

    private static void RequireNonEmptyElement(XElement member, string elementName, string displayName)
    {
        XElement? element = member.Element(elementName);
        Assert.IsNotNull(element, $"{displayName} 缺少 <{elementName}>。");
        Assert.IsFalse(string.IsNullOrWhiteSpace(element.Value), $"{displayName} 的 <{elementName}> 不可空白。");
    }

    private static void ValidateParameterDocumentation(XElement member, IReadOnlyList<ParameterInfo> parameters, string displayName)
    {
        foreach (ParameterInfo parameter in parameters)
        {
            Assert.IsTrue(
                HasParameterDocumentation(member, parameter),
                $"{displayName} 缺少參數 {parameter.Name} 的 <param>。");
        }
    }

    private static bool HasParameterDocumentation(XElement member, IReadOnlyList<ParameterInfo> parameters)
    {
        return parameters.All(parameter => HasParameterDocumentation(member, parameter));
    }

    private static bool HasParameterDocumentation(XElement member, ParameterInfo parameter)
    {
        return member
            .Elements("param")
            .Any(item => string.Equals(item.Attribute("name")?.Value, parameter.Name, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Value));
    }

    private static string GetXmlTypeName(Type type)
    {
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string FindRepoFile(string relativePath)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"找不到 repo 檔案：{relativePath}");
    }
}
