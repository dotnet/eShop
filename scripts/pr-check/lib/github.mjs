// GitHub access for the PR check: the `gh` CLI when it is installed, otherwise the REST API with
// GITHUB_TOKEN or GH_TOKEN.
import { writeFile } from 'node:fs/promises';
import { git } from '../../pipeline/lib/git.mjs';
import { run } from '../../pipeline/lib/util.mjs';

const token = () => process.env.GITHUB_TOKEN || process.env.GH_TOKEN || '';
const hasGh = () => run('gh', ['--version']).status === 0;

export function repoSlug(root) {
  if (process.env.GITHUB_REPOSITORY) return process.env.GITHUB_REPOSITORY;
  const url = git(root, ['remote', 'get-url', 'origin']).stdout.trim();
  const match = /github\.com[:/]([^/]+\/[^/]+?)(?:\.git)?$/.exec(url);
  if (!match) throw new Error(`cannot work out the GitHub repository from the origin remote "${url}"`);
  return match[1];
}

async function api(root, method, route, body) {
  if (!token()) throw new Error('GitHub access needs the gh CLI or GITHUB_TOKEN / GH_TOKEN');
  const response = await fetch(`https://api.github.com/repos/${repoSlug(root)}${route}`, {
    method,
    headers: { Authorization: `Bearer ${token()}`, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!response.ok) throw new Error(`GitHub ${method} ${route} failed: ${response.status} ${await response.text()}`);
  return response.json();
}

export async function fetchPullRequest(root, number) {
  if (hasGh()) {
    const result = run('gh', ['pr', 'view', String(number), '--json', 'number,title,body,url,headRefName,baseRefName,headRefOid,isCrossRepository'], { cwd: root });
    if (result.status !== 0) throw new Error(`gh pr view ${number} failed: ${result.stderr.trim()}`);
    const pr = JSON.parse(result.stdout);
    return { number: pr.number, title: pr.title, body: pr.body ?? '', url: pr.url, headRef: pr.headRefName, baseRef: pr.baseRefName, headSha: pr.headRefOid, fromFork: Boolean(pr.isCrossRepository) };
  }
  const pr = await api(root, 'GET', `/pulls/${number}`);
  return { number: pr.number, title: pr.title, body: pr.body ?? '', url: pr.html_url, headRef: pr.head.ref, baseRef: pr.base.ref, headSha: pr.head.sha, fromFork: pr.head.repo?.full_name !== pr.base.repo?.full_name };
}

export async function commentOnPullRequest(root, number, body) {
  if (hasGh()) {
    const result = run('gh', ['pr', 'comment', String(number), '--body-file', '-'], { cwd: root, input: body });
    if (result.status !== 0) throw new Error(`gh pr comment failed: ${result.stderr.trim()}`);
    return;
  }
  await api(root, 'POST', `/issues/${number}/comments`, { body });
}

// Video links in a PR body: direct files and GitHub's uploaded attachments.
export function videoLinks(text, extensions) {
  const ext = extensions.map((e) => e.slice(1)).join('|');
  const pattern = new RegExp(`https://[^\\s)"'<>\\]]+?\\.(?:${ext})(?=[\\s)"'<>\\]?#]|$)|https://github\\.com/user-attachments/assets/[\\w-]+`, 'gi');
  return [...new Set(text.match(pattern) ?? [])];
}

export async function download(url, file) {
  const host = new URL(url).hostname;
  const headers = token() && /(^|\.)github(usercontent)?\.com$/.test(host) ? { Authorization: `Bearer ${token()}` } : {};
  const response = await fetch(url, { headers, redirect: 'follow' });
  if (!response.ok) throw new Error(`download failed: ${response.status}`);
  if ((response.headers.get('content-type') ?? '').startsWith('text/html')) {
    throw new Error('the link returned a web page, not a video (a private attachment without access?)');
  }
  await writeFile(file, Buffer.from(await response.arrayBuffer()));
}
