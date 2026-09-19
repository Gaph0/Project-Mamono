#!/bin/bash
# Builds the whole Project Momo solution (core + all sub-mods that have a csproj).
set -e
cd "$(dirname "$0")"

# Changelog gate. A malformed changelog stops the build here. Unlogged changes
# only warn, because the changelog is usually written after the code. Pass
# --strict (or CHANGELOG_STRICT=1) to make unlogged changes fatal as well.
./changelog-check.sh "$@"

dotnet build ProjectMomo.sln

# The Zlepper SDK overwrites About/About.xml with a generated minimal version
# on every core build. Restore the hand-written one so sync.sh deploys correctly.
git checkout -- About/About.xml

echo "Solution built. About/About.xml restored to the hand-written version."
