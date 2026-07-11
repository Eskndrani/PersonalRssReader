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

function debounce(fn, delay) {
  let timer;
  return function (...args) {
    clearTimeout(timer);
    timer = setTimeout(() => fn.apply(this, args), delay);
  };
}

function initTheme() {
  const saved = localStorage.getItem("theme");
  if (saved === "dark" || saved === "light") {
    document.documentElement.setAttribute("data-theme", saved);
  }
}

function toggleTheme() {
  const current = document.documentElement.getAttribute("data-theme");
  let isDark;
  if (current === "dark") {
    isDark = true;
  } else if (current === "light") {
    isDark = false;
  } else {
    isDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  }
  const next = isDark ? "light" : "dark";
  document.documentElement.setAttribute("data-theme", next);
  localStorage.setItem("theme", next);
}

const translations = {
  en: {
    subscriptions: "Subscriptions",
    pasteFeedUrl: "Paste RSS / Atom feed URL…",
    add: "Add",
    riverOfNews: "River of News",
    refresh: "Refresh",
    searchArticles: "Search articles…",
    noArticles: "No articles yet.",
    emptyHint: "Add subscriptions in the sidebar, then hit <strong>Refresh</strong>.",
    previous: "Previous",
    next: "Next",
    jumpTo: "Jump to",
    close: "Close",
    readMore: "Read More",
    pageOf: "Page {current} of {total}",
    feedQuiet: "Your feed is quiet. Add some subscriptions on the left!",
    noMatchSearch: "No articles match your search.",
    noMatchFilter: "No articles match your current filters.",
    noSubscriptions: "No subscriptions yet.",
    couldNotLoadSubs: "Could not load subscriptions.",
    feedRefreshed: "Feed refreshed.",
    couldNotRefresh: "Could not refresh feed.",
    feedRemoved: "Feed removed.",
    couldNotRemove: "Could not remove feed.",
    couldNotLoadArticles: "Could not load articles.",
    feedAdded: "Feed added.",
    adding: "Adding…",
    showHideFeed: "Show/hide articles from this feed",
    refreshThisFeed: "Refresh this feed",
    unsubscribe: "Unsubscribe"
  },
  ar: {
    subscriptions: "الاشتراكات",
    pasteFeedUrl: "ألصق رابط RSS / Atom…",
    add: "إضافة",
    riverOfNews: "نهر الأخبار",
    refresh: "تحديث",
    searchArticles: "ابحث في المقالات…",
    noArticles: "لا توجد مقالات بعد.",
    emptyHint: "أضف اشتراكات من الشريط الجانبي، ثم اضغط <strong>تحديث</strong>.",
    previous: "السابق",
    next: "التالي",
    jumpTo: "انتقال إلى",
    close: "إغلاق",
    readMore: "اقرأ المزيد",
    pageOf: "الصفحة {current} من {total}",
    feedQuiet: "لا توجد مقالات. أضف بعض الاشتراكات من اليسار!",
    noMatchSearch: "لا توجد مقالات تطابق بحثك.",
    noMatchFilter: "لا توجد مقالات تطابق عوامل التصفية الحالية.",
    noSubscriptions: "لا توجد اشتراكات بعد.",
    couldNotLoadSubs: "تعذر تحميل الاشتراكات.",
    feedRefreshed: "تم تحديث الخلاصة.",
    couldNotRefresh: "تعذر تحديث الخلاصة.",
    feedRemoved: "تمت إزالة الخلاصة.",
    couldNotRemove: "تعذرت إزالة الخلاصة.",
    couldNotLoadArticles: "تعذر تحميل المقالات.",
    feedAdded: "تمت إضافة الخلاصة.",
    adding: "…جار الإضافة",
    showHideFeed: "إظهار/إخفاء المقالات من هذه الخلاصة",
    refreshThisFeed: "تحديث هذه الخلاصة",
    unsubscribe: "إلغاء الاشتراك"
  }
};

let currentLocale = "en";

function t(key, vars) {
  var str = (translations[currentLocale] && translations[currentLocale][key]) || (translations.en[key] || key);
  if (vars) {
    Object.keys(vars).forEach(function (k) {
      str = str.replace("{" + k + "}", vars[k]);
    });
  }
  return str;
}

function applyTranslations() {
  document.querySelectorAll("[data-i18n]").forEach(function (el) {
    var key = el.getAttribute("data-i18n");
    if (!key) return;
    if (key === "emptyHint") {
      el.innerHTML = t(key);
    } else {
      el.textContent = t(key);
    }
  });
  document.querySelectorAll("[data-i18n-placeholder]").forEach(function (el) {
    var key = el.getAttribute("data-i18n-placeholder");
    if (key) el.placeholder = t(key);
  });
  document.querySelectorAll("[data-i18n-title]").forEach(function (el) {
    var key = el.getAttribute("data-i18n-title");
    if (key) el.title = t(key);
  });
}

function initLocale() {
  var saved = localStorage.getItem("locale");
  if (saved === "ar" || saved === "en") {
    currentLocale = saved;
  }
  applyLocale();
  applyTranslations();
}

function applyLocale() {
  document.documentElement.lang = currentLocale;
  document.documentElement.dir = currentLocale === "ar" ? "rtl" : "ltr";
  var toggle = document.getElementById("lang-toggle");
  if (toggle) {
    toggle.textContent = currentLocale === "ar" ? "English" : "العربية";
  }
}

function toggleLocale() {
  currentLocale = currentLocale === "ar" ? "en" : "ar";
  localStorage.setItem("locale", currentLocale);
  applyLocale();
  applyTranslations();
  renderPage(currentPage);
}

let allFeeds = [];
let allArticles = [];
let excludedFeedTitles = new Set();
let searchQuery = "";
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

function openModal(article) {
  if (!article) return;

  var modalTitle = document.getElementById("modal-title");
  modalTitle.textContent = article.title;
  modalTitle.dir = "auto";

  var modalBody = document.getElementById("modal-body");
  modalBody.dir = "auto";
  modalBody.innerHTML = article.summary;

  if (article.audioUrl) {
    const audioEl = document.createElement("audio");
    audioEl.controls = true;
    audioEl.src = article.audioUrl;
    audioEl.style.cssText = "width: 100%; margin-top: 1rem; margin-bottom: 1rem; border-radius: 8px;";
    modalBody.appendChild(audioEl);
  }

  document.getElementById("read-modal").style.display = "flex";
}

function closeModal() {
  document.getElementById("read-modal").style.display = "none";
  document.getElementById("modal-body").innerHTML = "";
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
      '<li class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">' + t("noSubscriptions") + '</li>';
    showToast(t("couldNotLoadSubs"), true);
  }
}

function renderFeedList(feeds) {
  allFeeds = feeds;
  const currentTitles = new Set(feeds.map((f) => f.title));
  for (const title of excludedFeedTitles) {
    if (!currentTitles.has(title)) excludedFeedTitles.delete(title);
  }

  const list = document.getElementById("feed-list");

  if (feeds.length === 0) {
    list.innerHTML =
      '<li class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">' + t("noSubscriptions") + '</li>';
    return;
  }

  list.innerHTML = feeds
    .map(
      (f) => `
    <li class="feed-item">
      <input type="checkbox" class="feed-toggle" data-id="${escapeAttr(f.id)}" ${excludedFeedTitles.has(f.title) ? "" : "checked"} title="${escapeAttr(t("showHideFeed"))}">
      <span class="feed-title" title="${escapeAttr(f.title)}">${escapeHtml(f.title)}</span>
      <button class="feed-refresh" data-url="${escapeAttr(f.url)}" title="${escapeAttr(t("refreshThisFeed"))}">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <polyline points="23 4 23 10 17 10"/>
          <polyline points="1 20 1 14 7 14"/>
          <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
        </svg>
      </button>
      <button class="feed-delete" data-id="${escapeAttr(f.id)}" title="${escapeAttr(t("unsubscribe"))}">&times;</button>
    </li>`
    )
    .join("");
}

function toggleFeedVisibility(id) {
  const feed = allFeeds.find((f) => f.id === id);
  if (!feed) return;

  if (excludedFeedTitles.has(feed.title)) {
    excludedFeedTitles.delete(feed.title);
  } else {
    excludedFeedTitles.add(feed.title);
  }
  renderPage(currentPage);
}

async function refreshSingleFeed(url, btn) {
  btn.disabled = true;
  btn.classList.add("btn-refreshing");
  try {
    const res = await fetch("/api/refresh-feed", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url }),
    });
    if (!res.ok) throw new Error("Failed to refresh feed");
    showToast(t("feedRefreshed"), false);
    await loadNews();
  } catch {
    showToast(t("couldNotRefresh"), true);
  } finally {
    btn.disabled = false;
    btn.classList.remove("btn-refreshing");
  }
}

async function deleteFeed(id) {
  try {
    const res = await fetch(`/api/feeds/${id}`, { method: "DELETE" });
    if (!res.ok) throw new Error("Failed to delete feed");
    await loadFeeds();
    showToast(t("feedRemoved"), false);
  } catch {
    showToast(t("couldNotRemove"), true);
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
    emptyState.textContent = t("feedQuiet");
    emptyState.classList.remove("hidden");
    showToast(t("couldNotLoadArticles"), true);
  }
}

function getFilteredArticles() {
  const q = searchQuery.trim().toLowerCase();
  return allArticles.filter((a) => {
    if (excludedFeedTitles.has(a.feedTitle)) return false;
    if (!q) return true;
    const titleText = (a.title ?? "").toLowerCase();
    const summaryText = stripTags(a.summary ?? "").toLowerCase();
    return titleText.includes(q) || summaryText.includes(q);
  });
}

function renderPage(page) {
  const container = document.getElementById("articles-container");
  const emptyState = document.getElementById("empty-state");
  const pagination = document.getElementById("pagination-controls");
  const prevBtn = document.getElementById("prev-page-btn");
  const nextBtn = document.getElementById("next-page-btn");
  const indicator = document.getElementById("page-indicator");

  container.textContent = "";

  const visibleArticles = getFilteredArticles();

  if (visibleArticles.length === 0) {
    if (allArticles.length === 0) {
      emptyState.textContent = t("feedQuiet");
    } else if (searchQuery.trim()) {
      emptyState.textContent = t("noMatchSearch");
    } else {
      emptyState.textContent = t("noMatchFilter");
    }
    emptyState.classList.remove("hidden");
    pagination.style.display = "none";
    return;
  }

  emptyState.classList.add("hidden");

  const totalPages = Math.ceil(visibleArticles.length / PAGE_SIZE);
  currentPage = Math.max(1, Math.min(page, totalPages));

  const start = (currentPage - 1) * PAGE_SIZE;
  const pageArticles = visibleArticles.slice(start, start + PAGE_SIZE);

  const fragment = document.createDocumentFragment();

  pageArticles.forEach((a) => {
    const card = document.createElement("article");
    card.className = "article-card";

    const source = document.createElement("div");
    source.className = "article-feed-source";
    const tag = document.createElement("span");
    tag.className = "feed-tag";
    tag.innerHTML = a.feedTitle;
    tag.style.backgroundColor = getColorForFeed(a.feedTitle);
    source.appendChild(tag);

    const title = document.createElement("h3");
    title.className = "article-title";
    title.dir = "auto";
    const link = document.createElement("a");
    link.href = a.link;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.innerHTML = a.title;
    title.appendChild(link);

    const meta = document.createElement("div");
    meta.className = "article-meta";
    meta.textContent = formatDate(a.publishDate);

    const summary = document.createElement("div");
    summary.className = "article-summary";
    summary.dir = "auto";
    summary.innerHTML = a.summary;

    const readMore = document.createElement("button");
    readMore.className = "btn btn-secondary";
    readMore.textContent = t("readMore");
    readMore.style.marginTop = "0.5rem";
    readMore.addEventListener("click", () => openModal(a));

    card.appendChild(source);
    card.appendChild(title);
    card.appendChild(meta);
    card.appendChild(summary);

    const audioLink = a.audioUrl || a.AudioUrl;
    if (audioLink) {
        const audioEl = document.createElement("audio");
        audioEl.controls = true;
        audioEl.src = audioLink;
        audioEl.style.cssText = "width: 100%; margin-top: 1rem; margin-bottom: 1rem; border-radius: 8px;";
        card.appendChild(audioEl);
    }

    card.appendChild(readMore);

    fragment.appendChild(card);
  });

  container.appendChild(fragment);

  indicator.textContent = t("pageOf", { current: currentPage, total: totalPages });
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
    btn.textContent = t("adding");

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
      showToast(t("feedAdded"), false);
    } catch (err) {
      showToast(err.message, true);
    } finally {
      btn.disabled = false;
      btn.textContent = t("add");
    }
  });
}

document.addEventListener("DOMContentLoaded", () => {
  initTheme();
  initLocale();

  document.getElementById("theme-toggle").addEventListener("click", toggleTheme);
  document.getElementById("lang-toggle").addEventListener("click", toggleLocale);

  loadFeeds();
  setupAddFeedForm();

  document.getElementById("refresh-btn").addEventListener("click", () => {
    loadNews();
  });

  const debouncedSearch = debounce(() => {
    searchQuery = document.getElementById("search-input").value;
    renderPage(1);
  }, 300);

  document.getElementById("search-input").addEventListener("input", debouncedSearch);

  document.getElementById("jump-page-input").addEventListener("keydown", (e) => {
    if (e.key !== "Enter") return;
    const totalPages = Math.ceil(getFilteredArticles().length / PAGE_SIZE);
    const page = parseInt(e.target.value, 10);
    if (isNaN(page)) return;
    const clamped = Math.max(1, Math.min(page, totalPages));
    e.target.value = "";
    renderPage(clamped);
  });

  document.getElementById("prev-page-btn").addEventListener("click", () => {
    renderPage(currentPage - 1);
  });

  document.getElementById("next-page-btn").addEventListener("click", () => {
    renderPage(currentPage + 1);
  });

  document.getElementById("feed-list").addEventListener("click", (e) => {
    const toggle = e.target.closest(".feed-toggle");
    if (toggle) {
      toggleFeedVisibility(toggle.dataset.id);
      return;
    }
    const refreshBtn = e.target.closest(".feed-refresh");
    if (refreshBtn) {
      refreshSingleFeed(refreshBtn.dataset.url, refreshBtn);
      return;
    }
    const deleteBtn = e.target.closest(".feed-delete");
    if (deleteBtn) {
      deleteFeed(deleteBtn.dataset.id);
      return;
    }
  });

  document.getElementById("read-modal").querySelector(".modal-close").addEventListener("click", closeModal);

  document.getElementById("read-modal").querySelector(".modal-backdrop").addEventListener("click", closeModal);
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