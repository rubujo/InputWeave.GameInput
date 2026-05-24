using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

try
{
    GeneratorOptions options = GeneratorOptions.Parse(args);
    string headerText = File.ReadAllText(options.HeaderPath, Encoding.UTF8);
    IReadOnlyList<EnumDefinition> enums = GameInputHeaderParser.ParseEnums(headerText);
    AbiManifest manifest = GameInputHeaderParser.ParseAbiManifest(headerText, enums);

    WriteUtf8NoBom(options.OutputPath, GameInputEnumWriter.Write(enums, options.Namespace));

    if (!string.IsNullOrWhiteSpace(options.ManifestPath))
    {
        string manifestJson = JsonSerializer.Serialize(manifest, ManifestJsonContext.Default.AbiManifest);
        WriteUtf8NoBom(options.ManifestPath, manifestJson + Environment.NewLine);
    }

    if (!string.IsNullOrWhiteSpace(options.InteropOutputDir))
    {
        GameInputInteropWriter.WriteAll(manifest, options.Namespace, options.InteropOutputDir);
    }

    string headerHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(options.HeaderPath)));
    Console.WriteLine($"已產生 {enums.Count} 個 GameInput enum。");
    if (!string.IsNullOrWhiteSpace(options.ManifestPath))
    {
        Console.WriteLine($"已產生 GameInput ABI manifest：{options.ManifestPath}");
    }

    if (!string.IsNullOrWhiteSpace(options.InteropOutputDir))
    {
        Console.WriteLine($"已產生 GameInput 低階 interop：{options.InteropOutputDir}");
    }

    Console.WriteLine($"GameInput.h SHA256: {headerHash}");
    return 0;
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Console.Error.WriteLine($"產生 GameInput 繫結失敗：{ex.Message}");
    return 1;
}

static void WriteUtf8NoBom(string path, string content)
{
    string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(path, NormalizeToCrlf(content), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

static string NormalizeToCrlf(string value)
{
    string normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
    return normalized.Replace("\n", "\r\n", StringComparison.Ordinal);
}

internal sealed record GeneratorOptions(string HeaderPath, string OutputPath, string? ManifestPath, string? InteropOutputDir, string Namespace)
{
    public static GeneratorOptions Parse(string[] args)
    {
        string? header = null;
        string? output = null;
        string? manifest = null;
        string? interopOutputDir = null;
        string ns = GeneratorDefaults.Namespace;

        for (int index = 0; index < args.Length; index++)
        {
            string current = args[index];
            string ReadValue()
            {
                if (index + 1 >= args.Length)
                {
                    throw new ArgumentException($"參數 {current} 缺少值。");
                }

                index++;
                return args[index];
            }

            switch (current)
            {
                case "--header":
                    header = ReadValue();
                    break;
                case "--output":
                    output = ReadValue();
                    break;
                case "--manifest":
                    manifest = ReadValue();
                    break;
                case "--interop-output-dir":
                    interopOutputDir = ReadValue();
                    break;
                case "--namespace":
                    ns = ReadValue();
                    break;
                case "--help":
                case "-h":
                case "/?":
                    throw new ArgumentException("用法：--header <GameInput.h> --output <GameInputEnums.g.cs> [--manifest <gameinput-abi-manifest.json>] [--interop-output-dir <Generated 目錄>] [--namespace <命名空間>]");
                default:
                    throw new ArgumentException($"未知參數：{current}");
            }
        }

        if (string.IsNullOrWhiteSpace(header))
        {
            throw new ArgumentException("必須指定 --header。");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ArgumentException("必須指定 --output。");
        }

        if (!File.Exists(header))
        {
            throw new FileNotFoundException("找不到 GameInput.h。", header);
        }

        return new GeneratorOptions(
            Path.GetFullPath(header),
            Path.GetFullPath(output),
            string.IsNullOrWhiteSpace(manifest) ? null : Path.GetFullPath(manifest),
            string.IsNullOrWhiteSpace(interopOutputDir) ? null : Path.GetFullPath(interopOutputDir),
            ns);
    }
}

internal static class GeneratorDefaults
{
    public const string Namespace = "InputWeave.GameInput.Interop";
}

internal static partial class GameInputHeaderParser
{
    private static readonly Regex s_enumRegex = EnumRegex();
    private static readonly Regex s_structRegex = StructRegex();
    private static readonly Regex s_interfaceRegex = InterfaceRegex();
    private static readonly Regex s_callbackRegex = CallbackRegex();
    private static readonly Regex s_hresultRegex = HResultRegex();
    private static readonly Regex s_constantRegex = ConstantRegex();

    public static IReadOnlyList<EnumDefinition> ParseEnums(string headerText)
    {
        List<EnumDefinition> enums = [];
        MatchCollection matches = s_enumRegex.Matches(headerText);

        foreach (Match match in matches)
        {
            string name = match.Groups["name"].Value;
            string body = match.Groups["body"].Value;
            bool isFlags = headerText.Contains($"DEFINE_ENUM_FLAG_OPERATORS({name})", StringComparison.Ordinal);
            IReadOnlyList<EnumMemberDefinition> members = ParseMembers(body);
            enums.Add(new EnumDefinition(name, isFlags, members));
        }

        if (enums.Count == 0)
        {
            throw new InvalidOperationException("GameInput.h 未找到任何 enum。");
        }

        return enums;
    }

    public static AbiManifest ParseAbiManifest(string headerText, IReadOnlyList<EnumDefinition> enums)
    {
        return new AbiManifest(
            ApiVersion: 3,
            Enums: enums,
            Structs: ParseStructs(headerText),
            Callbacks: ParseCallbacks(headerText),
            Interfaces: ParseInterfaces(headerText),
            HResults: ParseHResults(headerText),
            Constants: ParseConstants(headerText));
    }

    private static List<EnumMemberDefinition> ParseMembers(string body)
    {
        List<EnumMemberDefinition> members = [];
        long nextValue = 0;

        foreach (string rawLine in body.Split('\n'))
        {
            string line = rawLine.Split("//", 2, StringSplitOptions.None)[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            line = line.TrimEnd(',');
            string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            string name = parts[0];
            long value = parts.Length == 2 ? EvaluateInteger(parts[1]) : nextValue;
            members.Add(new EnumMemberDefinition(name, value));
            nextValue = value + 1;
        }

        return members;
    }

    private static long EvaluateInteger(string expression)
    {
        string value = expression.Trim().TrimEnd('L', 'l', 'U', 'u');

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            ulong parsed = ulong.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return parsed > int.MaxValue ? unchecked((int)parsed) : (long)parsed;
        }

        return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static List<StructDefinition> ParseStructs(string headerText)
    {
        List<StructDefinition> structs = [];
        foreach (Match match in s_structRegex.Matches(headerText))
        {
            string name = match.Groups["name"].Value;
            string body = match.Groups["body"].Value;
            structs.Add(new StructDefinition(name, NormalizeDeclarationLines(body)));
        }

        return structs;
    }

    private static List<CallbackDefinition> ParseCallbacks(string headerText)
    {
        List<CallbackDefinition> callbacks = [];
        foreach (Match match in s_callbackRegex.Matches(headerText))
        {
            callbacks.Add(new CallbackDefinition(
                match.Groups["name"].Value,
                NormalizeDeclarationLines(match.Groups["body"].Value)));
        }

        return callbacks;
    }

    private static List<InterfaceDefinition> ParseInterfaces(string headerText)
    {
        List<InterfaceDefinition> interfaces = [];
        foreach (Match match in s_interfaceRegex.Matches(headerText))
        {
            string name = match.Groups["name"].Value;
            string iid = match.Groups["iid"].Value.ToUpperInvariant();
            IReadOnlyList<InterfaceMethodDefinition> methods = ParseInterfaceMethods(match.Groups["body"].Value);
            interfaces.Add(new InterfaceDefinition(name, iid, methods));
        }

        return interfaces;
    }

    private static List<InterfaceMethodDefinition> ParseInterfaceMethods(string body)
    {
        List<InterfaceMethodDefinition> methods = [];
        List<string> current = [];
        foreach (string rawLine in body.Split('\n'))
        {
            string line = StripLineComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            current.Add(line);
            if (!line.EndsWith("PURE;", StringComparison.Ordinal))
            {
                continue;
            }

            string signature = NormalizeWhitespace(string.Join(" ", current));
            string name = ExtractMethodName(signature);
            methods.Add(new InterfaceMethodDefinition(name, signature));
            current.Clear();
        }

        return methods;
    }

    private static List<HResultDefinition> ParseHResults(string headerText)
    {
        List<HResultDefinition> hresults = [];
        foreach (Match match in s_hresultRegex.Matches(headerText))
        {
            hresults.Add(new HResultDefinition(match.Groups["name"].Value, match.Groups["value"].Value.ToUpperInvariant()));
        }

        return hresults;
    }

    private static List<ConstantDefinition> ParseConstants(string headerText)
    {
        List<ConstantDefinition> constants = [];
        foreach (Match match in s_constantRegex.Matches(headerText))
        {
            constants.Add(new ConstantDefinition(match.Groups["name"].Value, match.Groups["value"].Value));
        }

        return constants;
    }

    private static List<string> NormalizeDeclarationLines(string body)
    {
        List<string> lines = [];
        foreach (string rawLine in body.Split('\n'))
        {
            string line = StripLineComment(rawLine).Trim();
            if (line.Length == 0 || line is "union" or "{" or "}" || line.StartsWith('}'))
            {
                continue;
            }

            lines.Add(NormalizeWhitespace(line.TrimEnd(',')));
        }

        return lines;
    }

    private static string StripLineComment(string value)
    {
        return value.Split("//", 2, StringSplitOptions.None)[0];
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string ExtractMethodName(string signature)
    {
        Match method = Regex.Match(signature, @"IFACEMETHOD(?:_\([^,]+,\s*(?<name>\w+)\)|\((?<name>\w+)\))");
        if (!method.Success)
        {
            throw new InvalidOperationException($"無法解析 COM 方法名稱：{signature}");
        }

        return method.Groups["name"].Value;
    }

    [GeneratedRegex(@"enum\s+(?<name>GameInput\w+)\s*\{(?<body>.*?)\};", RegexOptions.Singleline)]
    private static partial Regex EnumRegex();

    [GeneratedRegex(@"struct\s+(?<name>GameInput\w+)\s*\{(?<body>.*?)\};", RegexOptions.Singleline)]
    private static partial Regex StructRegex();

    [GeneratedRegex(@"DECLARE_INTERFACE_IID_\(?(?<name>IGameInput\w*)\s*,\s*IUnknown\s*,\s*""(?<iid>[0-9A-Fa-f\-]+)""\)\s*\{(?<body>.*?)\};", RegexOptions.Singleline)]
    private static partial Regex InterfaceRegex();

    [GeneratedRegex(@"typedef\s+void\s+\(CALLBACK\*\s*(?<name>GameInput\w+Callback)\)\((?<body>.*?)\);", RegexOptions.Singleline)]
    private static partial Regex CallbackRegex();

    [GeneratedRegex(@"const\s+HRESULT\s+(?<name>GAMEINPUT_E_\w+)\s*=\s*_HRESULT_TYPEDEF_\(0x(?<value>[0-9A-Fa-f]+)L\);")]
    private static partial Regex HResultRegex();

    [GeneratedRegex(@"const\s+uint32_t\s+(?<name>GAMEINPUT_\w+)\s*=\s*(?<value>\d+);")]
    private static partial Regex ConstantRegex();
}

internal static class GameInputEnumWriter
{
    public static string Write(IReadOnlyList<EnumDefinition> enums, string ns)
    {
        StringBuilder builder = CreateGeneratedBuilder();
        builder.AppendLine("#pragma warning disable CA1027");
        builder.AppendLine("#pragma warning disable CA2217");
        builder.AppendLine();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");

        foreach (EnumDefinition enumDefinition in enums)
        {
            if (enumDefinition.IsFlags)
            {
                builder.AppendLine("    [System.Flags]");
            }

            builder.AppendLine("    public enum " + enumDefinition.Name);
            builder.AppendLine("    {");

            for (int index = 0; index < enumDefinition.Members.Count; index++)
            {
                EnumMemberDefinition member = enumDefinition.Members[index];
                string separator = index == enumDefinition.Members.Count - 1 ? string.Empty : ",";
                builder.AppendLine($"        {member.Name} = {FormatValue(member.Value)}{separator}");
            }

            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string FormatValue(long value)
    {
        if (value < 0)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        return value > int.MaxValue
            ? $"unchecked((int)0x{unchecked((uint)value):X8})"
            : value.ToString(CultureInfo.InvariantCulture);
    }

    internal static StringBuilder CreateGeneratedBuilder()
    {
        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// 此檔案由 InputWeave.GameInput.BindingsGenerator 依 Microsoft GameInput.h 產生。");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        return builder;
    }
}

internal static class GameInputInteropWriter
{
    private static readonly IReadOnlyDictionary<string, string> s_knownTypeAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["APP_LOCAL_DEVICE_ID"] = "AppLocalDeviceId",
        ["GUID"] = "Guid",
        ["bool"] = "bool",
        ["float"] = "float",
        ["int32_t"] = "int",
        ["int64_t"] = "long",
        ["uint8_t"] = "byte",
        ["uint16_t"] = "ushort",
        ["uint32_t"] = "uint",
        ["uint64_t"] = "ulong",
        ["size_t"] = "UIntPtr"
    };

    public static void WriteAll(AbiManifest manifest, string ns, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        WriteFile(outputDir, "GameInputConstants.g.cs", WriteConstants(manifest, ns));
        WriteFile(outputDir, "GameInputHResult.g.cs", WriteHResults(manifest, ns));
        WriteFile(outputDir, "GameInputIids.g.cs", WriteIids(manifest, ns));
        WriteFile(outputDir, "GameInputCallbacks.g.cs", WriteCallbacks(manifest, ns));
        WriteFile(outputDir, "GameInputStructs.g.cs", WriteStructs(manifest, ns));
        WriteFile(outputDir, "GameInputNativeInterfaces.g.cs", WriteInterfaces(manifest, ns));
    }

    private static void WriteFile(string outputDir, string fileName, string content)
    {
        string path = Path.Combine(outputDir, fileName);
        File.WriteAllText(path, NormalizeToCrlf(content), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string NormalizeToCrlf(string value)
    {
        string normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        return normalized.Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static string WriteConstants(AbiManifest manifest, string ns)
    {
        int hapticLocations = GetConstant(manifest, "GAMEINPUT_HAPTIC_MAX_LOCATIONS");
        int hapticAudioEndpoint = GetConstant(manifest, "GAMEINPUT_HAPTIC_MAX_AUDIO_ENDPOINT_ID_SIZE");
        int maxSwitchStates = GetConstant(manifest, "GAMEINPUT_MAX_SWITCH_STATES");

        StringBuilder builder = GameInputEnumWriter.CreateGeneratedBuilder();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// GameInput v3 繫結會用到的固定常數。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static class GameInputConstants");
        builder.AppendLine("    {");
        builder.AppendLine("        public const int ApiVersion = 3;");
        builder.AppendLine();
        builder.AppendLine("        public const string DllName = \"GameInput.dll\";");
        builder.AppendLine();
        builder.AppendLine("        public const ulong CurrentCallbackTokenValue = 0;");
        builder.AppendLine();
        builder.AppendLine($"        public const int HapticMaxLocations = {hapticLocations};");
        builder.AppendLine();
        builder.AppendLine($"        public const int HapticMaxAudioEndpointIdSize = {hapticAudioEndpoint};");
        builder.AppendLine();
        builder.AppendLine($"        public const int MaxSwitchStates = {maxSwitchStates};");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string WriteHResults(AbiManifest manifest, string ns)
    {
        StringBuilder builder = GameInputEnumWriter.CreateGeneratedBuilder();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// GameInput 原生 HRESULT 值。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static class GameInputHResult");
        builder.AppendLine("    {");
        foreach (HResultDefinition hresult in manifest.HResults)
        {
            builder.AppendLine($"        public const int {ToHResultName(hresult.Name)} = unchecked((int)0x{hresult.Value});");
            builder.AppendLine();
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string WriteIids(AbiManifest manifest, string ns)
    {
        StringBuilder builder = GameInputEnumWriter.CreateGeneratedBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// GameInput v3 COM 介面的 IID。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static class GameInputIids");
        builder.AppendLine("    {");
        foreach (InterfaceDefinition interfaceDefinition in manifest.Interfaces)
        {
            builder.AppendLine($"        public static readonly Guid {interfaceDefinition.Name} = new(\"{interfaceDefinition.Iid}\");");
            builder.AppendLine();
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string WriteCallbacks(AbiManifest manifest, string ns)
    {
        StringBuilder builder = GameInputEnumWriter.CreateGeneratedBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Runtime.InteropServices;");
        builder.AppendLine();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");
        foreach (CallbackDefinition callback in manifest.Callbacks)
        {
            builder.AppendLine("    [UnmanagedFunctionPointer(CallingConvention.Winapi)]");
            builder.AppendLine("    " + GetCallbackDeclaration(callback.Name));
            builder.AppendLine();
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string WriteStructs(AbiManifest manifest, string ns)
    {
        StringBuilder builder = GameInputEnumWriter.CreateGeneratedBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Runtime.InteropServices;");
        builder.AppendLine();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");
        WriteAppLocalDeviceId(builder);

        foreach (StructDefinition structDefinition in manifest.Structs)
        {
            WriteStruct(builder, structDefinition);
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void WriteAppLocalDeviceId(StringBuilder builder)
    {
        builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
        builder.AppendLine("    public unsafe struct AppLocalDeviceId");
        builder.AppendLine("    {");
        builder.AppendLine("        public const int Size = 32;");
        builder.AppendLine();
        builder.AppendLine("        public fixed byte Value[Size];");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static string WriteInterfaces(AbiManifest manifest, string ns)
    {
        StringBuilder builder = GameInputEnumWriter.CreateGeneratedBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Runtime.InteropServices;");
        builder.AppendLine();
        builder.AppendLine("namespace " + ns);
        builder.AppendLine("{");
        foreach (InterfaceDefinition interfaceDefinition in manifest.Interfaces)
        {
            builder.AppendLine("    [ComImport]");
            builder.AppendLine($"    [Guid(\"{interfaceDefinition.Iid}\")]");
            builder.AppendLine("    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]");
            builder.AppendLine($"    public interface {interfaceDefinition.Name}");
            builder.AppendLine("    {");
            foreach (InterfaceMethodDefinition method in interfaceDefinition.Methods)
            {
                WriteInterfaceMethod(builder, interfaceDefinition.Name, method.Name);
            }

            TrimLastBlankLine(builder);
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static int GetConstant(AbiManifest manifest, string name)
    {
        ConstantDefinition? constant = manifest.Constants.FirstOrDefault(item => item.Name == name);
        if (constant is null)
        {
            throw new InvalidOperationException($"GameInput.h 缺少必要常數 {name}。");
        }

        return int.Parse(constant.Value, CultureInfo.InvariantCulture);
    }

    private static string GetCallbackDeclaration(string callbackName)
    {
        return callbackName switch
        {
            "GameInputReadingCallback" => "public delegate void GameInputReadingCallback(ulong callbackToken, IntPtr context, IGameInputReading reading);",
            "GameInputDeviceCallback" => "public delegate void GameInputDeviceCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus);",
            "GameInputSystemButtonCallback" => "public delegate void GameInputSystemButtonCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons);",
            "GameInputKeyboardLayoutCallback" => "public delegate void GameInputKeyboardLayoutCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout);",
            _ => throw new InvalidOperationException($"未支援的 GameInput callback：{callbackName}。")
        };
    }

    private static void WriteStruct(StringBuilder builder, StructDefinition structDefinition)
    {
        if (structDefinition.Name == "GameInputForceFeedbackParams")
        {
            WriteForceFeedbackParams(builder, structDefinition);
            return;
        }

        bool requiresUnsafe = structDefinition.Name == "GameInputControllerSwitchInfo";
        string declaration = requiresUnsafe
            ? $"    public unsafe struct {structDefinition.Name}"
            : $"    public struct {structDefinition.Name}";

        if (structDefinition.Name == "GameInputHapticInfo")
        {
            builder.AppendLine("    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]");
        }
        else
        {
            builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
        }

        builder.AppendLine(declaration);
        builder.AppendLine("    {");

        foreach (string member in structDefinition.Members)
        {
            WriteStructMember(builder, member);
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void WriteForceFeedbackParams(StringBuilder builder, StructDefinition structDefinition)
    {
        string[] expectedMembers =
        {
            "GameInputForceFeedbackEffectKind kind;",
            "GameInputForceFeedbackConstantParams constant;",
            "GameInputForceFeedbackRampParams ramp;",
            "GameInputForceFeedbackPeriodicParams sineWave;",
            "GameInputForceFeedbackPeriodicParams squareWave;",
            "GameInputForceFeedbackPeriodicParams triangleWave;",
            "GameInputForceFeedbackPeriodicParams sawtoothUpWave;",
            "GameInputForceFeedbackPeriodicParams sawtoothDownWave;",
            "GameInputForceFeedbackConditionParams spring;",
            "GameInputForceFeedbackConditionParams friction;",
            "GameInputForceFeedbackConditionParams damper;",
            "GameInputForceFeedbackConditionParams inertia;"
        };

        if (!expectedMembers.SequenceEqual(structDefinition.Members))
        {
            throw new InvalidOperationException("GameInputForceFeedbackParams union 結構與 generator 預期不一致。");
        }

        builder.AppendLine("    [StructLayout(LayoutKind.Explicit)]");
        builder.AppendLine("    public struct GameInputForceFeedbackParams");
        builder.AppendLine("    {");
        builder.AppendLine("        [FieldOffset(0)]");
        builder.AppendLine("        public GameInputForceFeedbackEffectKind Kind;");
        builder.AppendLine();
        foreach (string member in expectedMembers.Skip(1))
        {
            NativeMember parsed = ParseNativeMember(member);
            builder.AppendLine("        [FieldOffset(8)]");
            builder.AppendLine($"        public {ToManagedType(parsed.NativeType)} {ToPascalCase(parsed.Name)};");
            builder.AppendLine();
        }

        TrimLastBlankLine(builder);
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void WriteStructMember(StringBuilder builder, string member)
    {
        NativeMember parsed = ParseNativeMember(member);

        if (parsed.Name == "labels" && parsed.NativeType == "GameInputLabel" && parsed.ArraySize == "GAMEINPUT_MAX_SWITCH_STATES")
        {
            builder.AppendLine("        public fixed int Labels[GameInputConstants.MaxSwitchStates];");
            builder.AppendLine();
            return;
        }

        if (parsed.Name == "audioEndpointId" && parsed.NativeType == "wchar_t" && parsed.ArraySize == "GAMEINPUT_HAPTIC_MAX_AUDIO_ENDPOINT_ID_SIZE")
        {
            builder.AppendLine("        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = GameInputConstants.HapticMaxAudioEndpointIdSize)]");
            builder.AppendLine("        public string AudioEndpointId;");
            builder.AppendLine();
            return;
        }

        if (parsed.Name == "locations" && parsed.NativeType == "GUID" && parsed.ArraySize == "GAMEINPUT_HAPTIC_MAX_LOCATIONS")
        {
            builder.AppendLine("        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GameInputConstants.HapticMaxLocations)]");
            builder.AppendLine("        public Guid[] Locations;");
            builder.AppendLine();
            return;
        }

        string managedType = ToManagedType(parsed.NativeType);
        string managedName = ToPascalCase(parsed.Name);
        if (managedType == "bool")
        {
            builder.AppendLine("        [MarshalAs(UnmanagedType.I1)]");
        }

        builder.AppendLine($"        public {managedType} {managedName};");
        builder.AppendLine();
    }

    private static NativeMember ParseNativeMember(string member)
    {
        string cleaned = StripAnnotations(member).Trim().TrimEnd(';');
        Match arrayMatch = Regex.Match(cleaned, @"^(?<type>.+?)\s+(?<name>\w+)\[(?<size>\w+)\]$");
        if (arrayMatch.Success)
        {
            return new NativeMember(
                arrayMatch.Groups["type"].Value.Trim(),
                arrayMatch.Groups["name"].Value.Trim(),
                arrayMatch.Groups["size"].Value.Trim());
        }

        Match fieldMatch = Regex.Match(cleaned, @"^(?<type>.+?)\s+(?<name>\w+)$");
        if (!fieldMatch.Success)
        {
            throw new InvalidOperationException($"無法解析 struct 欄位：{member}");
        }

        return new NativeMember(fieldMatch.Groups["type"].Value.Trim(), fieldMatch.Groups["name"].Value.Trim(), null);
    }

    private static string StripAnnotations(string value)
    {
        string current = value.Trim();
        while (true)
        {
            string next = Regex.Replace(current, @"^_\w+(?:_\w+)*_(?:\([^)]*\))?\s+", string.Empty);
            if (next == current)
            {
                return current.Replace("const ", string.Empty, StringComparison.Ordinal).Trim();
            }

            current = next;
        }
    }

    private static string ToManagedType(string nativeType)
    {
        string normalized = StripAnnotations(nativeType)
            .Replace("const ", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (normalized.EndsWith("*", StringComparison.Ordinal))
        {
            return "IntPtr";
        }

        if (s_knownTypeAliases.TryGetValue(normalized, out string? alias))
        {
            return alias;
        }

        if (normalized.StartsWith("GameInput", StringComparison.Ordinal))
        {
            return normalized;
        }

        throw new InvalidOperationException($"未支援的 native type：{nativeType}");
    }

    private static string ToHResultName(string nativeName)
    {
        string value = nativeName["GAMEINPUT_E_".Length..].ToLowerInvariant();
        return string.Concat(value.Split('_').Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        string result = char.ToUpperInvariant(value[0]) + value[1..];
        return result
            .Replace("Dpad", "DPad", StringComparison.Ordinal)
            .Replace("Pnp", "Pnp", StringComparison.Ordinal)
            .Replace("Id", "Id", StringComparison.Ordinal);
    }

    private static void WriteInterfaceMethod(StringBuilder builder, string interfaceName, string methodName)
    {
        string key = interfaceName + "." + methodName;
        string[] lines = key switch
        {
            "IGameInput.GetCurrentTimestamp" => Method("ulong GetCurrentTimestamp();"),
            "IGameInput.GetCurrentReading" => Method("int GetCurrentReading(GameInputKind inputKind, IGameInputDevice? device, out IGameInputReading? reading);"),
            "IGameInput.GetNextReading" => Method("int GetNextReading(IGameInputReading referenceReading, GameInputKind inputKind, IGameInputDevice? device, out IGameInputReading? reading);"),
            "IGameInput.GetPreviousReading" => Method("int GetPreviousReading(IGameInputReading referenceReading, GameInputKind inputKind, IGameInputDevice? device, out IGameInputReading? reading);"),
            "IGameInput.RegisterReadingCallback" => Method("int RegisterReadingCallback(IGameInputDevice? device, GameInputKind inputKind, IntPtr context, GameInputReadingCallback callbackFunc, out ulong callbackToken);"),
            "IGameInput.RegisterDeviceCallback" => Method("int RegisterDeviceCallback(IGameInputDevice? device, GameInputKind inputKind, GameInputDeviceStatus statusFilter, GameInputEnumerationKind enumerationKind, IntPtr context, GameInputDeviceCallback callbackFunc, out ulong callbackToken);"),
            "IGameInput.RegisterSystemButtonCallback" => Method("int RegisterSystemButtonCallback(IGameInputDevice? device, GameInputSystemButtons buttonFilter, IntPtr context, GameInputSystemButtonCallback callbackFunc, out ulong callbackToken);"),
            "IGameInput.RegisterKeyboardLayoutCallback" => Method("int RegisterKeyboardLayoutCallback(IGameInputDevice? device, IntPtr context, GameInputKeyboardLayoutCallback callbackFunc, out ulong callbackToken);"),
            "IGameInput.StopCallback" => Method("void StopCallback(ulong callbackToken);"),
            "IGameInput.UnregisterCallback" => BoolMethod("bool UnregisterCallback(ulong callbackToken);"),
            "IGameInput.CreateDispatcher" => Method("int CreateDispatcher(out IGameInputDispatcher? dispatcher);"),
            "IGameInput.FindDeviceFromId" => Method("int FindDeviceFromId(ref AppLocalDeviceId value, out IGameInputDevice? device);"),
            "IGameInput.FindDeviceFromPlatformString" => Method("int FindDeviceFromPlatformString([MarshalAs(UnmanagedType.LPWStr)] string value, out IGameInputDevice? device);"),
            "IGameInput.SetFocusPolicy" => Method("void SetFocusPolicy(GameInputFocusPolicy policy);"),
            "IGameInput.CreateAggregateDevice" => Method("int CreateAggregateDevice(GameInputKind inputKind, out AppLocalDeviceId deviceId);"),
            "IGameInput.DisableAggregateDevice" => Method("int DisableAggregateDevice(ref AppLocalDeviceId deviceId);"),
            "IGameInputRawDeviceReport.GetDevice" => Method("void GetDevice(out IGameInputDevice? device);"),
            "IGameInputRawDeviceReport.GetReportInfo" => Method("void GetReportInfo(out GameInputRawDeviceReportInfo reportInfo);"),
            "IGameInputRawDeviceReport.GetRawDataSize" => Method("UIntPtr GetRawDataSize();"),
            "IGameInputRawDeviceReport.GetRawData" => Method("UIntPtr GetRawData(UIntPtr bufferSize, IntPtr buffer);"),
            "IGameInputRawDeviceReport.SetRawData" => BoolMethod("bool SetRawData(UIntPtr bufferSize, IntPtr buffer);"),
            "IGameInputReading.GetInputKind" => Method("GameInputKind GetInputKind();"),
            "IGameInputReading.GetTimestamp" => Method("ulong GetTimestamp();"),
            "IGameInputReading.GetDevice" => Method("void GetDevice(out IGameInputDevice? device);"),
            "IGameInputReading.GetControllerAxisCount" => Method("uint GetControllerAxisCount();"),
            "IGameInputReading.GetControllerAxisState" => Method("uint GetControllerAxisState(uint stateArrayCount, [Out] float[] stateArray);"),
            "IGameInputReading.GetControllerButtonCount" => Method("uint GetControllerButtonCount();"),
            "IGameInputReading.GetControllerButtonState" => Method("uint GetControllerButtonState(uint stateArrayCount, [Out] byte[] stateArray);"),
            "IGameInputReading.GetControllerSwitchCount" => Method("uint GetControllerSwitchCount();"),
            "IGameInputReading.GetControllerSwitchState" => Method("uint GetControllerSwitchState(uint stateArrayCount, [Out] GameInputSwitchPosition[] stateArray);"),
            "IGameInputReading.GetKeyCount" => Method("uint GetKeyCount();"),
            "IGameInputReading.GetKeyState" => Method("uint GetKeyState(uint stateArrayCount, [Out] GameInputKeyState[] stateArray);"),
            "IGameInputReading.GetMouseState" => BoolMethod("bool GetMouseState(out GameInputMouseState state);"),
            "IGameInputReading.GetSensorsState" => BoolMethod("bool GetSensorsState(out GameInputSensorsState state);"),
            "IGameInputReading.GetArcadeStickState" => BoolMethod("bool GetArcadeStickState(out GameInputArcadeStickState state);"),
            "IGameInputReading.GetFlightStickState" => BoolMethod("bool GetFlightStickState(out GameInputFlightStickState state);"),
            "IGameInputReading.GetGamepadState" => BoolMethod("bool GetGamepadState(out GameInputGamepadState state);"),
            "IGameInputReading.GetRacingWheelState" => BoolMethod("bool GetRacingWheelState(out GameInputRacingWheelState state);"),
            "IGameInputReading.GetRawReport" => BoolMethod("bool GetRawReport(out IGameInputRawDeviceReport? report);"),
            "IGameInputDevice.GetDeviceInfo" => Method("int GetDeviceInfo(out IntPtr info);"),
            "IGameInputDevice.GetHapticInfo" => Method("int GetHapticInfo(IntPtr info);"),
            "IGameInputDevice.GetDeviceStatus" => Method("GameInputDeviceStatus GetDeviceStatus();"),
            "IGameInputDevice.CreateForceFeedbackEffect" => Method("int CreateForceFeedbackEffect(uint motorIndex, IntPtr parameters, out IGameInputForceFeedbackEffect? effect);"),
            "IGameInputDevice.IsForceFeedbackMotorPoweredOn" => BoolMethod("bool IsForceFeedbackMotorPoweredOn(uint motorIndex);"),
            "IGameInputDevice.SetForceFeedbackMotorGain" => Method("void SetForceFeedbackMotorGain(uint motorIndex, float masterGain);"),
            "IGameInputDevice.SetRumbleState" => Method("void SetRumbleState(IntPtr parameters);"),
            "IGameInputDevice.DirectInputEscape" => Method("int DirectInputEscape(uint command, IntPtr bufferIn, uint bufferInSize, IntPtr bufferOut, uint bufferOutSize, out uint bufferOutSizeWritten);"),
            "IGameInputDevice.CreateInputMapper" => Method("int CreateInputMapper(out IGameInputMapper? inputMapper);"),
            "IGameInputDevice.GetExtraAxisCount" => Method("int GetExtraAxisCount(GameInputKind inputKind, out uint extraAxisCount);"),
            "IGameInputDevice.GetExtraButtonCount" => Method("int GetExtraButtonCount(GameInputKind inputKind, out uint extraButtonCount);"),
            "IGameInputDevice.GetExtraAxisIndexes" => Method("int GetExtraAxisIndexes(GameInputKind inputKind, uint extraAxisCount, [Out] byte[] extraAxisIndexes);"),
            "IGameInputDevice.GetExtraButtonIndexes" => Method("int GetExtraButtonIndexes(GameInputKind inputKind, uint extraButtonCount, [Out] byte[] extraButtonIndexes);"),
            "IGameInputDevice.CreateRawDeviceReport" => Method("int CreateRawDeviceReport(uint reportId, GameInputRawDeviceReportKind reportKind, out IGameInputRawDeviceReport? report);"),
            "IGameInputDevice.SendRawDeviceOutput" => Method("int SendRawDeviceOutput(IGameInputRawDeviceReport report);"),
            "IGameInputDispatcher.Dispatch" => BoolMethod("bool Dispatch(ulong quotaInMicroseconds);"),
            "IGameInputDispatcher.OpenWaitHandle" => Method("int OpenWaitHandle(out IntPtr waitHandle);"),
            "IGameInputForceFeedbackEffect.GetDevice" => Method("void GetDevice(out IGameInputDevice? device);"),
            "IGameInputForceFeedbackEffect.GetMotorIndex" => Method("uint GetMotorIndex();"),
            "IGameInputForceFeedbackEffect.GetGain" => Method("float GetGain();"),
            "IGameInputForceFeedbackEffect.SetGain" => Method("void SetGain(float gain);"),
            "IGameInputForceFeedbackEffect.GetParams" => Method("void GetParams(IntPtr parameters);"),
            "IGameInputForceFeedbackEffect.SetParams" => BoolMethod("bool SetParams(IntPtr parameters);"),
            "IGameInputForceFeedbackEffect.GetState" => Method("GameInputFeedbackEffectState GetState();"),
            "IGameInputForceFeedbackEffect.SetState" => Method("void SetState(GameInputFeedbackEffectState state);"),
            "IGameInputMapper.GetArcadeStickButtonMappingInfo" => BoolMethod("bool GetArcadeStickButtonMappingInfo(GameInputArcadeStickButtons buttonElement, IntPtr mapping);"),
            "IGameInputMapper.GetFlightStickAxisMappingInfo" => BoolMethod("bool GetFlightStickAxisMappingInfo(GameInputFlightStickAxes axisElement, IntPtr mapping);"),
            "IGameInputMapper.GetFlightStickButtonMappingInfo" => BoolMethod("bool GetFlightStickButtonMappingInfo(GameInputFlightStickButtons buttonElement, IntPtr mapping);"),
            "IGameInputMapper.GetGamepadAxisMappingInfo" => BoolMethod("bool GetGamepadAxisMappingInfo(GameInputGamepadAxes axisElement, IntPtr mapping);"),
            "IGameInputMapper.GetGamepadButtonMappingInfo" => BoolMethod("bool GetGamepadButtonMappingInfo(GameInputGamepadButtons buttonElement, IntPtr mapping);"),
            "IGameInputMapper.GetRacingWheelAxisMappingInfo" => BoolMethod("bool GetRacingWheelAxisMappingInfo(GameInputRacingWheelAxes axisElement, IntPtr mapping);"),
            "IGameInputMapper.GetRacingWheelButtonMappingInfo" => BoolMethod("bool GetRacingWheelButtonMappingInfo(GameInputRacingWheelButtons buttonElement, IntPtr mapping);"),
            _ => throw new InvalidOperationException($"未支援的 COM 方法：{key}。")
        };

        foreach (string line in lines)
        {
            builder.AppendLine(line);
        }

        builder.AppendLine();
    }

    private static string[] Method(string declaration)
    {
        return new[]
        {
            "        [PreserveSig]",
            "        " + declaration
        };
    }

    private static string[] BoolMethod(string declaration)
    {
        return new[]
        {
            "        [PreserveSig]",
            "        [return: MarshalAs(UnmanagedType.I1)]",
            "        " + declaration
        };
    }

    private static void TrimLastBlankLine(StringBuilder builder)
    {
        string blankLine = Environment.NewLine + Environment.NewLine;
        string value = builder.ToString();
        if (value.EndsWith(blankLine, StringComparison.Ordinal))
        {
            builder.Length -= Environment.NewLine.Length;
        }
    }
}

internal sealed record NativeMember(string NativeType, string Name, string? ArraySize);

internal sealed record EnumDefinition(string Name, bool IsFlags, IReadOnlyList<EnumMemberDefinition> Members);

internal sealed record EnumMemberDefinition(string Name, long Value);

internal sealed record StructDefinition(string Name, IReadOnlyList<string> Members);

internal sealed record CallbackDefinition(string Name, IReadOnlyList<string> Parameters);

internal sealed record InterfaceDefinition(string Name, string Iid, IReadOnlyList<InterfaceMethodDefinition> Methods);

internal sealed record InterfaceMethodDefinition(string Name, string Signature);

internal sealed record HResultDefinition(string Name, string Value);

internal sealed record ConstantDefinition(string Name, string Value);

internal sealed record AbiManifest(
    int ApiVersion,
    IReadOnlyList<EnumDefinition> Enums,
    IReadOnlyList<StructDefinition> Structs,
    IReadOnlyList<CallbackDefinition> Callbacks,
    IReadOnlyList<InterfaceDefinition> Interfaces,
    IReadOnlyList<HResultDefinition> HResults,
    IReadOnlyList<ConstantDefinition> Constants);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AbiManifest))]
internal sealed partial class ManifestJsonContext : JsonSerializerContext
{
}
