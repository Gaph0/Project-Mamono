#!/bin/bash
# Syncs the mod into RimWorld's local Mods folder for in-game testing.
# Excludes dev-only files (git, wiki, backups, plans) so the in-game copy is clean.
set -e
cd "$(dirname "$0")"

DEST="$HOME/.steam/steam/steamapps/common/RimWorld/Mods/Project Momo"
mkdir -p "$DEST"

rsync -a --delete \
  --exclude='/.git' \
  --exclude='/Wiki' \
  --exclude='Assemblies/*.bak*' \
  --exclude='*.zip' \
  --exclude='/*_Plan.md' \
  --exclude='/release.sh' \
  --exclude='/sync.sh' \
  ./ "$DEST/"

echo "Synced to: $DEST"
echo "Enable 'Project Momo' (pmm.core) first in the mod list, before the faction mods."
