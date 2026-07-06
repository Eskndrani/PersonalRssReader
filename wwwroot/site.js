let allArticles = [];

document.addEventListener("DOMContentLoaded", () => {
  loadFeeds();
  loadArticles();

  document.getElementById("add-feed-form").addEventListener("submit", addFeed);
  document.getElementById("refresh-btn").addEventListener("click", refreshArticles);
});

function toast(message, type = "info") {
  const container = document.getElementById("toast-container");
  const el = document.createElement("div");
  el.className = `toast toast--${type}`;
  el.textContent = message;
  container.appendChild(el);

  setTimeout(() => {
    el.classList.add("toast--out");
    el.addEventListener("animationend", () => el.remove());
  }, 3000);
}

function timeSince(dateStr) {
  const now = new Date();
  const then = new Date(dateStr);
  const diffMs = now - then;
  const diffMin = Math.floor(diffMs / 60000);
  const diffHr = Math.floor(diffMs / 3600000);
  const diffDay = Math.floor(diffMs / 86400000);

  if (diffMin < 1) return "Just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  if (diffHr < 24) return `${diffHr}h ago`;
  if (diffDay < 7) return `${diffDay}d ago`;
  return then.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

function stripHtml(html) {
  const tmp = document.createElement("div");
  tmp.innerHTML = html;
  return tmp.textContent || tmp.innerText || "";
}

function formatSummary(text) {
  const plain = stripHtml(text).trim();
  if (plain.length > 300) return plain.substring(0, 300) + "…";
  return plain;
}

/* ── Feed List ────────────────────────────────────────────── */

async function loadFeeds() {
  try {
    const res = await fetch("/api/feeds");
    if (!res.ok) throw new Error("Failed to load feeds");
    const feeds = await res.json();
    renderFeedList(feeds);
  } catch (err) {
    renderFeedList([]);
    toast("Could not load subscriptions.", "error");
  }
}

async function addFeed(e) {
  e.preventDefault();
  const input = document.getElementById("feed-url");
  const btn = input.nextElementSibling;
  const url = input.value.trim();

  if (!url) return;

  btn.disabled = true;
  btn.textContent = "Adding…";

  try {
    const res = await fetch("/api/feeds", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url }),
    });

    if (!res.ok) {
      const text = await res.text();
      throw new Error(text || "Invalid feed URL");
    }

    input.value = "";
    await loadFeeds();
    toast("Feed added.", "success");
  } catch (err) {
    toast(err.message || "Could not add feed.", "error");
  } finally {
    btn.disabled = false;
    btn.textContent = "Add";
  }
}

async function deleteFeed(id) {
  try {
    const res = await fetch(`/api/feeds/${id}`, { method: "DELETE" });
    if (!res.ok) throw new Error("Failed to delete feed");
    await loadFeeds();
    toast("Feed removed.", "success");
  } catch {
    toast("Could not remove feed.", "error");
  }
}

function renderFeedList(feeds) {
  const list = document.getElementById("feed-list");

  if (feeds.length === 0) {
    list.innerHTML = `<li class="feed-item" style="color:#9ca3af; font-size:0.8125rem; padding:1rem 1.25rem;">No subscriptions yet.</li>`;
    return;
  }

  list.innerHTML = feeds.map(f => `
    <li class="feed-item">
      <span class="feed-title" title="${escapeAttr(f.title)}">${escapeHtml(f.title)}</span>
      <button class="feed-delete" title="Unsubscribe" data-id="${escapeAttr(f.id)}">&times;</button>
    </li>
  `).join("");

  list.querySelectorAll(".feed-delete").forEach(btn => {
    btn.addEventListener("click", () => deleteFeed(btn.dataset.id));
  });
}

/* ── Articles ─────────────────────────────────────────────── */

async function loadArticles() {
  showSkeletons(true);
  try {
    const res = await fetch("/api/news");
    if (!res.ok) throw new Error("Failed to load articles");
    allArticles = await res.json();
    renderArticles(allArticles);
  } catch {
    allArticles = [];
    renderArticles([]);
    toast("Could not load articles.", "error");
  }
}

async function refreshArticles() {
  const btn = document.getElementById("refresh-btn");
  btn.classList.add("btn-refreshing");
  btn.disabled = true;

  await loadArticles();

  btn.classList.remove("btn-refreshing");
  btn.disabled = false;
  toast("Articles refreshed.", "success");
}

function renderArticles(articles) {
  showSkeletons(false);

  const container = document.getElementById("articles-container");
  const emptyState = document.getElementById("empty-state");

  if (articles.length === 0) {
    container.textContent = "";
    emptyState.classList.remove("hidden");
    return;
  }

  emptyState.classList.add("hidden");

  container.innerHTML = articles.map(a => `
    <article class="article-card">
      <div class="article-feed-source">
        <span class="feed-source-tag">${escapeHtml(a.feedTitle)}</span>
      </div>
      <h3 class="article-title">
        <a href="${escapeAttr(a.link)}" target="_blank" rel="noopener noreferrer">${escapeHtml(a.title)}</a>
      </h3>
      <div class="article-meta">${escapeHtml(timeSince(a.publishDate))}</div>
      <p class="article-summary">${escapeHtml(formatSummary(a.summary))}</p>
    </article>
  `).join("");
}

function showSkeletons(visible) {
  const container = document.getElementById("articles-container");
  const emptyState = document.getElementById("empty-state");

  if (visible) {
    emptyState.classList.add("hidden");
    container.innerHTML = Array.from({ length: 3 }, () => `
      <article class="article-card skeleton-article">
        <div class="article-feed-source"><span class="skeleton-block skeleton-tag"></span></div>
        <h3 class="article-title"><span class="skeleton-block skeleton-text"></span></h3>
        <div class="article-meta"><span class="skeleton-block skeleton-text skeleton-text--short"></span></div>
        <p class="article-summary"><span class="skeleton-block skeleton-text"></span><span class="skeleton-block skeleton-text skeleton-text--medium"></span></p>
      </article>
    `).join("");
    return;
  }

  container.querySelectorAll(".skeleton-article").forEach(el => el.remove());
}

/* ── XSS Helpers ──────────────────────────────────────────── */

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function escapeAttr(str) {
  return (str ?? "").replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/'/g, "&#39;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
