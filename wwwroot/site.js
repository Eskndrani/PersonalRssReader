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
    updateThemeIcon(saved);
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
  updateThemeIcon(next);
}

function updateThemeIcon(theme) {
  var sun = document.querySelector(".theme-toggle .icon-sun");
  var moon = document.querySelector(".theme-toggle .icon-moon");
  if (!sun || !moon) return;
  if (theme === "light") {
    sun.style.display = "none";
    moon.style.display = "inline";
  } else {
    sun.style.display = "inline";
    moon.style.display = "none";
  }
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
    unsubscribe: "Unsubscribe",
    processingBatch: "Processing Batch…",
    feedsAdded: "{added} feed(s) added.",
    feedsFailed: "{failed} feed(s) failed.",
    batchAdded: "{added} added, {failed} failed.",
    retentionLimit: "Keep articles for:",
    opt1Week: "1 Week",
    opt2Weeks: "2 Weeks",
    opt1Month: "1 Month",
    opt2Months: "2 Months",
    optForever: "Forever",
    toggleFavorite: "Toggle Favorite",
    copyFeedLink: "Copy Feed Link",
    linkCopied: "Link copied to clipboard!",
    allFeeds: "All",
    favorites: "Favorites",
    showAll: "Show All",
    hideAll: "Hide All",
    bookmarkArticle: "Bookmark article",
    articleBookmarked: "Article bookmarked",
    bookmarkRemoved: "Bookmark removed",
    aiSummary: "AI Summary",
    generatingSummary: "Generating...",
    allArticles: "All News",
    bookmarkedArticles: "Bookmarks",
    loginTitle: "Log In",
    registerTitle: "Create Account",
    noAccount: "No account?",
    signUpHere: "Sign up here",
    haveAccount: "Already have an account?",
    logInHere: "Log in here",
    logout: "Logout",
    dailyBriefing: "Daily Briefing",
    dismiss: "Dismiss",
    chatTitle: "Chat with your feeds",
    chatPlaceholder: "Ask about your news...",
    deepSummary: "Deep Summary",
    deepSummaryTitle: "AI Deep Summary",
    placeholderUrl: "Paste one or more URLs (one per line)...",
    placeholderUsername: "username (optional)",
    placeholderPassword: "password (optional)",
    placeholderSearch: "Search subscriptions..."
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
    feedRefreshed: "تم تحديث الاشتراك.",
    couldNotRefresh: "تعذر تحديث الاشتراك.",
    feedRemoved: "تمت إزالة الاشتراك.",
    couldNotRemove: "تعذرت إزالة الاشتراك.",
    couldNotLoadArticles: "تعذر تحميل المقالات.",
    feedAdded: "تمت إضافة الاشتراك.",
    adding: "…جار الإضافة",
    showHideFeed: "إظهار/إخفاء المقالات من هذا الاشتراك",
    refreshThisFeed: "تحديث هذا الاشتراك",
    unsubscribe: "إلغاء الاشتراك",
    processingBatch: "…جار معالجة الدفعة",
    feedsAdded: "تمت إضافة {added} خلاصة.",
    feedsFailed: "{failed} خلاصات فشلت.",
    batchAdded: "تمت إضافة {added}، {failed} فشلت.",
    retentionLimit: "الاحتفاظ بالمقالات لمدة:",
    opt1Week: "أسبوع واحد",
    opt2Weeks: "أسبوعين",
    opt1Month: "شهر واحد",
    opt2Months: "شهران",
    optForever: "للأبد",
    toggleFavorite: "تبديل المفضلة",
    copyFeedLink: "نسخ رابط الاشتراك",
    linkCopied: "تم نسخ الرابط!",
    allFeeds: "الكل",
    favorites: "المفضلة",
    showAll: "إظهار الكل",
    hideAll: "إخفاء الكل",
    bookmarkArticle: "حفظ المقال",
    articleBookmarked: "تم حفظ المقال",
    bookmarkRemoved: "تم إلغاء الحفظ",
    aiSummary: "ملخص AI",
    generatingSummary: "...جار التوليد",
    allArticles: "كل الأخبار",
    bookmarkedArticles: "المحفوظات",
    loginTitle: "تسجيل الدخول",
    registerTitle: "إنشاء حساب",
    noAccount: "ليس لديك حساب؟",
    signUpHere: "سجل هنا",
    haveAccount: "لديك حساب بالفعل؟",
    logInHere: "سجل الدخول هنا",
    logout: "تسجيل الخروج",
    dailyBriefing: "الموجز اليومي",
    dismiss: "إغلاق",
    chatTitle: "تحدث مع مصادر الأخبار",
    chatPlaceholder: "اسأل عن أخبارك...",
    deepSummary: "ملخص ذكي",
    deepSummaryTitle: "AI Deep Summary",
    placeholderUrl: "أدخل رابطاً أو أكثر (رابط في كل سطر)...",
    placeholderUsername: "اسم المستخدم (اختياري)",
    placeholderPassword: "كلمة المرور (اختياري)",
    placeholderSearch: "البحث في الاشتراكات..."
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
  document.getElementById("feed-url").placeholder = t("placeholderUrl");
  document.getElementById("feed-username").placeholder = t("placeholderUsername");
  document.getElementById("feed-password").placeholder = t("placeholderPassword");
  document.getElementById("feed-search-sidebar").placeholder = t("placeholderSearch");
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
let currentFeedTab = "all";
let currentArticleTab = "all";
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

function buildAudioPlayer(audioSrc) {
  var audioContainer = document.createElement("div");
  audioContainer.className = "audio-player-wrapper";

  var audioEl = document.createElement("audio");
  audioEl.controls = true;
  audioEl.src = audioSrc;
  audioEl.style.cssText = "width: 100%; border-radius: 8px;";

  var storageKey = "audio_pos_" + audioSrc;
  var savePosition = debounce(function (currentTime) {
    localStorage.setItem(storageKey, currentTime);
  }, 2000);

  audioEl.addEventListener("loadedmetadata", function () {
    var saved = localStorage.getItem(storageKey);
    if (saved) {
      audioEl.currentTime = parseFloat(saved) || 0;
    }
  });

  audioEl.addEventListener("timeupdate", function () {
    savePosition(audioEl.currentTime);
  });

  var controls = document.createElement("div");
  controls.className = "audio-custom-controls";

  var btnSkipBack = document.createElement("button");
  btnSkipBack.className = "btn btn-secondary";
  btnSkipBack.setAttribute("aria-label", "Skip backward 15 seconds");
  btnSkipBack.textContent = "-15s";
  btnSkipBack.addEventListener("click", function () {
    audioEl.currentTime = Math.max(0, audioEl.currentTime - 15);
  });

  var speeds = [1, 1.25, 1.5, 2];
  var speedIndex = 0;
  var btnSpeed = document.createElement("button");
  btnSpeed.className = "btn btn-secondary";
  btnSpeed.setAttribute("aria-label", "Change playback speed");
  btnSpeed.textContent = "1x Speed";
  btnSpeed.addEventListener("click", function () {
    speedIndex = (speedIndex + 1) % speeds.length;
    audioEl.playbackRate = speeds[speedIndex];
    btnSpeed.textContent = speeds[speedIndex] + "x Speed";
  });

  var btnSkipForward = document.createElement("button");
  btnSkipForward.className = "btn btn-secondary";
  btnSkipForward.setAttribute("aria-label", "Skip forward 15 seconds");
  btnSkipForward.textContent = "+15s";
  btnSkipForward.addEventListener("click", function () {
    if (audioEl.duration && isFinite(audioEl.duration)) {
      audioEl.currentTime = Math.min(audioEl.duration, audioEl.currentTime + 15);
    }
  });

  controls.appendChild(btnSkipBack);
  controls.appendChild(btnSpeed);
  controls.appendChild(btnSkipForward);

  audioContainer.appendChild(audioEl);
  audioContainer.appendChild(controls);

  return audioContainer;
}

function openModal(article) {
  if (!article) return;

  var modalTitle = document.getElementById("modal-title");
  modalTitle.textContent = article.title;
  modalTitle.dir = "auto";

  var modalBody = document.getElementById("modal-body");
  modalBody.dir = "auto";
  modalBody.innerHTML = article.summary;

  if (article.audioUrl && !article.audioUrl.toLowerCase().includes(".webm") && !article.audioUrl.toLowerCase().includes(".mp4")) {
    modalBody.appendChild(buildAudioPlayer(article.audioUrl));
  }

  document.getElementById("read-modal").style.display = "flex";
}

function closeModal() {
  document.getElementById("read-modal").style.display = "none";
  document.getElementById("modal-body").innerHTML = "";
}

async function loadFeeds() {
  var list = document.getElementById("feed-list");
  list.replaceChildren();

  try {
    await loadNews();
    var res = await fetch("/api/feeds?t=" + Date.now(), {
      credentials: "include",
      cache: "no-store"
    });
    if (!res.ok) throw new Error("Failed to fetch feeds");
    var feeds = await res.json();
    renderFeedList(feeds);
  } catch {
    list.innerHTML =
      '<li class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">' + t("noSubscriptions") + '</li>';
    showToast(t("couldNotLoadSubs"), true);
  }
}

function getFaviconHtml(feed) {
  var color = getColorForFeed(feed.title || feed.name || "");
  var url = feed.faviconUrl || (feed.url ? "https://www.google.com/s2/favicons?domain=" + new URL(feed.url).hostname + "&sz=64" : "");
  return '<span class="icon-wrapper" style="width:16px;height:16px;border-radius:50%;display:inline-block;overflow:hidden;background-color:' + color + ';vertical-align:middle;margin-right:6px;flex-shrink:0;">' +
    '<img src="' + escapeAttr(url) + '" style="width:100%;height:100%;object-fit:cover;" onerror="this.style.display=\'none\'" alt="">' +
    '</span>';
}

function renderFeedList(feeds) {
  allFeeds = feeds;
  const currentTitles = new Set(feeds.map((f) => f.title));
  for (const title of excludedFeedTitles) {
    if (!currentTitles.has(title)) excludedFeedTitles.delete(title);
  }

  const list = document.getElementById("feed-list");

  var feedsToRender = allFeeds;
  if (currentFeedTab === "fav") {
    feedsToRender = allFeeds.filter(function (f) { return f.isFavorite; });
  }

  if (feedsToRender.length === 0) {
    list.innerHTML =
      '<li class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">' + t("noSubscriptions") + '</li>';
    return;
  }

  list.innerHTML = feedsToRender
    .slice()
    .sort(function (a, b) {
      if (a.isFavorite !== b.isFavorite) return a.isFavorite ? -1 : 1;
      return (a.title || "").localeCompare(b.title || "");
    })
    .map(
      (f) => `
    <li class="feed-item feed-item-card">
      <div class="feed-item-info">
        <input type="checkbox" class="feed-toggle" data-id="${escapeAttr(f.id)}" ${excludedFeedTitles.has(f.title) ? "" : "checked"} title="${escapeAttr(t("showHideFeed"))}">
        ${getFaviconHtml(f)}
        <label class="feed-label">
          <span class="feed-name" title="${escapeAttr(f.title)}">${escapeHtml(f.title)}</span>
          <span class="feed-count">(${f.articleCount || 0})</span>
        </label>
      </div>
      <div class="feed-item-options">
        <button class="feed-favorite" data-id="${escapeAttr(f.id)}" title="${escapeAttr(t("toggleFavorite"))}">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="${f.isFavorite ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
          </svg>
        </button>
        <button class="feed-share" data-url="${escapeAttr(f.url)}" title="${escapeAttr(t("copyFeedLink"))}">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
            <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
          </svg>
        </button>
        <button class="feed-refresh" data-url="${escapeAttr(f.url)}" title="${escapeAttr(t("refreshThisFeed"))}">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="23 4 23 10 17 10"/>
            <polyline points="1 20 1 14 7 14"/>
            <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
          </svg>
        </button>
        <button class="feed-delete" data-id="${escapeAttr(f.id)}" title="${escapeAttr(t("unsubscribe"))}">&times;</button>
      </div>
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
    const res = await fetch("/api/refresh-feed?retentionDays=" + (localStorage.getItem("retentionDays") || "30"), {
      credentials: "include",
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url }),
    });
    if (!res.ok) throw new Error("Failed to refresh feed");
    showToast(t("feedRefreshed"), false);
    await loadFeeds();
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

async function toggleFavoriteFeed(id) {
  try {
    const res = await fetch(`/api/feeds/${id}/favorite`, { method: "PATCH", credentials: "include" });
    if (!res.ok) throw new Error("Failed to toggle favorite");
    const feed = allFeeds.find(function (f) { return f.id === id; });
    if (feed) {
      feed.isFavorite = !feed.isFavorite;
      renderFeedList(allFeeds);
      renderPage(currentPage);
    }
  } catch {
    showToast(t("couldNotRemove"), true);
  }
}

function copyFeedLink(url) {
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(url).then(function () {
      showToast(t("linkCopied"), false);
    });
    return;
  }
  var el = document.createElement("textarea");
  el.value = url;
  el.style.position = "fixed";
  el.style.left = "-9999px";
  document.body.appendChild(el);
  el.select();
  document.execCommand("copy");
  document.body.removeChild(el);
  showToast(t("linkCopied"), false);
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
    const url = "/api/news?retentionDays=" + (localStorage.getItem("retentionDays") || "30");
    console.log("[DEBUG] Fetching:", url);
    const res = await fetch(url, { credentials: "include" });
    console.log("[DEBUG] Response status:", res.status);
    if (!res.ok) {
      const errText = await res.text();
      console.error("[DEBUG] Response body:", errText);
      throw new Error("Failed to fetch news: HTTP " + res.status);
    }
    allArticles = await res.json();
    console.log("[DEBUG] Articles count:", allArticles.length);
    if (allArticles.length > 0) console.log("[DEBUG] First article:", JSON.stringify(allArticles[0]));
    renderPage(1);
  } catch (err) {
    console.error("[DEBUG] Fetch error:", err);
    container.textContent = "";
    emptyState.textContent = t("feedQuiet");
    emptyState.classList.remove("hidden");
    showToast(t("couldNotLoadArticles"), true);
  }
}

function getFilteredArticles() {
  const q = searchQuery.trim().toLowerCase();
  return allArticles.filter((a) => {
    if (currentArticleTab === "bookmarks" && !a.isBookmarked) return false;
    if (excludedFeedTitles.has(a.feedTitle)) return false;
    var favTitles = new Set(allFeeds.filter(function (f) { return f.isFavorite; }).map(function (f) { return f.title; }));
    if (currentFeedTab === "fav" && !favTitles.has(a.feedTitle)) return false;
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
    if (a.isBookmarked) card.classList.add("article-bookmarked");
    if (a.isRead) card.classList.add("article-read");

    const source = document.createElement("div");
    source.className = "article-feed-source";

    var feed = allFeeds.find(function (f) { return f.title === a.feedTitle; });
    if (feed) {
      source.innerHTML += getFaviconHtml(feed);
    }

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

    const contentWrapper = document.createElement("div");
    contentWrapper.className = "article-content-wrapper";
    contentWrapper.appendChild(source);
    contentWrapper.appendChild(title);
    contentWrapper.appendChild(meta);

    const summary = document.createElement("div");
    summary.className = "article-summary";
    summary.dir = "auto";
    summary.innerHTML = a.summary;
    contentWrapper.appendChild(summary);

    const audioLink = a.audioUrl || a.AudioUrl;
    if (audioLink && !audioLink.toLowerCase().includes(".webm") && !audioLink.toLowerCase().includes(".mp4")) {
        contentWrapper.appendChild(buildAudioPlayer(audioLink));
    }

    var articleActions = document.createElement("div");
    articleActions.className = "article-actions";

    var bookmarkBtn = document.createElement("button");
    bookmarkBtn.className = "btn btn-icon";
    bookmarkBtn.setAttribute("aria-label", t("bookmarkArticle"));
    bookmarkBtn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="' + (a.isBookmarked ? 'currentColor' : 'none') + '" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg>';
    bookmarkBtn.addEventListener("click", function () {
      fetch("/api/news/" + a.id + "/bookmark", { method: "PATCH", credentials: "include" })
        .then(function (res) { return res.json(); })
        .then(function (data) {
          a.isBookmarked = data.isBookmarked;
          bookmarkBtn.querySelector("svg").setAttribute("fill", data.isBookmarked ? "currentColor" : "none");
          if (data.isBookmarked) {
            card.classList.add("article-bookmarked");
            showToast(t("articleBookmarked"), false);
          } else {
            card.classList.remove("article-bookmarked");
            showToast(t("bookmarkRemoved"), false);
          }
        });
    });

    var readMore = document.createElement("button");
    readMore.className = "btn btn-secondary";
    readMore.textContent = t("readMore");
    readMore.addEventListener("click", function () {
      openModal(a);
      fetch("/api/news/" + a.id + "/read", { method: "PATCH" });
      if (!a.isRead) {
        a.isRead = true;
        card.classList.add("article-read");
      }
    });

    articleActions.appendChild(bookmarkBtn);

    var deepBtn = document.createElement("button");
    deepBtn.className = "btn btn-secondary";
    deepBtn.innerHTML = t("deepSummary");
    deepBtn.addEventListener("click", function () {
      deepBtn.disabled = true;
      deepBtn.classList.add("btn-refreshing");
      var modal = document.getElementById("deep-summary-modal");
      var body = document.getElementById("deep-summary-body");
      modal.style.display = "flex";
      body.innerHTML = '<p style="color:var(--color-faint)">' + t("generatingSummary") + '</p>';

      fetch("/api/ai/summary/article/" + a.id + "?lang=" + currentLocale, { credentials: "include" })
        .then(function (res) { return res.json(); })
        .then(function (data) {
          body.innerHTML = marked.parse(data.summary);
        })
        .catch(function () {
          body.innerHTML = '<p style="color:var(--color-error)">Failed to generate summary.</p>';
        })
        .finally(function () {
          deepBtn.disabled = false;
          deepBtn.classList.remove("btn-refreshing");
        });
    });
    articleActions.appendChild(deepBtn);

    articleActions.appendChild(readMore);
    contentWrapper.appendChild(articleActions);

    card.appendChild(contentWrapper);

    const imgUrl = a.imageUrl || a.ImageUrl || a.imageurl;
    var summaryHasImage = /<img[^>]+>/i.test(a.summary);
    if (imgUrl && !summaryHasImage) {
      const img = document.createElement("img");
      img.className = "article-thumbnail";
      img.src = imgUrl;
      img.onerror = function () { console.error("Image failed to load:", imgUrl); img.style.display = "none"; };
      card.insertBefore(img, contentWrapper);
    }

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
  const textarea = document.getElementById("feed-url");
  const usernameInput = document.getElementById("feed-username");
  const passwordInput = document.getElementById("feed-password");

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const raw = textarea.value.trim();
    if (!raw) return;

    const urls = raw.split(/\r?\n/)
      .map(function (line) { return line.trim(); })
      .filter(function (line) { return line.length > 0; });

    if (urls.length === 0) return;

    const username = usernameInput.value.trim() || null;
    const password = passwordInput.value.trim() || null;

    const btn = form.querySelector("button[type='submit']");
    btn.disabled = true;
    btn.textContent = t("processingBatch");

    try {
      const res = await fetch("/api/feeds/batch", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ urls: urls, username: username, password: password }),
  });

  if (!res.ok) {
        var errMsg = "Batch import failed";
        try {
          var errData = await res.json();
          errMsg = errData.message || errData.errors || errMsg;
        } catch (e) {}
        throw new Error(errMsg);
      }

      const result = await res.json();

      textarea.value = "";
      usernameInput.value = "";
      passwordInput.value = "";
      await loadFeeds();

      if (result.added > 0 && result.failed === 0) {
        showToast(t("feedsAdded", { added: result.added }), false);
      } else if (result.failed > 0) {
        showToast(t("batchAdded", { added: result.added, failed: result.failed }), true);
      }
    } catch (err) {
      showToast(err.message, true);
    } finally {
      btn.disabled = false;
      btn.textContent = t("add");
    }
  });
}

async function checkAuth() {
  try {
    var res = await fetch("/api/auth/me", { credentials: "include" });
    if (res.ok) {
      var data = await res.json();
      document.getElementById("user-email").textContent = data.email || "";
      document.getElementById("btn-logout").style.display = "";
      setupAuthorized();
      return;
    }
    if (res.status === 401 || res.status === 403) {
      window.location.href = "/welcome.html";
      return;
    }
  } catch (e) {
    console.error("Auth check failed:", e);
  }
  window.location.href = "/welcome.html";
}

function setupAuthorized() {
  document.getElementById("hamburger-btn").addEventListener("click", function () {
    document.querySelector(".sidebar").classList.toggle("open");
  });
  loadFeeds();
  setupAddFeedForm();
  setupRetention();
  setupSearch();
  setupTabs();
  setupButtons();
  setupListeners();
  setupBriefing();
  setupChat();
}

function setupBriefing() {
  document.getElementById("btn-daily-briefing").addEventListener("click", async function () {
    var btn = document.getElementById("btn-daily-briefing");
    var card = document.getElementById("briefing-card");
    var content = document.getElementById("briefing-content");

    btn.disabled = true;
    btn.classList.add("btn-refreshing");
    card.style.display = "block";
    content.textContent = "Generating your briefing...";

    try {
      var res = await fetch("/api/news/daily-briefing?lang=" + currentLocale, { credentials: "include" });
      if (!res.ok) throw new Error("Failed to generate briefing");
      var data = await res.json();
      content.innerHTML = marked.parse(data.summary);
    } catch (err) {
      content.textContent = "Could not generate briefing. Please try again.";
    } finally {
      btn.disabled = false;
      btn.classList.remove("btn-refreshing");
    }
  });

  document.getElementById("btn-dismiss-briefing").addEventListener("click", function () {
    document.getElementById("briefing-card").style.display = "none";
  });
}

function setupChat() {
  document.getElementById("chat-widget").style.display = "";

  var panel = document.querySelector(".chat-panel");
  var messages = document.getElementById("chat-messages");

  document.getElementById("btn-chat-toggle").addEventListener("click", function () {
    panel.style.display = panel.style.display === "none" ? "" : "none";
  });

  document.querySelector(".chat-close-btn").addEventListener("click", function () {
    panel.style.display = "none";
  });

  async function sendMessage() {
    var input = document.getElementById("chat-input");
    var msg = input.value.trim();
    if (!msg) return;

    var userDiv = document.createElement("div");
    userDiv.className = "chat-msg chat-msg-user";
    userDiv.textContent = msg;
    messages.appendChild(userDiv);

    var typingDiv = document.createElement("div");
    typingDiv.className = "chat-msg-typing";
    typingDiv.textContent = "Thinking...";
    messages.appendChild(typingDiv);
    messages.scrollTop = messages.scrollHeight;

    input.value = "";

    try {
      var res = await fetch("/api/chat?lang=" + currentLocale, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: msg }),
        credentials: "include"
      });
      if (!res.ok) throw new Error("Chat failed");
      var data = await res.json();

      typingDiv.remove();

      var aiDiv = document.createElement("div");
      aiDiv.className = "chat-msg chat-msg-ai";
      aiDiv.textContent = data.response;
      messages.appendChild(aiDiv);
    } catch (err) {
      typingDiv.textContent = "Sorry, something went wrong.";
    }

    messages.scrollTop = messages.scrollHeight;
  }

  document.getElementById("btn-chat-send").addEventListener("click", sendMessage);
  document.getElementById("chat-input").addEventListener("keydown", function (e) {
    if (e.key === "Enter") sendMessage();
  });
}

function setupRetention() {
  var saved = localStorage.getItem("retentionDays");
  document.getElementById("retention-select").value = saved || "30";
  if (!saved) localStorage.setItem("retentionDays", "30");
  document.getElementById("retention-select").addEventListener("change", async function () {
    localStorage.setItem("retentionDays", this.value);
    await loadFeeds();
  });
}

function setupSearch() {
  document.getElementById("feed-search-sidebar").addEventListener("input", filterSidebarFeeds);
  document.getElementById("search-input").addEventListener("input", debounce(function () {
    searchQuery = document.getElementById("search-input").value;
    renderPage(1);
  }, 300));
}

function setupTabs() {
  document.querySelectorAll(".tab-btn").forEach(function (btn) {
    btn.addEventListener("click", function (e) {
      document.querySelectorAll(".tab-btn").forEach(function (b) { b.classList.remove("active"); });
      e.target.classList.add("active");
      currentFeedTab = e.target.dataset.tab;
      renderFeedList(allFeeds);
      renderPage(1);
    });
  });
  document.querySelectorAll("[data-article-tab]").forEach(function (btn) {
    btn.addEventListener("click", function (e) {
      document.querySelectorAll("[data-article-tab]").forEach(function (b) { b.classList.remove("active"); });
      e.target.classList.add("active");
      currentArticleTab = e.target.dataset.articleTab;
      renderPage(1);
    });
  });
}

function setupButtons() {
  document.getElementById("btn-show-all").addEventListener("click", function () {
    excludedFeedTitles.clear();
    renderFeedList(allFeeds);
    renderPage(1);
  });
  document.getElementById("btn-hide-all").addEventListener("click", function () {
    var visibleFeeds = allFeeds;
    if (currentFeedTab === "fav") {
      visibleFeeds = allFeeds.filter(function (f) { return f.isFavorite; });
    }
    visibleFeeds.forEach(function (f) { excludedFeedTitles.add(f.title); });
    renderFeedList(allFeeds);
    renderPage(1);
  });
}

function setupListeners() {
  document.getElementById("refresh-btn").addEventListener("click", function () { loadFeeds(); });
  document.getElementById("jump-page-input").addEventListener("keydown", function (e) {
    if (e.key !== "Enter") return;
    var totalPages = Math.ceil(getFilteredArticles().length / PAGE_SIZE);
    var page = parseInt(e.target.value, 10);
    if (isNaN(page)) return;
    var clamped = Math.max(1, Math.min(page, totalPages));
    e.target.value = "";
    renderPage(clamped);
  });
  document.getElementById("prev-page-btn").addEventListener("click", function () { renderPage(currentPage - 1); });
  document.getElementById("next-page-btn").addEventListener("click", function () { renderPage(currentPage + 1); });
  document.getElementById("feed-list").addEventListener("click", function (e) {
    var toggle = e.target.closest(".feed-toggle");
    if (toggle) { toggleFeedVisibility(toggle.dataset.id); return; }
    var refreshBtn = e.target.closest(".feed-refresh");
    if (refreshBtn) { refreshSingleFeed(refreshBtn.dataset.url, refreshBtn); return; }
    var deleteBtn = e.target.closest(".feed-delete");
    if (deleteBtn) { deleteFeed(deleteBtn.dataset.id); return; }
    var favoriteBtn = e.target.closest(".feed-favorite");
    if (favoriteBtn) { toggleFavoriteFeed(favoriteBtn.dataset.id); return; }
    var shareBtn = e.target.closest(".feed-share");
    if (shareBtn) { copyFeedLink(shareBtn.dataset.url); return; }
  });
  document.getElementById("read-modal").querySelector(".modal-close").addEventListener("click", closeModal);
  document.getElementById("read-modal").querySelector(".modal-backdrop").addEventListener("click", closeModal);
}

document.addEventListener("DOMContentLoaded", () => {
  initTheme();
  initLocale();

  checkAuth();

  document.getElementById("theme-toggle").addEventListener("click", toggleTheme);
  document.getElementById("lang-toggle").addEventListener("click", toggleLocale);

  document.getElementById("btn-logout").addEventListener("click", async function () {
    await fetch("/api/auth/logout", { method: "POST", credentials: "include" });
    window.location.href = "/welcome.html";
  });

  document.getElementById("read-modal").querySelector(".modal-close").addEventListener("click", closeModal);
  document.getElementById("read-modal").querySelector(".modal-backdrop").addEventListener("click", closeModal);
});

function filterSidebarFeeds() {
  var query = document.getElementById("feed-search-sidebar").value.trim().toLowerCase();
  var items = document.querySelectorAll("#feed-list .feed-item");

  items.forEach(function (item) {
    var titleSpan = item.querySelector(".feed-name");
    if (!titleSpan) return;

    var title = (titleSpan.textContent || "").toLowerCase();

    if (!query || title.indexOf(query) !== -1) {
      item.style.display = "";
    } else {
      item.style.display = "none";
    }
  });
}

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

document.addEventListener("DOMContentLoaded", function () {
  var closeBtns = document.querySelectorAll("#deep-summary-modal .modal-close, #deep-summary-modal .modal-backdrop");
  closeBtns.forEach(function (el) {
    el.addEventListener("click", function () {
      document.getElementById("deep-summary-modal").style.display = "none";
    });
  });
});