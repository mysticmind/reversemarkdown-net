#!/usr/bin/env bash
# Downloads the benchmark corpus used by src/ReverseMarkdown.Benchmark/CorpusBenchmark.cs.
#
# The fixtures are the ones the mdream project publishes: real-world pages (Wikipedia articles,
# framework docs, MDN) that exercise the converter far more realistically than synthetic input.
#
# The corpus is third-party content and is NOT committed - it lives in a gitignored directory.
# Run this once before `dotnet run -c Release --project src/ReverseMarkdown.Benchmark`.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dest="$repo_root/src/ReverseMarkdown.Benchmark/Files/corpus"
base="https://raw.githubusercontent.com/harlan-zw/mdream/main"

# fixture-name : source path within the mdream repo
fixtures=(
  "nuxt-example.html:crates/core/tests/fixtures/nuxt-example.html"
  "vuejs-docs.html:crates/core/tests/fixtures/vuejs-docs.html"
  "wikipedia-small.html:crates/core/tests/fixtures/wikipedia-small.html"
  "mdn-array.html:crates/core/tests/fixtures/mdn-array.html"
  "react-learn.html:crates/core/tests/fixtures/react-learn.html"
  "github-markdown-complete.html:crates/core/tests/fixtures/github-markdown-complete.html"
  "wikipedia-largest.html:crates/node/test/fixtures/wikipedia-largest.html"
)

mkdir -p "$dest"
echo "Fetching benchmark corpus into $dest"

for entry in "${fixtures[@]}"; do
  name="${entry%%:*}"
  path="${entry#*:}"
  target="$dest/$name"

  if [[ -s "$target" ]]; then
    printf '  %-32s (cached)\n' "$name"
    continue
  fi

  if ! curl -fsSL -o "$target" "$base/$path"; then
    echo "  FAILED to download $name from $base/$path" >&2
    rm -f "$target"
    exit 1
  fi

  printf '  %-32s %6s KB\n' "$name" "$(( $(wc -c < "$target") / 1024 ))"
done

echo
echo "Done. Run the corpus benchmark with:"
echo "  dotnet run -c Release --project src/ReverseMarkdown.Benchmark -- --filter '*Corpus*'"
echo "Or the quick throughput summary (MB/s per group):"
echo "  dotnet run -c Release --project src/ReverseMarkdown.Benchmark -- throughput"
