# Personal RSS Reader

A lightweight, high-performance, and secure web-based RSS/Atom feed reader. Built with a focus on modern UI/UX, concurrent processing, and robust XSS prevention.

## 🚀 Features

* **River of News:** Aggregates multiple RSS and Atom feeds into a single, beautifully paginated timeline.

* **Smart Performance:** Utilizes concurrent network requests and aggressive in-memory caching to ensure lightning-fast load times.

* **Bulletproof Security:** Implements strict backend HTML sanitization and URI scheme validation to neutralize Cross-Site Scripting (XSS) and malicious payloads.

* **Modern UI/UX:** Features skeleton loading states, CSS line-clamping for clean layouts, deterministic color-hashed feed tags, and a distraction-free reading modal.

* **Resilient Parsing:** Handles broken, messy, or non-standard real-world XML feeds gracefully without crashing.

## 🛠️ Tech Stack

* **Backend:** C# / .NET 8 (Minimal APIs)

* **Frontend:** Vanilla HTML5, CSS3, ES6 JavaScript (No frameworks)

* **Data Storage:** Local JSON file (`feeds.json`)

* **Libraries:**

  * `HtmlSanitizer` (Backend HTML scrubbing)

  * `CodeHollow.FeedReader` (Robust RSS/Atom parsing)

## 🏗️ Architecture & Design Decisions

### 1. Security First (Defeating XSS)

RSS feeds are fundamentally untrusted external input. This application employs a defense-in-depth strategy:

* **Backend Sanitization:** Before parsing or saving feed data, the backend passes all Titles and Summaries through `HtmlSanitizer` to strip dangerous tags (like `<script>`).

* **Strict URI Validation:** Hyperlinks are explicitly checked against a `{ Scheme: "http" or "https" }` pattern to prevent `javascript:` execution attacks.

* **Safe Rich Text:** Because the backend guarantees a clean HTML payload, the frontend can safely render rich text (bolding, italics, code blocks) using `innerHTML` without risking injection, providing a vastly superior reading experience.

### 2. Performance (Concurrency & Caching)

To prevent the application from making dozens of slow HTTP requests every time a user refreshes the page:

* **Concurrent Fetching:** Feed fetching is orchestrated using `Task.WhenAll`, allowing all subscriptions to be downloaded simultaneously rather than sequentially.

* **In-Memory Caching:** The aggregated "River of News" is stored in the server's RAM using `IMemoryCache` for 5 minutes. Subsequent page loads are served instantaneously. The cache is smartly invalidated whenever a user adds or removes a subscription.

### 3. Technical Trade-offs: Feed Parsing

*Initial Prototype vs. Production V2*
For the initial prototype, the application utilized the built-in `System.ServiceModel.Syndication` library to minimize external dependencies. However, real-world RSS feeds are highly fragmented and frequently contain malformed XML. For the production-ready V2, the architecture was migrated to `CodeHollow.FeedReader`. This community-supported parser provides much better fault tolerance and a unified data model, drastically reducing parsing exceptions from edge-case blogs.

## 💻 Getting Started

### Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### Run Locally

1. Clone the repository.

2. Navigate to the root directory in your terminal.

3. Run the application:

   ```bash
   dotnet run

4. Open your browser and navigate to 
    http://localhost:5152.