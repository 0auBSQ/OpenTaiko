#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Structural parity check between the English pages and every translation.

    python check.py            (from the docs folder)

For each language folder it verifies, page by page against the English source: the leading
<!-- path --> comment, the number of code fences, the set of Markdown link targets, and that
the sidebar links the same routes. Exit code 1 when anything differs.
"""
import io, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
LANGS = ["de", "es", "fr", "ja", "ko", "nl", "ru", "zh"]
LINK = re.compile(r"\]\(([^)\s]+)\)")


def read(p):
    with io.open(p, encoding="utf-8-sig") as f:
        return f.read()


def pages():
    out = ["README.md", "getting-started.md"]
    for sub in ("api", "guides"):
        out += sorted(os.path.join(sub, f) for f in os.listdir(os.path.join(HERE, sub)) if f.endswith(".md"))
    return out


def links(md):
    # code blocks can legitimately differ (comments), so links are compared outside fences only
    out, fence = [], False
    for line in md.splitlines():
        if line.startswith("```"):
            fence = not fence
            continue
        if not fence:
            out += [l.split("#")[0] for l in LINK.findall(line) if not l.startswith(("http", "mailto"))]
    return sorted(set(out))


def main():
    problems = 0
    en_pages = pages()
    for lang in LANGS:
        folder = os.path.join(HERE, lang)
        if not os.path.isdir(folder):
            print("%s: missing folder" % lang); problems += 1; continue
        for rel in en_pages + ["_sidebar.md"]:
            src, dst = os.path.join(HERE, rel), os.path.join(folder, rel)
            if not os.path.exists(dst):
                print("%s/%s: missing" % (lang, rel)); problems += 1; continue
            a, b = read(src), read(dst)
            if rel != "_sidebar.md":
                fa, fb = a.splitlines()[0] if a else "", b.splitlines()[0] if b else ""
                if fa != fb:
                    print("%s/%s: first line differs (%r vs %r)" % (lang, rel, fa, fb)); problems += 1
            na, nb = a.count("\n```"), b.count("\n```")
            if a.startswith("```"): na += 1
            if b.startswith("```"): nb += 1
            if na != nb:
                print("%s/%s: code fences %d vs %d" % (lang, rel, nb, na)); problems += 1
            la, lb = links(a), links(b)
            if la != lb:
                print("%s/%s: link targets differ: missing %s, extra %s" % (lang, rel, sorted(set(la) - set(lb)), sorted(set(lb) - set(la)))); problems += 1
    print("OK: every translation matches the English structure" if problems == 0 else "%d problem(s)" % problems)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
