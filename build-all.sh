#!/bin/bash
# Builds the whole Project Momo solution (core + all sub-mods that have a csproj).
set -e
cd "$(dirname "$0")"

dotnet build ProjectMomo.sln

# The Zlepper SDK overwrites About/About.xml with a generated minimal version
# on every core build. Restore the hand-written one so sync.sh deploys correctly.
git checkout -- About/About.xml

echo "Solution built. About/About.xml restored to the hand-written version."
