#!/usr/bin/env bash
set -euo pipefail

TARGET_DIR="angular-client"

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "Error: run this from inside the parent git repository." >&2
  exit 1
fi

if [ ! -d "$TARGET_DIR" ]; then
  echo "Error: '$TARGET_DIR' directory not found." >&2
  exit 1
fi

mode="$(git ls-files --stage -- "$TARGET_DIR" | awk 'NR==1 {print $1}')"

if [ "$mode" = "160000" ]; then
  echo "Detected gitlink/submodule pointer for '$TARGET_DIR'. Converting to regular tracked files..."
  git rm --cached "$TARGET_DIR"
else
  echo "'$TARGET_DIR' is not currently tracked as a gitlink (mode=$mode)."
fi

if [ -f .gitmodules ]; then
  if git config -f .gitmodules --get-regexp '^submodule\..*\.path$' | awk '{print $2}' | grep -Fxq "$TARGET_DIR"; then
    module_name="$(git config -f .gitmodules --get-regexp '^submodule\..*\.path$' | awk -v d="$TARGET_DIR" '$2==d {print $1}' | sed -E 's/^submodule\.([^.]*)\.path$/\1/')"
    git config -f .gitmodules --remove-section "submodule.$module_name" || true
    if [ ! -s .gitmodules ]; then
      rm -f .gitmodules
      git rm --cached .gitmodules 2>/dev/null || true
    else
      git add .gitmodules
    fi
    git config --remove-section "submodule.$module_name" 2>/dev/null || true
  fi
fi

rm -rf ".git/modules/$TARGET_DIR"

if [ -f "$TARGET_DIR/.git" ]; then
  rm -f "$TARGET_DIR/.git"
fi

if [ -z "$(find "$TARGET_DIR" -mindepth 1 -maxdepth 1 -print -quit)" ]; then
  echo "Warning: '$TARGET_DIR' is empty right now, so there are no files to add yet."
else
  git add "$TARGET_DIR"
fi

echo
echo "Done. Next steps:"
echo "  git status"
echo "  git commit -m \"Track angular-client as regular files\""
echo "  git push"
