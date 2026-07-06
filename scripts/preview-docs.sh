#!/usr/bin/env bash
#
# Build the combined ReverseMarkdown docs site locally, mirroring the GitHub Pages
# deploy: v6 (from master) at the root and v5 (from the 5.x branch) under /v5/.
#
# The two sites live on different branches, so this checks out each branch into a
# throwaway worktree, builds it, and assembles the output under ./_preview.
#
# Usage:
#   scripts/preview-docs.sh           Build into ./_preview and print the serve command
#   scripts/preview-docs.sh --serve   Build, then serve at http://localhost:$PORT
#
# Env:
#   PORT   Port for --serve (default 8000)
#
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
PORT="${PORT:-8000}"
TMP="$(mktemp -d)"
WT_V6="$TMP/v6"
WT_V5="$TMP/v5"

remove_worktrees() {
  git -C "$ROOT" worktree remove --force "$WT_V6" 2>/dev/null || true
  git -C "$ROOT" worktree remove --force "$WT_V5" 2>/dev/null || true
}
cleanup() { remove_worktrees; rm -rf "$TMP"; }
trap cleanup EXIT

echo "==> Checking out master (v6) and 5.x (v5)"
# --detach avoids the "branch already checked out" error when one of these is the
# branch currently checked out in your main worktree.
git -C "$ROOT" worktree add --detach "$WT_V6" master >/dev/null
git -C "$ROOT" worktree add --detach "$WT_V5" 5.x >/dev/null

echo "==> Building v6 (master)"
( cd "$WT_V6" && npm ci && npm run docs:build )

echo "==> Building v5 (5.x)"
( cd "$WT_V5" && npm ci && npm run docs:build )

OUT="$ROOT/_preview/reversemarkdown-net"
echo "==> Assembling combined site into _preview/"
rm -rf "$ROOT/_preview"
mkdir -p "$OUT/v5"
cp -r "$WT_V6/docs/.vitepress/dist/." "$OUT/"
cp -r "$WT_V5/docs/.vitepress/dist/." "$OUT/v5/"

# Worktrees are no longer needed once their dist output is copied. Remove them now
# so they are gone even when --serve replaces this shell via exec (bypassing the trap).
remove_worktrees

echo
echo "Combined site ready under _preview/:"
echo "  v6 (latest): http://localhost:$PORT/reversemarkdown-net/"
echo "  v5:          http://localhost:$PORT/reversemarkdown-net/v5/"

if [[ "${1:-}" == "--serve" ]]; then
  echo
  echo "==> Serving _preview on port $PORT (Ctrl+C to stop)"
  cd "$ROOT/_preview" && exec python3 -m http.server "$PORT"
else
  echo
  echo "To serve:  (cd _preview && python3 -m http.server $PORT)"
fi
