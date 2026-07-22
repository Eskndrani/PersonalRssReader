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

function timeSince(dateString) {
  const date = new Date(dateString);
  const seconds = Math.floor((new Date() - date) / 1000);
  let interval = seconds / 31536000;
  if (interval > 1) return Math.floor(interval) + "y ago";
  interval = seconds / 2592000;
  if (interval > 1) return Math.floor(interval) + "mo ago";
  interval = seconds / 86400;
  if (interval > 1) return Math.floor(interval) + "d ago";
  interval = seconds / 3600;
  if (interval > 1) return Math.floor(interval) + "h ago";
  interval = seconds / 60;
  if (interval > 1) return Math.floor(interval) + "m ago";
  return Math.floor(seconds) + "s ago";
}

function stripTags(html) {
  var tmp = document.createElement("div");
  tmp.innerHTML = html;
  return tmp.textContent || tmp.innerText || "";
}

function decodeHtml(html) {
  if (!html) return "";
  var txt = document.createElement("textarea");
  txt.innerHTML = html;
  return txt.value.replace(/<!\[CDATA\[(.*?)\]\]>/g, "$1");
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
    guestAccount: "Guest Account",
    signInRegister: "Sign In / Register",
    uncategorized: "Uncategorized",
    newPlaylist: "New Playlist",
    renamePlaylist: "Rename",
    deletePlaylist: "Delete Playlist",
    moveToPlaylist: "Move to playlist",
    favoriteAll: "Favorite All",
    favorited: "Favorited!",
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
    guestAccount: "حساب ضيف",
    signInRegister: "تسجيل الدخول / إنشاء حساب",
    uncategorized: "غير مصنف",
    newPlaylist: "قائمة جديدة",
    renamePlaylist: "إعادة تسمية",
    deletePlaylist: "حذف القائمة",
    moveToPlaylist: "نقل إلى قائمة",
    favoriteAll: "تفضيل الكل",
    favorited: "تم التفضيل!",
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
  var profileName = document.getElementById("profile-name");
  var profileBtn = document.getElementById("profile-auth-btn");
  if (profileName && isGuest) {
    profileName.textContent = t("guestAccount");
  }
  if (profileBtn) {
    profileBtn.textContent = isGuest ? t("signInRegister") : t("logout");
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
let historyArticles = [];
let allPlaylists = [];
let collapsedPlaylists = new Set();
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
    var res = await apiFetch("/api/feeds?t=" + Date.now());
    if (!res.ok) throw new Error("Failed to fetch feeds");
    var feeds = await res.json();
    await loadPlaylists();
    renderFeedList(feeds);
    await loadNews();
  } catch {
    list.innerHTML =
      '<div class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">' + t("noSubscriptions") + '</div>';
    showToast(t("couldNotLoadSubs"), true);
  }
}

async function loadPlaylists() {
  try {
    var res = await apiFetch("/api/playlists?t=" + Date.now());
    if (!res.ok) throw new Error("Failed to fetch playlists");
    allPlaylists = await res.json();
    var saved = localStorage.getItem("collapsedPlaylists");
    if (saved) {
      collapsedPlaylists = new Set(JSON.parse(saved));
    }
  } catch {
    allPlaylists = [];
  }
}

async function createPlaylist(name) {
  try {
    var res = await apiFetch("/api/playlists", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: name })
    });
    if (!res.ok) throw new Error("Failed");
    var playlist = await res.json();
    allPlaylists.push(playlist);
    renderFeedList(allFeeds);
    return playlist;
  } catch (e) {
    showToast(e.message || "Could not create playlist", true);
    return null;
  }
}

async function renamePlaylist(id, newName) {
  try {
    var res = await apiFetch("/api/playlists/" + id, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: newName })
    });
    if (!res.ok) throw new Error("Failed");
    var p = allPlaylists.find(function (pl) { return pl.id === id; });
    if (p) p.name = newName;
    renderFeedList(allFeeds);
  } catch (e) {
    showToast(e.message || "Could not rename playlist", true);
  }
}

async function deletePlaylist(id, deleteFeeds) {
  try {
    var res = await apiFetch("/api/playlists/" + id + "?deleteFeeds=" + (deleteFeeds ? "true" : "false"), { method: "DELETE" });
    if (!res.ok) throw new Error("Failed");
    allPlaylists = allPlaylists.filter(function (p) { return p.id !== id; });
    if (deleteFeeds) {
      allFeeds = allFeeds.filter(function (f) { return f.playlistId !== id; });
    } else {
      allFeeds.forEach(function (f) { if (f.playlistId === id) f.playlistId = null; });
    }
    var groupEl = document.querySelector('.playlist-group[data-playlist-id="' + id + '"]');
    if (groupEl) {
      groupEl.classList.add("playlist-removing");
      setTimeout(function () { renderFeedList(allFeeds); }, 350);
    } else {
      renderFeedList(allFeeds);
    }
  } catch (e) {
    showToast(e.message || "Could not delete playlist", true);
  }
}

var pendingDeletePlaylistId = null;

function showPlaylistDeleteModal(playlistId) {
  pendingDeletePlaylistId = playlistId;
  document.getElementById("playlist-delete-modal").style.display = "flex";
}

function hidePlaylistDeleteModal() {
  document.getElementById("playlist-delete-modal").style.display = "none";
  pendingDeletePlaylistId = null;
}

async function favoriteAllInPlaylist(playlistId, menuItem) {
  try {
    var res = await apiFetch("/api/playlists/" + playlistId + "/favorite", { method: "PATCH" });
    if (!res.ok) throw new Error("Failed");
    allFeeds.forEach(function (f) {
      if (f.playlistId === playlistId) f.isFavorite = true;
    });
    renderFeedList(allFeeds);
    if (menuItem) {
      var origText = menuItem.textContent;
      menuItem.textContent = "\u2714 " + t("favorited");
      menuItem.classList.add("playlist-menu-item--success");
      setTimeout(function () {
        menuItem.textContent = origText;
        menuItem.classList.remove("playlist-menu-item--success");
      }, 2000);
    }
  } catch (e) {
    showToast(e.message || "Could not favorite feeds", true);
  }
}

function closeAllPlaylistMenus() {
  document.querySelectorAll(".playlist-menu").forEach(function (m) { m.style.display = "none"; });
}

async function assignFeedToPlaylist(feedId, playlistId) {
  try {
    var res = await apiFetch("/api/feeds/" + feedId + "/playlist", {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playlistId: playlistId || null })
    });
    if (!res.ok) throw new Error("Failed");
    var feed = allFeeds.find(function (f) { return f.id === feedId; });
    if (feed) feed.playlistId = playlistId || null;
    renderFeedList(allFeeds);
  } catch (e) {
    showToast(e.message || "Could not assign feed", true);
  }
}

function populatePlaylistDropdown() {
  var select = document.getElementById("feed-playlist-select");
  if (!select) return;
  var currentVal = select.value;
  select.innerHTML = '<option value="">' + t("uncategorized") + '</option>';
  allPlaylists.forEach(function (p) {
    var opt = document.createElement("option");
    opt.value = p.id;
    opt.textContent = p.name;
    select.appendChild(opt);
  });
  if (currentVal && allPlaylists.some(function (p) { return p.id === currentVal; })) {
    select.value = currentVal;
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
  var currentTitles = new Set(feeds.map(function (f) { return f.title; }));
  excludedFeedTitles.forEach(function (title) {
    if (!currentTitles.has(title)) excludedFeedTitles.delete(title);
  });

  var feedsToRender = allFeeds;
  if (currentFeedTab === "fav") {
    feedsToRender = allFeeds.filter(function (f) { return f.isFavorite; });
  }

  var list = document.getElementById("feed-list");
  populatePlaylistDropdown();

  if (feedsToRender.length === 0) {
    list.innerHTML =
      '<div class="feed-item" style="color:#9ca3af; padding:1rem 1.25rem;">' + t("noSubscriptions") + '</div>';
    return;
  }

  var seen = new Set();
  var grouped = {};
  feedsToRender.forEach(function (f) {
    if (seen.has(f.id)) return;
    seen.add(f.id);
    var key = f.playlistId || "__uncategorized__";
    if (!grouped[key]) grouped[key] = [];
    grouped[key].push(f);
  });

  var keys = Object.keys(grouped).sort(function (a, b) {
    if (a === "__uncategorized__") return 1;
    if (b === "__uncategorized__") return -1;
    var pa = allPlaylists.find(function (p) { return p.id === a; });
    var pb = allPlaylists.find(function (p) { return p.id === b; });
    return (pa ? pa.name : "").localeCompare(pb ? pb.name : "");
  });

  var html = "";

  keys.forEach(function (key) {
    var groupFeeds = grouped[key].slice().sort(function (a, b) {
      if (a.isFavorite !== b.isFavorite) return a.isFavorite ? -1 : 1;
      return (a.title || "").localeCompare(b.title || "");
    });

    var playlist = allPlaylists.find(function (p) { return p.id === key; });
    var playlistName = playlist ? playlist.name : t("uncategorized");
    var playlistId = playlist ? playlist.id : "";
    var collapsed = collapsedPlaylists.has(key) ? " collapsed" : "";

    html += '<div class="playlist-group' + collapsed + '" data-playlist-id="' + escapeAttr(playlistId) + '" data-group-key="' + escapeAttr(key) + '">';
    html += '<div class="playlist-header">';
    html += '<svg class="playlist-chevron" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>';
    html += '<span class="playlist-name">' + escapeHtml(playlistName) + '</span>';
    html += '<span class="playlist-count">(' + groupFeeds.length + ')</span>';
    if (playlistId) {
      html += '<button class="playlist-kebab" data-action="toggle-menu" title="' + escapeAttr("Actions") + '">\u22EE</button>';
      html += '<div class="playlist-menu" style="display:none;">';
      html += '<button class="playlist-menu-item" data-action="favorite-all">\u2605 ' + t("favoriteAll") + '</button>';
      html += '<button class="playlist-menu-item" data-action="rename">\u270E ' + t("renamePlaylist") + '</button>';
      html += '<button class="playlist-menu-item playlist-menu-item--danger" data-action="delete">\uD83D\uDDD1 ' + t("deletePlaylist") + '</button>';
      html += '</div>';
    }
    html += '</div>';
    html += '<div class="playlist-feeds">';

    groupFeeds.forEach(function (f) {
      html += '<div class="feed-item feed-item-card" draggable="true" data-feed-id="' + escapeAttr(f.id) + '">';
      html += '<div class="feed-item-info">';
      html += '<input type="checkbox" class="feed-toggle" data-id="' + escapeAttr(f.id) + '" ' + (excludedFeedTitles.has(f.title) ? "" : "checked") + ' title="' + escapeAttr(t("showHideFeed")) + '">';
      html += getFaviconHtml(f);
      html += '<label class="feed-label">';
      html += '<span class="feed-name" title="' + escapeAttr(f.title) + '">' + escapeHtml(f.title) + '</span>';
      html += '<span class="feed-count">(' + (f.articleCount || 0) + ')</span>';
      html += '</label>';
      html += '</div>';
      html += '<div class="feed-item-options">';
      html += '<button class="feed-favorite" data-id="' + escapeAttr(f.id) + '" title="' + escapeAttr(t("toggleFavorite")) + '">';
      html += '<svg width="14" height="14" viewBox="0 0 24 24" fill="' + (f.isFavorite ? "currentColor" : "none") + '" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>';
      html += '</button>';
      html += '<button class="feed-share" data-url="' + escapeAttr(f.url) + '" title="' + escapeAttr(t("copyFeedLink")) + '">';
      html += '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>';
      html += '</button>';
      html += '<button class="feed-refresh" data-url="' + escapeAttr(f.url) + '" title="' + escapeAttr(t("refreshThisFeed")) + '">';
      html += '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/></svg>';
      html += '</button>';
      html += '<select class="feed-playlist-select" data-feed-id="' + escapeAttr(f.id) + '" title="' + escapeAttr(t("moveToPlaylist")) + '">';
      html += '<option value="">' + t("uncategorized") + '</option>';
      allPlaylists.forEach(function (p) {
        html += '<option value="' + escapeAttr(p.id) + '"' + (f.playlistId === p.id ? " selected" : "") + '>' + escapeHtml(p.name) + '</option>';
      });
      html += '</select>';
      html += '<button class="feed-delete" data-id="' + escapeAttr(f.id) + '" title="' + escapeAttr(t("unsubscribe")) + '">&times;</button>';
      html += '</div>';
      html += '</div>';
    });

    html += '</div></div>';
  });

  list.innerHTML = html;
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
    var res = await apiFetch("/api/refresh-feed?retentionDays=" + (localStorage.getItem("retentionDays") || "30"), {
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
    var res = await apiFetch("/api/feeds/" + id, { method: "DELETE" });
    if (!res.ok) throw new Error("Failed to delete feed");
    await loadFeeds();
    showToast(t("feedRemoved"), false);
  } catch {
    showToast(t("couldNotRemove"), true);
  }
}

async function toggleFavoriteFeed(id) {
  try {
    var res = await apiFetch("/api/feeds/" + id + "/favorite", { method: "PATCH" });
    if (!res.ok) throw new Error("Failed to toggle favorite");
    var feed = allFeeds.find(function (f) { return f.id === id; });
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
  var container = document.getElementById("articles-container");
  var emptyState = document.getElementById("empty-state");
  emptyState.classList.add("hidden");
  container.innerHTML = skeletonHtml();

  try {
    var url = "/api/news?retentionDays=" + (localStorage.getItem("retentionDays") || "30");
    var res = await fetch(url, { credentials: "include", headers: { "X-Guest-Session": isGuest ? getSessionId() : "" } });
    if (!res.ok) throw new Error("Failed to fetch news: HTTP " + res.status);
    allArticles = await res.json();
    renderPage(1);
  } catch (err) {
    console.error("Fetch error:", err);
    container.textContent = "";
    emptyState.textContent = t("feedQuiet");
    emptyState.classList.remove("hidden");
    showToast(t("couldNotLoadArticles"), true);
  }
}

async function loadHistory() {
  try {
    var res = await apiFetch("/api/articles/history");
    if (res.ok) {
      historyArticles = await res.json();
    }
  } catch {
    historyArticles = [];
  }
}

function skeletonHtml() {
  return Array.from({ length: 3 }, function () { return '<article class="article-card skeleton-article"><div class="article-feed-source"><span class="skeleton-block skeleton-tag"></span></div><h3 class="article-title"><span class="skeleton-block skeleton-text"></span></h3><div class="article-meta"><span class="skeleton-block skeleton-text skeleton-text--short"></span></div><p class="article-summary"><span class="skeleton-block skeleton-text"></span><span class="skeleton-block skeleton-text skeleton-text--medium"></span></p></article>'; }).join("");
}

function getFilteredArticles() {
  const q = searchQuery.trim().toLowerCase();
  var source = currentArticleTab === "history" ? historyArticles : allArticles;
  return source.filter((a) => {
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
    tag.innerHTML = decodeHtml(a.feedTitle);
    tag.style.backgroundColor = getColorForFeed(a.feedTitle);
    source.appendChild(tag);

    const title = document.createElement("h3");
    title.className = "article-title";
    title.dir = "auto";
    const link = document.createElement("a");
    link.href = a.link;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.innerHTML = decodeHtml(a.title);
    title.appendChild(link);

    const meta = document.createElement("div");
    meta.className = "article-meta";
    meta.textContent = timeSince(a.publishDate) + " \u2022 " + formatDate(a.publishDate);

    const contentWrapper = document.createElement("div");
    contentWrapper.className = "article-content-wrapper";
    contentWrapper.appendChild(source);
    contentWrapper.appendChild(title);
    contentWrapper.appendChild(meta);

    const summary = document.createElement("div");
    summary.className = "article-summary";
    summary.dir = "auto";
    summary.innerHTML = decodeHtml(a.summary);
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
      apiFetch("/api/news/" + a.id + "/bookmark", { method: "PATCH" })
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
      apiFetch("/api/news/" + a.id + "/read", { method: "PATCH" });
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

      if (isGuest) {
        var chatHeaders = { "Content-Type": "application/json" };
        if (isGuest) chatHeaders["X-Guest-Session"] = getSessionId();
        fetch("/api/ai/chat?lang=" + currentLocale, {
          method: "POST", headers: chatHeaders,
          body: JSON.stringify({ message: "Summarize this article:\n\nTitle: " + a.title + "\n\n" + stripTags(a.summary) }),
          credentials: "include"
        })
          .then(function (res) {
            if (res.status === 429) return res.json().then(function (d) { throw new Error(d.error); });
            return res.json();
          })
          .then(function (data) { body.innerHTML = marked.parse(data.response); updateQuotaUI(); })
          .catch(function (err) { body.innerHTML = '<p style="color:var(--color-error)">' + (err.message || 'Failed to generate summary.') + '</p>'; })
          .finally(function () { deepBtn.disabled = false; deepBtn.classList.remove("btn-refreshing"); });
        return;
      }

      var summaryHeaders = { "Content-Type": "application/json" };
      if (isGuest) summaryHeaders["X-Guest-Session"] = getSessionId();
      fetch("/api/ai/summary/article/" + a.id + "?lang=" + currentLocale, { credentials: "include", headers: summaryHeaders })
        .then(function (res) {
          if (res.status === 429) return res.json().then(function (d) { throw new Error(d.error); });
          return res.json();
        })
        .then(function (data) {
          body.innerHTML = marked.parse(data.summary);
          updateQuotaUI();
        })
        .catch(function (err) {
          body.innerHTML = '<p style="color:var(--color-error)">' + (err.message || 'Failed to generate summary.') + '</p>';
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

function showFeatureGate() {
  document.getElementById("feature-gate-modal").style.display = "flex";
}

function setupAddFeedForm() {
  const form = document.getElementById("add-feed-form");
  const textarea = document.getElementById("feed-url");
  const usernameInput = document.getElementById("feed-username");
  const passwordInput = document.getElementById("feed-password");
  const playlistSelect = document.getElementById("feed-playlist-select");

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
    const playlistId = playlistSelect ? (playlistSelect.value || null) : null;

    const btn = form.querySelector("button[type='submit']");
    btn.disabled = true;
    btn.textContent = t("processingBatch");

    try {
      const res = await apiFetch("/api/feeds/batch", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ urls: urls, username: username, password: password, playlistId: playlistId }),
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

var isGuest = false;
var guestSessionId = "";

function getSessionId() {
  if (!guestSessionId) {
    guestSessionId = localStorage.getItem("session_id") || "";
    if (!guestSessionId) {
      guestSessionId = crypto.randomUUID ? crypto.randomUUID() : "sess-" + Date.now() + "-" + Math.random().toString(36).slice(2,10);
      localStorage.setItem("session_id", guestSessionId);
    }
  }
  return guestSessionId;
}

var quotaTimer = null;

async function updateQuotaUI() {
  var el = document.getElementById("ai-quota-counter");
  if (!el) return;
  try {
    var res = await apiFetch("/api/quota");
    if (!res.ok) return;
    var data = await res.json();
    el.style.display = "";

    var pct = data.limit > 0 ? Math.round((data.used / data.limit) * 100) : 0;
    var exhausted = data.used >= data.limit;

    el.querySelector(".quota-numbers").textContent = data.used + "/" + data.limit;
    el.querySelector(".quota-bar-fill").style.width = pct + "%";
    el.querySelector(".quota-bar-fill").className = "quota-bar-fill" + (exhausted ? " exhausted" : "");

    if (exhausted) el.classList.add("quota-exhausted");
    else el.classList.remove("quota-exhausted");

    if (quotaTimer) clearInterval(quotaTimer);
    var resetEl = el.querySelector(".quota-reset-timer");
    function tick() {
      var now = Date.now();
      var target = new Date(data.nextReset).getTime();
      var diff = Math.max(0, target - now);
      if (diff <= 0) {
        resetEl.textContent = "Resetting...";
        if (quotaTimer) clearInterval(quotaTimer);
        return;
      }
      var h = Math.floor(diff / 3600000);
      var m = Math.floor((diff % 3600000) / 60000);
      resetEl.textContent = "Resets in " + h + "h " + m + "m";
    }
    tick();
    quotaTimer = setInterval(tick, 30000);
  } catch {
    el.style.display = "none";
  }
}

function apiFetch(url, options) {
  options = options || {};
  options.credentials = "include";
  options.headers = options.headers || {};
  if (isGuest) options.headers["X-Guest-Session"] = getSessionId();
  else delete options.headers["X-Guest-Session"];
  return fetch(url, options).then(function (res) {
    if (!res.ok) {
      var contentType = res.headers.get("content-type") || "";
      if (contentType.indexOf("application/json") !== -1) {
        return res.json().then(function (data) {
          throw new Error(data.error || data.message || "Request failed (" + res.status + ")");
        });
      }
      return res.text().then(function () {
        throw new Error("Server error (" + res.status + "). Please try again.");
      });
    }
    return res;
  });
}

async function checkAuth() {
  try {
    var res = await fetch("/api/auth/me", { credentials: "include" });
    if (res.ok) {
      var data = await res.json();
      isGuest = data.isGuest === true;
      buildProfileHeader(data);
      setupAuthorized();
      updateQuotaUI();
      return;
    }
  } catch (e) {
    console.error("Auth check failed:", e);
  }
  isGuest = true;
  buildProfileHeader({ email: "", isGuest: true });
  setupAuthorized();
  updateQuotaUI();
}

function buildProfileHeader(data) {
  var avatar = document.getElementById("profile-avatar");
  var name = document.getElementById("profile-name");
  var authBtn = document.getElementById("profile-auth-btn");

  if (!data.isGuest) {
    var firstLetter = (data.email || "U")[0].toUpperCase();
    avatar.style.backgroundColor = getColorForFeed(data.email);
    avatar.innerHTML = '<a href="/profile.html" style="display:flex;align-items:center;justify-content:center;width:100%;height:100%;border-radius:50%;text-decoration:none">' +
      '<span class="avatar-letter">' + firstLetter + '</span></a>';
    name.textContent = data.email || "";
    authBtn.textContent = t("logout");
    authBtn.className = "profile-auth-btn sign-out-btn";
    authBtn.onclick = async function () {
      await fetch("/api/auth/logout", { method: "POST", credentials: "include" });
      window.location.href = "/welcome.html";
    };
  } else {
    avatar.style.backgroundColor = "";
    avatar.innerHTML = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
      '<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>';
    name.textContent = t("guestAccount");
    authBtn.textContent = t("signInRegister");
    authBtn.className = "profile-auth-btn sign-in-btn";
    authBtn.onclick = function () { window.location.href = "/welcome.html"; };
  }
}

var authorizedSetupDone = false;

function setupAuthorized() {
  document.getElementById("auth-loading").style.display = "none";
  document.getElementById("app-layout").style.display = "";
  updateQuotaUI();

  if (authorizedSetupDone) {
    loadFeeds();
    return;
  }
  authorizedSetupDone = true;

  document.getElementById("hamburger-btn").addEventListener("click", function () {
    document.querySelector(".sidebar").classList.toggle("open");
    document.getElementById("sidebar-backdrop").classList.toggle("open");
  });
  document.getElementById("sidebar-backdrop").addEventListener("click", function () {
    document.querySelector(".sidebar").classList.remove("open");
    this.classList.remove("open");
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
      var res;
      if (isGuest) {
        var top = allArticles.slice(0, 5).map(function(a) { return decodeHtml(a.title) + ": " + decodeHtml(a.summary); }).join("\n\n");
        var briefingHeaders = { "Content-Type": "application/json" };
        briefingHeaders["X-Guest-Session"] = getSessionId();
        res = await fetch("/api/ai/chat?lang=" + currentLocale, {
          method: "POST", headers: briefingHeaders,
          body: JSON.stringify({ message: "Generate a daily news briefing from these articles:\n\n" + top }),
          credentials: "include"
        });
        if (res.status === 429) { var qd = await res.json(); showFeatureGate(); return; }
        if (!res.ok) throw new Error("Failed");
        var d = await res.json();
        content.innerHTML = marked.parse(d.response);
        updateQuotaUI();
      } else {
        res = await fetch("/api/news/daily-briefing?lang=" + currentLocale, { credentials: "include" });
        if (res.status === 429) { var qdata = await res.json(); content.textContent = qdata.error; return; }
        if (!res.ok) throw new Error("Failed to generate briefing");
        var data = await res.json();
        content.innerHTML = marked.parse(data.summary);
        updateQuotaUI();
      }
    } catch (err) {
      content.textContent = "Could not generate briefing. Please try again.";
    } finally {
      btn.disabled = false;
      btn.classList.remove("btn-refreshing");
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
      var chatHeaders = { "Content-Type": "application/json" };
      if (isGuest) chatHeaders["X-Guest-Session"] = getSessionId();
      var res = await fetch("/api/chat?lang=" + currentLocale, {
        method: "POST",
        headers: chatHeaders,
        body: JSON.stringify({ message: msg }),
        credentials: "include"
      });
      if (res.status === 429) {
        var qdata = await res.json();
        if (isGuest) { showFeatureGate(); throw new Error(""); }
        throw new Error(qdata.error || "Rate limit exceeded.");
      }
      if (!res.ok) throw new Error("Chat failed");
      var data = await res.json();

      typingDiv.remove();

      var aiDiv = document.createElement("div");
      aiDiv.className = "chat-msg chat-msg-ai";
      aiDiv.innerHTML = marked.parse(data.response);
      updateQuotaUI();
      messages.appendChild(aiDiv);
    } catch (err) {
      typingDiv.textContent = err.message || "Sorry, something went wrong.";
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
    btn.addEventListener("click", async function (e) {
      document.querySelectorAll("[data-article-tab]").forEach(function (b) { b.classList.remove("active"); });
      e.target.classList.add("active");
      currentArticleTab = e.target.dataset.articleTab;
      if (currentArticleTab === "history") await loadHistory();
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
    var playlistHeader = e.target.closest(".playlist-header");
    if (playlistHeader) {
      var menuItem = e.target.closest(".playlist-menu-item");
      if (menuItem) {
        var menuGroup = menuItem.closest(".playlist-group");
        var menuPlaylistId = menuGroup.dataset.playlistId;
        var action = menuItem.dataset.action;
        if (action === "favorite-all") {
          favoriteAllInPlaylist(menuPlaylistId, menuItem);
        } else if (action === "rename") {
          closeAllPlaylistMenus();
          var renameName = menuGroup.querySelector(".playlist-name");
          var currentName = renameName.textContent;
          var input = document.createElement("input");
          input.type = "text";
          input.className = "playlist-rename-input";
          input.value = currentName;
          input.addEventListener("keydown", function (ev) {
            if (ev.key === "Enter") {
              renamePlaylist(menuPlaylistId, input.value.trim());
            }
            if (ev.key === "Enter" || ev.key === "Escape") {
              input.replaceWith(renameName);
            }
          });
          input.addEventListener("blur", function () {
            input.replaceWith(renameName);
          });
          renameName.replaceWith(input);
          input.focus();
          input.select();
        } else if (action === "delete") {
          closeAllPlaylistMenus();
          showPlaylistDeleteModal(menuPlaylistId);
        }
        return;
      }

      var kebabBtn = e.target.closest(".playlist-kebab");
      if (kebabBtn) {
        e.stopPropagation();
        var menu = kebabBtn.parentElement.querySelector(".playlist-menu");
        var isOpen = menu.style.display !== "none";
        closeAllPlaylistMenus();
        if (!isOpen) menu.style.display = "";
        return;
      }

      var group = playlistHeader.closest(".playlist-group");
      var key = group.dataset.groupKey;
      if (group.classList.contains("collapsed")) {
        group.classList.remove("collapsed");
        collapsedPlaylists.delete(key);
      } else {
        group.classList.add("collapsed");
        collapsedPlaylists.add(key);
      }
      localStorage.setItem("collapsedPlaylists", JSON.stringify(Array.from(collapsedPlaylists)));
      return;
    }
  });
  document.getElementById("feed-list").addEventListener("change", function (e) {
    var select = e.target.closest(".feed-playlist-select");
    if (select) {
      assignFeedToPlaylist(select.dataset.feedId, select.value);
    }
  });
  document.getElementById("feed-list").addEventListener("dragstart", function (e) {
    var feedItem = e.target.closest(".feed-item");
    if (!feedItem || !feedItem.dataset.feedId) return;
    e.dataTransfer.setData("text/plain", feedItem.dataset.feedId);
    e.dataTransfer.effectAllowed = "move";
    feedItem.style.opacity = "0.5";
  });
  document.getElementById("feed-list").addEventListener("dragend", function (e) {
    var feedItem = e.target.closest(".feed-item");
    if (feedItem) feedItem.style.opacity = "";
  });
  document.getElementById("feed-list").addEventListener("dragover", function (e) {
    var header = e.target.closest(".playlist-header");
    if (!header) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
    header.classList.add("drag-over");
  });
  document.getElementById("feed-list").addEventListener("dragleave", function (e) {
    var header = e.target.closest(".playlist-header");
    if (!header) return;
    header.classList.remove("drag-over");
  });
  document.getElementById("feed-list").addEventListener("drop", function (e) {
    var header = e.target.closest(".playlist-header");
    if (!header) return;
    e.preventDefault();
    header.classList.remove("drag-over");
    var feedId = e.dataTransfer.getData("text/plain");
    var group = header.closest(".playlist-group");
    var playlistId = group.dataset.playlistId || null;
    if (feedId && playlistId) {
      assignFeedToPlaylist(feedId, playlistId);
    }
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

  var gateModal = document.getElementById("feature-gate-modal");
  if (gateModal) {
    gateModal.querySelector(".modal-close").addEventListener("click", function () { gateModal.style.display = "none"; });
    gateModal.querySelector(".modal-backdrop").addEventListener("click", function () { gateModal.style.display = "none"; });
  }

  document.getElementById("read-modal").querySelector(".modal-close").addEventListener("click", closeModal);
  document.getElementById("read-modal").querySelector(".modal-backdrop").addEventListener("click", closeModal);

  document.getElementById("btn-new-playlist").addEventListener("click", function () {
    showInlinePlaylistCreator();
  });

  document.getElementById("btn-delete-cancel").addEventListener("click", function () {
    hidePlaylistDeleteModal();
  });
  document.getElementById("btn-delete-keep-feeds").addEventListener("click", function () {
    var id = pendingDeletePlaylistId;
    hidePlaylistDeleteModal();
    if (id) deletePlaylist(id, false);
  });
  document.getElementById("btn-delete-all").addEventListener("click", function () {
    var id = pendingDeletePlaylistId;
    hidePlaylistDeleteModal();
    if (id) deletePlaylist(id, true);
  });
  var playlistDeleteModal = document.getElementById("playlist-delete-modal");
  if (playlistDeleteModal) {
    playlistDeleteModal.querySelector(".modal-close").addEventListener("click", hidePlaylistDeleteModal);
    playlistDeleteModal.querySelector(".modal-backdrop").addEventListener("click", hidePlaylistDeleteModal);
  }
});

document.addEventListener("click", function (e) {
  if (!e.target.closest(".playlist-menu") && !e.target.closest(".playlist-kebab")) {
    closeAllPlaylistMenus();
  }
});

function showInlinePlaylistCreator() {
  var existing = document.querySelector(".inline-playlist-creator");
  if (existing) {
    existing.querySelector("input").focus();
    return;
  }

  var creator = document.createElement("div");
  creator.className = "inline-playlist-creator";
  creator.innerHTML = '<input type="text" placeholder="' + t("newPlaylist") + '" maxlength="50">' +
    '<button class="btn btn-primary">' + t("add") + '</button>' +
    '<button class="btn btn-secondary">' + t("close") + '</button>';

  var sidebarHeader = document.querySelector(".sidebar-header");
  sidebarHeader.insertAdjacentElement("afterend", creator);

  var input = creator.querySelector("input");
  var addBtn = creator.querySelector(".btn-primary");
  var cancelBtn = creator.querySelector(".btn-secondary");

  input.focus();

  var submitCreate = async function () {
    var name = input.value.trim();
    if (!name) return;
    await createPlaylist(name);
    creator.remove();
  };

  addBtn.addEventListener("click", submitCreate);
  input.addEventListener("keydown", function (ev) {
    if (ev.key === "Enter") submitCreate();
    if (ev.key === "Escape") creator.remove();
  });
  cancelBtn.addEventListener("click", function () { creator.remove(); });
  input.addEventListener("blur", function () { setTimeout(function () { if (document.activeElement !== addBtn && document.activeElement !== cancelBtn) creator.remove(); }, 150); });
}

window.addEventListener("pageshow", function (event) {
  if (event.persisted) {
    checkAuth();
  }
});

function filterSidebarFeeds() {
  var query = document.getElementById("feed-search-sidebar").value.trim().toLowerCase();
  var groups = document.querySelectorAll("#feed-list .playlist-group");

  groups.forEach(function (group) {
    var items = group.querySelectorAll(".feed-item");
    var visibleCount = 0;

    items.forEach(function (item) {
      var titleSpan = item.querySelector(".feed-name");
      if (!titleSpan) return;

      var title = (titleSpan.textContent || "").toLowerCase();

      if (!query || title.indexOf(query) !== -1) {
        item.style.display = "";
        visibleCount++;
      } else {
        item.style.display = "none";
      }
    });

    if (visibleCount === 0 && query) {
      group.style.display = "none";
    } else {
      group.style.display = "";
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