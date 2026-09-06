#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Build content.js for the documentation site.

index.html is a single-page viewer: it renders Markdown from the bundle this script writes.
Run it after editing any .md file:

    python build.py            (from the docs folder)

Sources:
  README.md, getting-started.md, api/*.md, guides/*.md   English pages
  <lang>/...                                                       translations (same paths)
  _sidebar.md / <lang>/_sidebar.md                                 navigation (groups + links)
  ui.json                                                          per-language UI strings

A page missing in a language falls back to English in the viewer, so translations can be partial.
"""
import io, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
VERSION = "0.6.1"
NAME = "OpenTaiko Documentation"
HOME = "README"
LANGS = [
    ("en", "English"), ("de", "Deutsch"), ("es", "Español"), ("fr", "Français"),
    ("ja", "日本語"), ("ko", "한국어"), ("nl", "Nederlands"), ("ru", "Русский"), ("zh", "中文"),
]
LINK = re.compile(r"^\s*-\s*\[([^\]]+)\]\(([^)]+)\)(?:\s*<span class=\"badge-exp\">([^<]+)</span>)?")
GROUP = re.compile(r"^-\s+(?!\[)(.+?)\s*$")
TAG = re.compile(r"<span[^>]*>.*?</span>|<[^>]+>")   # badges are dropped from titles entirely


def read(path):
    with io.open(path, encoding="utf-8-sig") as f:
        return f.read()


def route_of(link):
    link = link.split("#", 1)[0]
    if link.endswith(".md"):
        link = link[:-3]
    return link.strip("/")


def parse_sidebar(path):
    groups, current = [], None
    for line in read(path).splitlines():
        m = GROUP.match(line)
        if m:
            current = {"group": m.group(1), "items": []}
            groups.append(current)
            continue
        m = LINK.match(line)
        if m and current is not None:
            item = {"label": m.group(1), "route": route_of(m.group(2))}
            if m.group(3):
                item["badge"] = m.group(3).strip()   # rendered as a yellow tag right of the label
            current["items"].append(item)
    return groups


def title_of(md):
    for line in md.splitlines():
        if line.startswith("# "):
            return TAG.sub("", line[2:]).strip()
    return None


def slug(text):
    """Same rule as index.html: lower case, runs of anything but letters/digits/_ become '-'."""
    return re.sub(r"^-+|-+$", "", re.sub(r"[^\w]+", "-", text.lower()))


def anchors_of(md):
    """The page's section anchors for the sidebar: its ### headings (the globals and handles),
    or its ## headings when it has no ###. Ids follow slug(); duplicates get -2, -3, ..."""
    fence, h2, h3 = False, [], []
    for line in md.splitlines():
        if line.startswith("```"):
            fence = not fence
            continue
        if fence:
            continue
        if line.startswith("### "):
            h3.append(TAG.sub("", line[4:]).strip())
        elif line.startswith("## "):
            h2.append(TAG.sub("", line[3:]).strip())
    picked = h3 if h3 else h2
    out, seen = [], {}
    for label in picked:
        sid = slug(label)
        if not sid:
            continue
        if sid in seen:
            seen[sid] += 1
            sid = "%s-%d" % (sid, seen[sid])
        else:
            seen[sid] = 1
        out.append({"id": sid, "label": label})
    return out


def collect(lang_dir):
    """route -> markdown for every page linked from that folder's sidebar."""
    sidebar = os.path.join(lang_dir, "_sidebar.md")
    if not os.path.exists(sidebar):
        return None, {}
    nav = parse_sidebar(sidebar)
    docs = {}
    for g in nav:
        for it in g["items"]:
            p = os.path.join(lang_dir, it["route"] + ".md")
            if os.path.exists(p):
                docs[it["route"]] = read(p)
                it["anchors"] = anchors_of(docs[it["route"]])
    return nav, docs


def main():
    ui = json.loads(read(os.path.join(HERE, "ui.json")))
    bundle = {"version": VERSION, "name": NAME,
              "langs": [{"code": c, "label": l} for c, l in LANGS],
              "home": HOME, "docs": {}, "nav": {}, "titles": {}, "ui": ui}
    for code, _ in LANGS:
        folder = HERE if code == "en" else os.path.join(HERE, code)
        nav, docs = collect(folder)
        if nav is None:
            print("skip %s: no _sidebar.md" % code)
            continue
        bundle["docs"][code] = docs
        bundle["nav"][code] = nav
        bundle["titles"][code] = {r: (title_of(md) or NAME) for r, md in docs.items()}
        missing = [it["route"] for g in nav for it in g["items"] if it["route"] not in docs]
        print("%s: %d pages%s" % (code, len(docs), (", missing: " + ", ".join(missing)) if missing else ""))
    out = "window.__DOCS__ = " + json.dumps(bundle, ensure_ascii=False) + ";\n"
    with io.open(os.path.join(HERE, "content.js"), "w", encoding="utf-8", newline="\n") as f:
        f.write(out)
    print("content.js written (%d KB)" % (len(out.encode("utf-8")) // 1024))


if __name__ == "__main__":
    sys.exit(main())
