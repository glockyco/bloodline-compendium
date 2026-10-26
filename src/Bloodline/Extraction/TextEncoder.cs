using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Bloodline.Records;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace Bloodline.Extraction;

internal static class TextEncoder
{
    public static JsonNode Encode(FText value) =>
        value.TextHistory switch
        {
            FTextHistory.Base text => new JsonObject
            {
                [RecordFormat.Text] = text.SourceString,
                [RecordFormat.TextNamespace] = text.Namespace,
                [RecordFormat.TextKey] = text.Key,
            },
            FTextHistory.None text => new JsonObject
            {
                [RecordFormat.Text] = text.CultureInvariantString ?? "",
            },
            FTextHistory.NamedFormat text => Formatted(
                text.SourceFmt,
                Arguments(text.Arguments.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)))
            ),
            FTextHistory.OrderedFormat text => Formatted(
                text.SourceFmt,
                Arguments(
                    text.Arguments.Select(
                        (argument, index) =>
                            KeyValuePair.Create(
                                index.ToString(CultureInfo.InvariantCulture),
                                argument
                            )
                    )
                )
            ),
            FTextHistory.ArgumentFormat text => Formatted(
                text.SourceFmt,
                Arguments(
                    text.Arguments.Select(argument =>
                        KeyValuePair.Create(argument.ArgumentName, argument.ArgumentValue)
                    )
                )
            ),
            FTextHistory.FormatNumber text => Number(text, value.HistoryType),
            FTextHistory.AsDate text => Date(
                text.SourceDateTime,
                "yyyy-MM-dd",
                value.HistoryType,
                text.TimeZone,
                text.DateStyle,
                null
            ),
            FTextHistory.AsTime text => Date(
                text.SourceDateTime,
                "HH:mm:ss",
                value.HistoryType,
                text.TimeZone,
                null,
                text.TimeStyle
            ),
            FTextHistory.AsDateTime text => Date(
                text.SourceDateTime,
                "yyyy-MM-dd HH:mm:ss",
                value.HistoryType,
                text.TimeZone,
                text.DateStyle,
                text.TimeStyle
            ),
            FTextHistory.Transform text => Transform(text),
            FTextHistory.StringTableEntry text => new JsonObject
            {
                [RecordFormat.Text] = text.SourceString ?? $"{{{text.TableId.Text}:{text.Key}}}",
                [RecordFormat.TextTable] = text.TableId.Text,
                [RecordFormat.TextKey] = text.Key,
            },
            _ => new JsonObject { [RecordFormat.Unsupported] = $"TextHistory.{value.HistoryType}" },
        };

    private static JsonObject Arguments(
        IEnumerable<KeyValuePair<string, FFormatArgumentValue>> values
    )
    {
        var result = new JsonObject();
        foreach (var (name, argument) in values)
            result[name] = Argument(argument);
        return result;
    }

    private static JsonNode? Argument(FFormatArgumentValue argument) =>
        argument.Value switch
        {
            FText text => Encode(text),
            long number => JsonValue.Create(number),
            int number => JsonValue.Create(number),
            ulong number => JsonValue.Create(number),
            float number when float.IsFinite(number) => JsonValue.Create(number),
            double number when double.IsFinite(number) => JsonValue.Create(number),
            _ => new JsonObject { [RecordFormat.Unsupported] = $"TextArgument.{argument.Type}" },
        };

    private static JsonObject Formatted(FText source, JsonObject arguments)
    {
        var encoded = Encode(source);
        var pattern = RecordValues.Text(encoded) ?? "{format}";
        var result = Formatted(pattern, arguments);
        foreach (
            var key in new[]
            {
                RecordFormat.TextNamespace,
                RecordFormat.TextKey,
                RecordFormat.TextTable,
            }
        )
            if (encoded[key] is { } metadata)
                result[key] = metadata.DeepClone();
        if (RecordValues.Text(encoded) is null)
            result["source"] = encoded;
        return result;
    }

    private static JsonObject Formatted(
        string pattern,
        JsonObject arguments,
        string? display = null
    ) =>
        new()
        {
            [RecordFormat.Text] = display ?? Render(pattern, arguments),
            [RecordFormat.TextFormat] = pattern,
            [RecordFormat.TextArguments] = arguments,
        };

    internal static string Render(string pattern, JsonObject arguments)
    {
        var result = new StringBuilder(pattern.Length);
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (
                character == '`'
                && index + 1 < pattern.Length
                && pattern[index + 1] is '{' or '}' or '`' or '|'
            )
            {
                result.Append(pattern[++index]);
                continue;
            }
            if (character == '{')
            {
                var close = pattern.IndexOf('}', index + 1);
                if (close >= 0)
                {
                    var name = pattern[(index + 1)..close];
                    if (Display(arguments[name]) is { } replacement)
                    {
                        result.Append(replacement);
                        index = close;
                        continue;
                    }
                }
            }
            result.Append(character);
        }
        return result.ToString();
    }

    private static string? Display(JsonNode? value)
    {
        if (RecordValues.Text(value) is { } text)
            return text;
        return
            value is JsonValue scalar
            && scalar.GetValueKind() is System.Text.Json.JsonValueKind.Number
            ? scalar.ToJsonString()
            : null;
    }

    private static JsonObject Number(FTextHistory.FormatNumber text, ETextHistoryType history)
    {
        var value = Argument(text.SourceValue);
        var arguments = new JsonObject { ["value"] = value };
        var pattern = history switch
        {
            ETextHistoryType.AsPercent => "{value}%",
            ETextHistoryType.AsCurrency when !string.IsNullOrEmpty(text.CurrencyCode) =>
                "{currency} {value}",
            _ => "{value}",
        };
        if (history == ETextHistoryType.AsCurrency && !string.IsNullOrEmpty(text.CurrencyCode))
            arguments["currency"] = new JsonObject { [RecordFormat.Text] = text.CurrencyCode };
        var number = value is JsonValue scalar ? scalar.ToJsonString() : null;
        if (history == ETextHistoryType.AsPercent)
            number = text.SourceValue.Value switch
            {
                float source when float.IsFinite(source * 100F) => (source * 100F).ToString(
                    CultureInfo.InvariantCulture
                ),
                double source when double.IsFinite(source * 100D) => (source * 100D).ToString(
                    CultureInfo.InvariantCulture
                ),
                long source => ((decimal)source * 100M).ToString(CultureInfo.InvariantCulture),
                int source => ((decimal)source * 100M).ToString(CultureInfo.InvariantCulture),
                ulong source => ((decimal)source * 100M).ToString(CultureInfo.InvariantCulture),
                _ => null,
            };
        if (number is not null && text.FormatOptions is { } options)
            number = FormatNumber(number, options);
        var display = number is null
            ? null
            : history switch
            {
                ETextHistoryType.AsPercent => number + "%",
                ETextHistoryType.AsCurrency when !string.IsNullOrEmpty(text.CurrencyCode) =>
                    text.CurrencyCode + " " + number,
                _ => number,
            };
        var result = Formatted(pattern, arguments, display);
        result["history"] = history.ToString();
        if (text.FormatOptions is { } authored)
        {
            result["options"] = new JsonObject
            {
                ["alwaysSign"] = authored.AlwaysSign,
                ["useGrouping"] = authored.UseGrouping,
                ["roundingMode"] = authored.RoundingMode.ToString(),
                ["minimumIntegralDigits"] = authored.MinimumIntegralDigits,
                ["maximumIntegralDigits"] = authored.MaximumIntegralDigits,
                ["minimumFractionalDigits"] = authored.MinimumFractionalDigits,
                ["maximumFractionalDigits"] = authored.MaximumFractionalDigits,
            };
        }
        return result;
    }

    private static string FormatNumber(string value, FNumberFormattingOptions options)
    {
        if (
            !decimal.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var number
            )
        )
            return value;
        var minimum = Math.Clamp(options.MinimumFractionalDigits, 0, 28);
        var maximum = Math.Clamp(options.MaximumFractionalDigits, minimum, 28);
        var truncated = decimal.Round(number, maximum, MidpointRounding.ToZero);
        var step = 1M;
        for (var index = 0; index < maximum; index++)
            step /= 10M;
        number = options.RoundingMode switch
        {
            ERoundingMode.FromZero => number == truncated
                ? number
                : truncated + Math.Sign(number) * step,
            ERoundingMode.ToZero => truncated,
            ERoundingMode.ToNegativeInfinity => decimal.Round(
                number,
                maximum,
                MidpointRounding.ToNegativeInfinity
            ),
            ERoundingMode.ToPositiveInfinity => decimal.Round(
                number,
                maximum,
                MidpointRounding.ToPositiveInfinity
            ),
            ERoundingMode.HalfFromZero => decimal.Round(
                number,
                maximum,
                MidpointRounding.AwayFromZero
            ),
            ERoundingMode.HalfToZero when decimal.Abs(number - truncated) == step / 2M => truncated,
            _ => decimal.Round(number, maximum, MidpointRounding.ToEven),
        };
        var integral = new string('0', Math.Clamp(options.MinimumIntegralDigits, 1, 324));
        var format = (options.UseGrouping ? "#,##" : "") + integral;
        if (maximum > 0)
            format += "." + new string('0', minimum) + new string('#', maximum - minimum);
        return (options.AlwaysSign && number >= 0 ? "+" : "")
            + number.ToString(format, CultureInfo.InvariantCulture);
    }

    private static JsonObject Transform(FTextHistory.Transform text)
    {
        var source = Encode(text.SourceText);
        var display = Display(source);
        if (display is not null)
            display = text.TransformType switch
            {
                ETransformType.ToLower => display.ToLowerInvariant(),
                ETransformType.ToUpper => display.ToUpperInvariant(),
                _ => null,
            };
        var result = Formatted("{value}", new JsonObject { ["value"] = source }, display);
        result["history"] = ETextHistoryType.Transform.ToString();
        result["transform"] = text.TransformType.ToString();
        return result;
    }

    private static JsonObject Date(
        FDateTime source,
        string format,
        ETextHistoryType history,
        string? timeZone,
        EDateTimeStyle? dateStyle,
        EDateTimeStyle? timeStyle
    )
    {
        var valid =
            source.Ticks >= DateTime.MinValue.Ticks && source.Ticks <= DateTime.MaxValue.Ticks;
        var arguments = new JsonObject
        {
            ["value"] = valid
                ? JsonValue.Create(source.Ticks)
                : new JsonObject { [RecordFormat.Unsupported] = "TextDateTicks" },
        };
        var result = Formatted(
            "{value}",
            arguments,
            valid
                ? new DateTime(source.Ticks).ToString(format, CultureInfo.InvariantCulture)
                : "{value}"
        );
        result["history"] = history.ToString();
        if (timeZone is not null)
            result["timeZone"] = timeZone;
        if (dateStyle is not null)
            result["dateStyle"] = dateStyle.ToString();
        if (timeStyle is not null)
            result["timeStyle"] = timeStyle.ToString();
        return result;
    }
}
