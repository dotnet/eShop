import fs from 'node:fs';
import path from 'node:path';
import { run, sha } from './util.mjs';

export const git = (root, args, opts = {}) => run('git', args, { cwd: root, ...opts });

export function fsReader(root) {
  return {
    rev: null,
    read(rel) {
      try {
        return fs.readFileSync(path.join(root, rel), 'utf8');
      } catch {
        return null;
      }
    },
    list(relDir) {
      try {
        return fs.readdirSync(path.join(root, relDir)).sort();
      } catch {
        return [];
      }
    },
    walk(relDir) {
      const out = [];
      const visit = (rel) => {
        let entries;
        try {
          entries = fs.readdirSync(path.join(root, rel), { withFileTypes: true });
        } catch {
          return;
        }
        for (const entry of entries) {
          if (SKIP_DIRS.has(entry.name)) continue;
          const child = `${rel}/${entry.name}`;
          if (entry.isDirectory()) visit(child);
          else if (entry.isFile()) out.push(child);
        }
      };
      visit(relDir);
      return out.sort();
    },
  };
}

const SKIP_DIRS = new Set(['node_modules', 'bin', 'obj', '.git', 'TestResults']);

export function gitReader(root, rev) {
  return {
    rev,
    read(rel) {
      const result = git(root, ['show', `${rev}:${rel}`]);
      return result.status === 0 ? result.stdout : null;
    },
    list(relDir) {
      const result = git(root, ['ls-tree', '--name-only', `${rev}:${relDir}`]);
      return result.status === 0 ? result.stdout.split('\n').filter(Boolean).sort() : [];
    },
    walk(relDir) {
      const result = git(root, ['ls-tree', '-r', '--name-only', rev, '--', relDir]);
      return result.status === 0
        ? result.stdout
            .split('\n')
            .filter((f) => f && !f.split('/').some((part) => SKIP_DIRS.has(part)))
            .sort()
        : [];
    },
  };
}

export function revExists(root, rev) {
  return git(root, ['rev-parse', '--verify', '--quiet', `${rev}^{commit}`]).status === 0;
}

export function mergeBase(root, baseRef) {
  for (const ref of [baseRef, 'main', 'origin/main']) {
    if (!ref || !revExists(root, ref)) continue;
    const result = git(root, ['merge-base', 'HEAD', ref]);
    if (result.status === 0) return result.stdout.trim();
  }
  return revExists(root, 'HEAD') ? 'HEAD' : null;
}

export function currentBranch(root) {
  const result = git(root, ['rev-parse', '--abbrev-ref', 'HEAD']);
  return result.status === 0 ? result.stdout.trim() : '';
}

export function changedFiles(root, from, to) {
  if (!from || !revExists(root, from)) return [];
  const args = to ? ['diff', '--name-only', from, to] : ['diff', '--name-only', from];
  const result = git(root, args);
  return result.status === 0 ? result.stdout.split('\n').filter(Boolean) : [];
}

const excludeSpecs = (excludes) => ['--', '.', ...excludes.map((p) => `:(exclude)${p}`)];

function untracked(root, excludes) {
  const result = git(root, ['ls-files', '--others', '--exclude-standard', ...excludeSpecs(excludes)]);
  return result.status === 0 ? result.stdout.split('\n').filter(Boolean).sort() : [];
}

// Fingerprint of the implementation changes (tracked diff against the merge base plus untracked
// files), ignoring tests, plans and pipeline bookkeeping so QE agent output never invalidates a review.
export function implChanges(config) {
  const base = mergeBase(config.root, config.baseRef);
  if (!base) return { base: null, hash: '', files: [] };
  const diff = git(config.root, ['diff', '--no-ext-diff', base, ...excludeSpecs(config.implExclude)]).stdout;
  const names = git(config.root, ['diff', '--name-only', base, ...excludeSpecs(config.implExclude)])
    .stdout.split('\n')
    .filter(Boolean);
  const newFiles = untracked(config.root, config.implExclude);
  const newHashes = newFiles.map((f) => `${f}:${git(config.root, ['hash-object', f]).stdout.trim()}`);
  const files = [...new Set([...names, ...newFiles])].sort();
  if (files.length === 0) return { base, hash: '', files };
  return { base, hash: sha(`${diff}\n${newHashes.join('\n')}`), files };
}

export function implDiff(config, maxChars = 200_000) {
  const { base, files } = implChanges(config);
  if (!base) return '';
  const stat = git(config.root, ['diff', '--stat', base, ...excludeSpecs(config.implExclude)]).stdout;
  let diff = git(config.root, ['diff', '--no-ext-diff', base, ...excludeSpecs(config.implExclude)]).stdout;
  for (const file of untracked(config.root, config.implExclude)) {
    const content = fs.readFileSync(path.join(config.root, file), 'utf8');
    diff += `\n--- /dev/null\n+++ b/${file} (new, untracked)\n${content.split('\n').map((l) => `+${l}`).join('\n')}\n`;
  }
  if (diff.length > maxChars) {
    diff = `${diff.slice(0, maxChars)}\n… diff truncated at ${maxChars} chars; read the remaining files directly: ${files.join(', ')}`;
  }
  return `Base commit: ${base}\n\n${stat}\n${diff}`;
}
