# Flavors

v5 selects the output style with boolean flags on `Config`. Each flag is independent:

| Flavor | Flag | What it produces |
| --- | --- | --- |
| [GitHub](/flavors/github) | `GithubFlavored = true` | GitHub-flavored markdown for `br`, `pre`, task lists, and tables. |
| [Slack](/flavors/slack) | `SlackFlavored = true` | Slack mrkdwn: `*bold*`, `_italic_`, `~strike~`, and `•` bullets. |
| [Telegram](/flavors/telegram) | `TelegramMarkdownV2 = true` | Telegram MarkdownV2 with its escaping rules and fallbacks. |
| [CommonMark](/flavors/commonmark) | `CommonMark = true` | CommonMark-focused output. |

By default (no flags set) ReverseMarkdown produces general-purpose markdown. Note that tables are
always emitted as GitHub-flavored markdown regardless of the `GithubFlavored` flag.

::: warning Combine with care
The flavor flags are independent booleans, not a single selector. Combining `CommonMark` with
`GithubFlavored` can produce mixed output - keep them separate unless you explicitly want that
behavior.
:::

::: tip v6 uses a single Flavor enum
v6 replaces these booleans with one canonical `Flavor` enum (and adds MultiMarkdown and Pandoc).
See the [v6 flavors documentation](https://mysticmind.github.io/reversemarkdown-net/flavors/).
:::
