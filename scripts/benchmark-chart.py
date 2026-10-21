#!/usr/bin/env python3
"""Draws docs/public/benchmark-6.2.1-6.3.0.svg from the corpus results in
src/ReverseMarkdown.Benchmark/Benchmark.md.

The fixtures differ in size by three orders of magnitude, so the chart plots the percentage change
from 6.2.1 rather than absolute values. Update ROWS from a new BenchmarkDotNet run, then:

    python3 scripts/benchmark-chart.py docs/public/benchmark-6.2.1-6.3.0.svg
"""
import sys

# (fixture, size, mean 6.2.1, mean 6.3.0, allocated 6.2.1, allocated 6.3.0). Units cancel out, so
# any consistent unit works; these are microseconds and KB as BenchmarkDotNet reports them.
ROWS = [
    ("nuxt-example", "3.5 KB", 72.66, 62.88, 133.88, 105.6),
    ("vuejs-docs", "110 KB", 4388.22, 4315.10, 4937.64, 3374.99),
    ("wikipedia-small", "162 KB", 7494.42, 7420.65, 7881.74, 6337.24),
    ("mdn-array", "230 KB", 9772.04, 9693.22, 18105.25, 8196.53),
    ("react-learn", "259 KB", 10317.36, 10010.08, 14329.59, 7135.4),
    ("github-markdown-complete", "420 KB", 11572.56, 9787.82, 25274.92, 10734.01),
    ("wikipedia-largest", "1.77 MB", 175059.67, 176397.39, 227855.84, 203417.78),
]

# Time changes inside this band are treated as unchanged and shaded on the time panel.
NOISE = 5.0

WIDTH = 920
INK, SECONDARY, MUTED, GRID, BAND, MARK = "#1E293B", "#475569", "#64748B", "#E2E8F0", "#EEF2F6", "#0D8D85"
FONT = "-apple-system, Segoe UI, Helvetica, Arial, sans-serif"
LABEL_RIGHT, X0, X1 = 300, 320, 860
LOW, HIGH = -70.0, 10.0
ROW, BAR = 36, 20
MINUS = "−"


def pct(old, new):
    return (new - old) / old * 100


def sx(value):
    return X0 + (value - LOW) / (HIGH - LOW) * (X1 - X0)


ZERO = sx(0)


def bar(y, value):
    """A bar from the zero baseline, rounded only at its data end."""
    x = sx(value)
    sign = -1 if value < 0 else 1
    r = min(4, abs(x - ZERO) / 2)
    return (f'<path d="M{ZERO:.1f},{y} H{x - sign * r:.1f} Q{x:.1f},{y} {x:.1f},{y + r:.1f} '
            f'V{y + BAR - r:.1f} Q{x:.1f},{y + BAR} {x - sign * r:.1f},{y + BAR} H{ZERO:.1f} Z" '
            f'fill="{MARK}"/>')


def signed(value):
    return f"{value:+.0f}%".replace("-", MINUS)


def panel(out, top, title, values, noise):
    out.append(f'<text x="40" y="{top}" font-size="17" font-weight="700" fill="{INK}">{title}</text>')
    out.append(f'<text x="{X1}" y="{top}" font-size="12" fill="{MUTED}" text-anchor="end">'
               f'change vs 6.2.1, lower is better</text>')
    plot_top = top + 18
    plot_height = ROW * len(ROWS)
    if noise:
        out.append(f'<rect x="{sx(-NOISE):.1f}" y="{plot_top}" width="{sx(NOISE) - sx(-NOISE):.1f}" '
                   f'height="{plot_height}" fill="{BAND}"/>')
    for tick in (-60, -40, -20):
        out.append(f'<line x1="{sx(tick):.1f}" y1="{plot_top}" x2="{sx(tick):.1f}" '
                   f'y2="{plot_top + plot_height}" stroke="{GRID}" stroke-width="1"/>')
    for i, ((name, size, *_), value) in enumerate(zip(ROWS, values)):
        y = plot_top + i * ROW + (ROW - BAR) / 2
        baseline = y + BAR / 2 + 4.5
        out.append(f'<text x="{LABEL_RIGHT}" y="{baseline:.1f}" font-size="13" fill="{INK}" '
                   f'text-anchor="end">{name} <tspan fill="{MUTED}">{size}</tspan></text>')
        out.append(bar(y, value))
        if value < 0:
            out.append(f'<text x="{sx(value) - 6:.1f}" y="{baseline:.1f}" font-size="12" '
                       f'font-weight="700" fill="{INK}" text-anchor="end">{signed(value)}</text>')
        else:
            out.append(f'<text x="{sx(value) + 6:.1f}" y="{baseline:.1f}" font-size="12" '
                       f'font-weight="700" fill="{INK}">{signed(value)}</text>')
    out.append(f'<line x1="{ZERO:.1f}" y1="{plot_top - 4}" x2="{ZERO:.1f}" '
               f'y2="{plot_top + plot_height + 4}" stroke="{SECONDARY}" stroke-width="1.5"/>')
    axis_y = plot_top + plot_height + 20
    for tick in (-60, -40, -20, 0):
        label = f"{tick}%".replace("-", MINUS)
        out.append(f'<text x="{sx(tick):.1f}" y="{axis_y}" font-size="12" fill="{MUTED}" '
                   f'text-anchor="middle">{label}</text>')
    if noise:
        out.append(f'<text x="{ZERO:.1f}" y="{axis_y + 18}" font-size="12" fill="{MUTED}" '
                   f'text-anchor="middle">shaded: ±{NOISE:.0f}%, treated as unchanged</text>')
    return axis_y + (40 if noise else 22)


def main(path):
    allocated = [pct(row[4], row[5]) for row in ROWS]
    time = [pct(row[2], row[3]) for row in ROWS]

    body = []
    y = panel(body, 150, "Allocated memory", allocated, noise=False)
    y = panel(body, y + 34, "Mean execution time", time, noise=True)
    height = int(y + 24)

    description = (
        "Percentage change from 6.2.1 to 6.3.0 per fixture. Allocated memory: "
        + ", ".join(f"{row[0]} {value:+.0f}%" for row, value in zip(ROWS, allocated))
        + ". Mean time: "
        + ", ".join(f"{row[0]} {value:+.0f}%" for row, value in zip(ROWS, time))
        + "."
    )
    head = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{height}" '
        f'viewBox="0 0 {WIDTH} {height}" font-family="{FONT}" role="img" aria-labelledby="t d">',
        '<title id="t">ReverseMarkdown 6.2.1 vs 6.3.0 on real-world pages</title>',
        f'<desc id="d">{description}</desc>',
        f'<rect width="{WIDTH}" height="{height}" fill="#ffffff"/>',
        f'<text x="40" y="52" font-size="24" font-weight="800" fill="{INK}">'
        f'ReverseMarkdown 6.2.1 vs 6.3.0 on real-world pages</text>',
        f'<text x="40" y="76" font-size="13" fill="{MUTED}">BenchmarkDotNet, .NET 9.0, Apple M1, '
        f'default flavor, three launches each. Pages from the mdream corpus.</text>',
        f'<text x="40" y="96" font-size="13" fill="{MUTED}">Allocations are deterministic; '
        f'timings vary by a few percent between runs.</text>',
    ]
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(head + body + ["</svg>"]) + "\n")

    for row, a, t in zip(ROWS, allocated, time):
        print(f"{row[0]:<26} allocated {a:+6.1f}%   time {t:+6.1f}%")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "docs/public/benchmark-6.2.1-6.3.0.svg")
