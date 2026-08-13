using System;
using System.Collections;
using System.Collections.Generic;
using AngleSharp.Dom;

namespace ReverseMarkdown.Preprocessing;

/// <summary>
/// An ordered pipeline of HTML transformations applied to the source markup before it is read into
/// the Markdown DOM. Configure it through <see cref="Config.Preprocess"/>:
/// <code>
/// var config = new Config();
/// config.Preprocess
///       .RemoveScripts()
///       .RemoveStyles()
///       .Remove("nav, footer, .advertisement")
///       .Unwrap("span, font")
///       .Rename("b", "strong");
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Steps run in the order they were added and each one sees the result of the previous step, so
/// ordering matters: <c>Rename("h1", "h2")</c> followed by <c>Remove("h2")</c> is not the same as
/// the reverse.
/// </para>
/// <para>
/// Steps operate on the whole parsed document (both <c>&lt;head&gt;</c> and <c>&lt;body&gt;</c>),
/// which is why <see cref="RemoveStyles"/> also reaches stylesheets the HTML5 parser hoists into
/// the head. They run before the <see cref="Config.HtmlFilterOptions"/> filters.
/// </para>
/// <para>
/// Configure the pipeline before converting. Mutating it while a conversion is in flight is not
/// supported; custom steps and predicates must themselves be thread-safe.
/// </para>
/// </remarks>
public sealed partial class HtmlPreprocessor : IEnumerable<IHtmlPreprocessStep>
{
    private readonly List<IHtmlPreprocessStep> _steps = new();
    private readonly List<IHtmlTextPreprocessStep> _textSteps = new();

    /// <summary>The configured DOM steps, in the order they run.</summary>
    public IReadOnlyList<IHtmlPreprocessStep> Steps => _steps;

    /// <summary>
    /// The configured text steps, in the order they run. These all run before the DOM steps, since
    /// they operate on the markup before it is parsed.
    /// </summary>
    public IReadOnlyList<IHtmlTextPreprocessStep> TextSteps => _textSteps;

    /// <summary>The total number of configured steps, text and DOM alike.</summary>
    public int Count => _steps.Count + _textSteps.Count;

    internal bool HasSteps => _steps.Count > 0 || _textSteps.Count > 0;

    /// <summary>Appends a custom step to the pipeline.</summary>
    public HtmlPreprocessor Add(IHtmlPreprocessStep step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Appends a custom step backed by a delegate. The delegate receives the document element
    /// (<c>&lt;html&gt;</c>) and transforms it in place.
    /// </summary>
    /// <param name="name">A short name used by <see cref="object.ToString"/> for diagnostics.</param>
    /// <param name="step">The transformation.</param>
    public HtmlPreprocessor Add(string name, Action<IElement> step)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A step name is required.", nameof(name));
        }

        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        return Add(new DelegateStep(name, step));
    }

    /// <summary>
    /// Appends a step that rewrites the raw HTML before it is parsed. Text steps always run first,
    /// in the order they were added, ahead of every DOM step.
    /// </summary>
    /// <remarks>
    /// Reach for this only when the transformation cannot be done on the DOM - repairing markup the
    /// HTML5 parser would otherwise discard, stripping templating artifacts, and the like. Anything
    /// structural is safer and clearer as a DOM step.
    /// </remarks>
    public HtmlPreprocessor AddText(string name, Func<string, string> step)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A step name is required.", nameof(name));
        }

        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        return AddText(new DelegateTextStep(name, step));
    }

    /// <summary>Appends a custom text step. Text steps run before every DOM step.</summary>
    public HtmlPreprocessor AddText(IHtmlTextPreprocessStep step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        _textSteps.Add(step);
        return this;
    }

    /// <summary>Removes all configured steps, text and DOM alike.</summary>
    public HtmlPreprocessor Clear()
    {
        _steps.Clear();
        _textSteps.Clear();
        return this;
    }

    /// <summary>
    /// Runs every step, in order, against <paramref name="root"/>. Called by
    /// <see cref="Converter.Convert"/> and <see cref="Converter.Parse(string)"/>; call it directly
    /// only when driving AngleSharp yourself.
    /// </summary>
    public void Apply(IElement root)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].Apply(root);
        }
    }

    /// <summary>
    /// Runs every text step, in order, against <paramref name="html"/> and returns the result.
    /// Called before the HTML is parsed.
    /// </summary>
    public string ApplyText(string html)
    {
        for (var i = 0; i < _textSteps.Count; i++)
        {
            html = _textSteps[i].Apply(html) ?? string.Empty;
        }

        return html;
    }

    /// <inheritdoc />
    public IEnumerator<IHtmlPreprocessStep> GetEnumerator() => _steps.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateStep : IHtmlPreprocessStep
    {
        private readonly Action<IElement> _step;

        public DelegateStep(string name, Action<IElement> step)
        {
            Name = name;
            _step = step;
        }

        public string Name { get; }

        public void Apply(IElement root) => _step(root);

        public override string ToString() => Name;
    }

    private sealed class DelegateTextStep : IHtmlTextPreprocessStep
    {
        private readonly Func<string, string> _step;

        public DelegateTextStep(string name, Func<string, string> step)
        {
            Name = name;
            _step = step;
        }

        public string Name { get; }

        public string Apply(string html) => _step(html);

        public override string ToString() => Name;
    }
}
