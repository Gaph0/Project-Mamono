#!/bin/bash
# Checks all five Project Momo changelogs against CHANGELOG-FORMAT.md.
#
# Two checks per mod:
#   STRUCTURE  always fatal. The file starts with "# Changelog", it has the two
#              sections in order, and every entry is a flat dated line that
#              starts with an allowed verb.
#   COVERAGE   a warning by default. The mod has changed files under a content
#              path (Defs, Source, Patches, About, Languages, Textures) but
#              CHANGELOG.md is not one of them. --strict makes this fatal too.
#
# This script only reads. It never writes to a repo.
#
# Usage:
#   ./changelog-check.sh            structure fatal, coverage warns
#   ./changelog-check.sh --strict   coverage fatal too
#   CHANGELOG_STRICT=1 ./changelog-check.sh
set -uo pipefail

STRICT="${CHANGELOG_STRICT:-0}"
for arg in "$@"; do
	case "$arg" in
		--strict) STRICT=1 ;;
		-h|--help) sed -n '2,16p' "$0"; exit 0 ;;
		*) echo "changelog-check: unknown argument '$arg'" >&2; exit 2 ;;
	esac
done

# The five mod folders sit beside this script's own folder. Override with
# PMM_ROOT if that layout ever changes.
ROOT="${PMM_ROOT:-$(cd "$(dirname "$0")/.." && pwd)}"
MODS=(
	"Project Momo"
	"Project Momo Reptiles"
	"Project Momo Slime Faction"
	"Project Momo Elementals"
	"Project Momo Insects"
)
CONTENT_PATHS=(Defs Source Patches About Languages Textures)
LONG_LINE_WORDS=40

fail=0
warn=0

echo "changelog-check: root $ROOT"

for mod in "${MODS[@]}"; do
	dir="$ROOT/$mod"
	file="$dir/CHANGELOG.md"
	echo
	echo "$mod"

	if [ ! -f "$file" ]; then
		echo "  FAIL  no CHANGELOG.md"
		fail=1
		continue
	fi

	# ---------- structure (fatal) ----------
	structure="$(awk '
		NR == 1 { if ($0 != "# Changelog") print "    line 1: first line must be \"# Changelog\" (found: " substr($0, 1, 50) ")"; next }
		/^## Player-facing$/ { if (seenP || seenI) print "    line " NR ": repeated or misplaced \"## Player-facing\""; seenP = 1; section = "player"; next }
		/^## Internal$/ { if (!seenP) print "    line " NR ": \"## Internal\" appears before \"## Player-facing\""; else if (seenI) print "    line " NR ": repeated \"## Internal\""; seenI = 1; section = "internal"; next }
		/^#/ { print "    line " NR ": stray heading: " substr($0, 1, 50); next }
		/^[[:space:]]*$/ { next }
		{
			if (section == "") { print "    line " NR ": entry sits before any section"; next }
			if ($0 !~ /^- [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]: (Added|Changed|Fixed|Removed|Rebalanced) /) {
				print "    line " NR ": want \"- YYYY-MM-DD: <verb> ...\" but found: " substr($0, 1, 60)
			}
		}
		END { if (!seenP) print "    missing \"## Player-facing\""; if (!seenI) print "    missing \"## Internal\"" }
	' "$file")"

	if [ -n "$structure" ]; then
		echo "  FAIL  structure"
		echo "$structure"
		fail=1
	else
		echo "  ok    structure"
	fi

	# ---------- long lines (advisory, never fatal) ----------
	long="$(awk -v max="$LONG_LINE_WORDS" '
		/^- / { n = NF - 2; if (n > max) { count++; if (n > worst) worst = n } }
		END { if (count > 0) print "    " count " line(s) over " max " words (longest " worst ") - the cap is soft, but check them" }
	' "$file")"
	if [ -n "$long" ]; then
		echo "  note  length"
		echo "$long"
	fi

	# ---------- coverage (warn, or fatal with --strict) ----------
	changed="$(git -C "$dir" status --porcelain -- "${CONTENT_PATHS[@]}" 2>/dev/null)"
	changelog="$(git -C "$dir" status --porcelain -- CHANGELOG.md 2>/dev/null)"

	if [ -n "$changed" ] && [ -z "$changelog" ]; then
		count="$(printf '%s\n' "$changed" | grep -c .)"
		echo "  WARN  coverage: $count changed content file(s), CHANGELOG.md not touched"
		printf '%s\n' "$changed" | sed 's/^/          /'
		warn=1
	elif [ -n "$changed" ]; then
		echo "  ok    coverage (CHANGELOG.md is part of the change)"
	else
		echo "  ok    coverage (no content changes pending)"
	fi
done

echo
if [ "$fail" -ne 0 ]; then
	echo "changelog-check: FAILED (structure). Fix the lines above. See Project Momo/CHANGELOG-FORMAT.md."
	exit 1
fi
if [ "$STRICT" -eq 1 ] && [ "$warn" -ne 0 ]; then
	echo "changelog-check: FAILED (--strict: a mod has changes with no new changelog line)."
	exit 1
fi
if [ "$warn" -ne 0 ]; then
	echo "changelog-check: pass with warnings (use --strict to make coverage fatal)."
	exit 0
fi
echo "changelog-check: pass"
exit 0
