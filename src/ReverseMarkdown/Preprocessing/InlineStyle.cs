using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;

namespace ReverseMarkdown.Preprocessing;

/// <summary>
/// Reads and edits an element's inline <c>style</c> attribute. AngleSharp's core package ships no
/// CSSOM (that lives in the optional <c>AngleSharp.Css</c> package), so these helpers parse the
/// attribute directly - enough for the markup that matters in practice, since Word, Outlook, Google
/// Docs and email templates emit their formatting inline.
/// </summary>
/// <remarks>
/// This reads the <c>style</c> attribute only. It does not apply the cascade, so a rule in a
/// <c>&lt;style&gt;</c> block or an external sheet is invisible to it. For that, supply the
/// converter with a browsing context configured for <c>AngleSharp.Css</c> and use
/// <c>ComputeCurrentStyle()</c> inside a custom step.
/// </remarks>
public static class InlineStyle
{
    /// <summary>
    /// Returns the value of an inline style declaration, or <c>null</c> when the element has no
    /// such declaration. Property names are matched case-insensitively.
    /// </summary>
    public static string? Get(IElement element, string property)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (string.IsNullOrWhiteSpace(property))
        {
            throw new ArgumentException("A property name is required.", nameof(property));
        }

        var name = property.Trim();
        foreach (var declaration in Parse(element.GetAttribute("style")))
        {
            if (string.Equals(declaration.Property, name, StringComparison.OrdinalIgnoreCase))
            {
                return declaration.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true when the element's inline style declares <paramref name="property"/> and the
    /// value contains <paramref name="value"/> (case-insensitive substring match, which covers
    /// shorthand values such as <c>text-decoration: underline line-through</c>).
    /// </summary>
    public static bool Has(IElement element, string property, string value)
    {
        var declared = Get(element, property);
        return declared is not null &&
               declared.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes the named declarations from the element's inline style, dropping the <c>style</c>
    /// attribute entirely once it is empty. Declarations that are kept are rewritten in
    /// <c>name: value</c> form.
    /// </summary>
    public static void Remove(IElement element, params string[] properties)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (properties is null || properties.Length == 0)
        {
            return;
        }

        var style = element.GetAttribute("style");
        if (string.IsNullOrEmpty(style))
        {
            return;
        }

        var kept = Parse(style)
            .Where(d => !properties.Any(p => string.Equals(d.Property, p?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (kept.Count == 0)
        {
            element.RemoveAttribute("style");
            return;
        }

        element.SetAttribute("style", string.Join("; ", kept.Select(d => $"{d.Property}: {d.Value}")));
    }

    /// <summary>
    /// Returns true when the inline style declares a bold weight: <c>bold</c>, <c>bolder</c>, or a
    /// numeric weight of 600 or more.
    /// </summary>
    public static bool IsBold(IElement element)
    {
        var weight = Get(element, "font-weight");
        if (weight is null)
        {
            return false;
        }

        weight = weight.Trim();
        if (weight.Equals("bold", StringComparison.OrdinalIgnoreCase) ||
            weight.Equals("bolder", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(weight, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) &&
               numeric >= 600;
    }

    /// <summary>Returns true when the inline style declares <c>italic</c> or <c>oblique</c>.</summary>
    public static bool IsItalic(IElement element)
    {
        var style = Get(element, "font-style")?.Trim();
        return style is not null &&
               (style.Equals("italic", StringComparison.OrdinalIgnoreCase) ||
                style.Equals("oblique", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Returns true when the inline style declares a line-through decoration.</summary>
    public static bool IsStruckThrough(IElement element) =>
        Has(element, "text-decoration", "line-through") ||
        Has(element, "text-decoration-line", "line-through");

    /// <summary>
    /// Returns true when the inline style hides the element (<c>display: none</c> or
    /// <c>visibility: hidden</c>).
    /// </summary>
    public static bool IsHidden(IElement element)
    {
        var display = Get(element, "display")?.Trim();
        if (display is not null && display.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var visibility = Get(element, "visibility")?.Trim();
        return visibility is not null &&
               (visibility.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
                visibility.Equals("collapse", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Splits an inline style attribute into declarations. Separators inside quotes and parentheses
    /// are ignored, so a <c>url(data:image/png;base64,...)</c> value survives intact.
    /// </summary>
    internal static List<(string Property, string Value)> Parse(string? style)
    {
        var declarations = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(style))
        {
            return declarations;
        }

        var start = 0;
        var depth = 0;
        var quote = '\0';

        for (var i = 0; i <= style!.Length; i++)
        {
            if (i == style.Length || (style[i] == ';' && depth == 0 && quote == '\0'))
            {
                AddDeclaration(declarations, style.Substring(start, i - start));
                start = i + 1;
                continue;
            }

            var c = style[i];
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }
            }
            else if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == '(')
            {
                depth++;
            }
            else if (c == ')' && depth > 0)
            {
                depth--;
            }
        }

        return declarations;
    }

    private static void AddDeclaration(List<(string, string)> declarations, string declaration)
    {
        var colon = IndexOfSeparator(declaration);
        if (colon <= 0)
        {
            return;
        }

        var property = declaration.Substring(0, colon).Trim();
        var value = declaration.Substring(colon + 1).Trim();
        if (property.Length > 0)
        {
            declarations.Add((property.ToLowerInvariant(), value));
        }
    }

    // The first colon outside parentheses and quotes: "background: url(data:image/png;base64,..)"
    // separates on the colon after "background", not the one inside url().
    private static int IndexOfSeparator(string declaration)
    {
        var depth = 0;
        var quote = '\0';

        for (var i = 0; i < declaration.Length; i++)
        {
            var c = declaration[i];
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }
            }
            else if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == '(')
            {
                depth++;
            }
            else if (c == ')' && depth > 0)
            {
                depth--;
            }
            else if (c == ':' && depth == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
