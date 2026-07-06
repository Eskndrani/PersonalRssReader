function showToast(message, isError) {
  const container = document.getElementById("toast-container");
  const toast = document.createElement("div");
  toast.className = `toast ${isError ? "toast--error" : "toast--success"}`;
  toast.textContent = message;
  container.appendChild(toast);

  setTimeout(() => {
    toast.classList.add("toast--out");
    toast.addEventListener("animationend", () => toast.remove());
  }, 3000);
}

function formatDate(dateStr) {
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

function stripTags(html) {
  const tmp = document.createElement("div");
  tmp.innerHTML = html;
  return tmp.textContent || tmp.innerText || "";
}

let allArticles = [];
let currentPage = 1;
const PAGE_SIZE = 10;

function getColorForFeed(feedTitle) {
  let hash = 0;
  for (let i = 0; i < feedTitle.length; i++) {
    hash = feedTitle.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = ((hash % 360) + 360) % 360;
  return `hsl(${hue}, 55%, 45%)`;
}

async function loadFeeds() {
  const list = document.getElementById("feed-list");

  try {
    const res = await fetch("/api/feeds");
    if (!res.ok) throw new Error("Failed to fetch feeds");
    const feeds = await res.json();
    renderFeedList(feeds);
    await loadNews();
  } catch {
    list.innerHTML =
      '<li class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">No subscriptions yet.</li>';
    showToast("Could not load subscriptions.", true);
  }
}

function renderFeedList(feeds) {
  const list = document.getElementById("feed-list");

  if (feeds.length === 0) {
    list.innerHTML =
      '<li class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">No subscriptions yet.</li>';
    return;
  }

  list.innerHTML = feeds
    .map(
      (f) => `
    <li class="feed-item">
      <span class="feed-title" title="${escapeAttr(f.title)}">${escapeHtml(f.title)}</span>
      <button class="feed-delete" data-id="${escapeAttr(f.id)}" title="Unsubscribe">&times;</button>
    </li>`
    )
    .join("");

  list.querySelectorAll(".feed-delete").forEach((btn) => {
    btn.addEventListener("click", () => deleteFeed(btn.dataset.id));
  });
}

async function deleteFeed(id) {
  try {
    const res = await fetch(`/api/feeds/${id}`, { method: "DELETE" });
    if (!res.ok) throw new Error("Failed to delete feed");
    await loadFeeds();
    showToast("Feed removed.", false);
  } catch {
    showToast("Could not remove feed.", true);
  }
}

async function loadNews() {
  const container = document.getElementById("articles-container");
  const emptyState = document.getElementById("empty-state");

  emptyState.classList.add("hidden");

  const skeletonHtml = Array.from({ length: 3 }, () => `
    <article class="article-card skeleton-article">
      <div class="article-feed-source"><span class="skeleton-block skeleton-tag"></span></div>
      <h3 class="article-title"><span class="skeleton-block skeleton-text"></span></h3>
      <div class="article-meta"><span class="skeleton-block skeleton-text skeleton-text--short"></span></div>
      <p class="article-summary"><span class="skeleton-block skeleton-text"></span><span class="skeleton-block skeleton-text skeleton-text--medium"></span></p>
    </article>
  `).join("");

  container.innerHTML = skeletonHtml;

  try {
    const res = await fetch("/api/news");
    if (!res.ok) throw new Error("Failed to fetch news");
    allArticles = await res.json();
    renderPage(1);
  } catch {
    container.textContent = "";
    emptyState.textContent = "Your feed is quiet. Add some subscriptions on the left!";
    emptyState.classList.remove("hidden");
    showToast("Could not load articles.", true);
  }
}

function renderPage(page) {
  const container = document.getElementById("articles-container");
  const emptyState = document.getElementById("empty-state");
  const pagination = document.getElementById("pagination-controls");
  const prevBtn = document.getElementById("prev-page-btn");
  const nextBtn = document.getElementById("next-page-btn");
  const indicator = document.getElementById("page-indicator");

  container.textContent = "";

  if (allArticles.length === 0) {
    emptyState.textContent = "Your feed is quiet. Add some subscriptions on the left!";
    emptyState.classList.remove("hidden");
    pagination.style.display = "none";
    return;
  }

  emptyState.classList.add("hidden");

  const totalPages = Math.ceil(allArticles.length / PAGE_SIZE);
  currentPage = Math.max(1, Math.min(page, totalPages));

  const start = (currentPage - 1) * PAGE_SIZE;
  const pageArticles = allArticles.slice(start, start + PAGE_SIZE);

  pageArticles.forEach((a) => {
    const card = document.createElement("article");
    card.className = "article-card";

    const source = document.createElement("div");
    source.className = "article-feed-source";
    const tag = document.createElement("span");
    tag.className = "feed-tag";
    tag.textContent = a.feedTitle;
    tag.style.backgroundColor = getColorForFeed(a.feedTitle);
    source.appendChild(tag);

    const title = document.createElement("h3");
    title.className = "article-title";
    const link = document.createElement("a");
    link.href = a.link;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.textContent = a.title;
    title.appendChild(link);

    const meta = document.createElement("div");
    meta.className = "article-meta";
    meta.textContent = formatDate(a.publishDate);

    const summary = document.createElement("p");
    summary.className = "article-summary";
    summary.textContent = stripTags(a.summary);

    card.appendChild(source);
    card.appendChild(title);
    card.appendChild(meta);
    card.appendChild(summary);

    container.appendChild(card);
  });

  indicator.textContent = `Page ${currentPage} of ${totalPages}`;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  pagination.style.display = "flex";

  document.getElementById("articles-container").scrollIntoView({ behavior: "smooth", block: "start" });
}

function setupAddFeedForm() {
  const form = document.getElementById("add-feed-form");
  const input = document.getElementById("feed-url");

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const url = input.value.trim();
    if (!url) return;

    const btn = form.querySelector("button");
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
      showToast("Feed added.", false);
    } catch (err) {
      showToast(err.message, true);
    } finally {
      btn.disabled = false;
      btn.textContent = "Add";
    }
  });
}

document.addEventListener("DOMContentLoaded", () => {
  loadFeeds();
  setupAddFeedForm();

  document.getElementById("refresh-btn").addEventListener("click", () => {
    loadNews();
  });

  document.getElementById("prev-page-btn").addEventListener("click", () => {
    renderPage(currentPage - 1);
  });

  document.getElementById("next-page-btn").addEventListener("click", () => {
    renderPage(currentPage + 1);
  });
});

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function escapeAttr(str) {
  return (str ?? "")
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}