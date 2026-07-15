# Personal RSS Reader

A lightweight, high-performance, and secure web-based RSS/Atom feed reader. Built with a focus on modern UI/UX, concurrent processing, and robust XSS prevention.

## 🚀 Features

* **River of News:** Aggregates multiple RSS and Atom feeds into a single, beautifully paginated timeline.
* **Granular Feed Control (V3):** Instantly filter specific feeds in or out of your timeline without requiring a full page reload or backend fetch.
* **Granular Refreshing (V3):** Refresh individual feeds to pull their latest data into the cache without bottlenecking the server with global fetches.
* **Advanced Navigation (V3):** Instantly search and filter loaded articles with a debounced search bar, and jump directly to specific pages in the timeline.
* **Dark Mode (V3):** Fully integrated dark theme utilizing CSS variables. Automatically respects the user's OS-level preferences while offering a persistent manual toggle.
* **Smart Performance:** Utilizes concurrent network requests, aggressive in-memory caching, and frontend DOM batching (`DocumentFragment`) to ensure lightning-fast load times and smooth rendering.
* **Bulletproof Security:** Implements strict backend HTML sanitization and URI scheme validation to neutralize Cross-Site Scripting (XSS) and malicious payloads.
* **Resilient Parsing:** Handles broken, messy, or non-standard real-world XML feeds gracefully without crashing, automatically decoding double-escaped HTML entities.

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
* **Backend Sanitization:** Before parsing or saving feed data, the backend passes all Titles and Summaries through `HtmlSanitizer` to strip dangerous tags.
* **Strict URI Validation:** Hyperlinks are explicitly checked against a `{ Scheme: "http" or "https" }` pattern to prevent `javascript:` execution attacks.
* **Safe Rich Text:** Because the backend guarantees a clean HTML payload, the frontend can safely render rich text (bolding, italics, code blocks) directly without risking injection.

### 2. Performance (Concurrency, Caching, & DOM Rendering)
To prevent the application from making dozens of slow HTTP requests and dropping browser frames:
* **Concurrent Fetching:** Feed fetching is orchestrated using `Task.WhenAll`, allowing all subscriptions to be downloaded simultaneously.
* **In-Memory Caching:** The aggregated "River of News" is stored in the server's RAM using `IMemoryCache` for 5 minutes.
* **Optimized Rendering:** The frontend avoids heavy browser recalculations by debouncing input events and batching DOM updates entirely in memory before painting them to the screen.

### 3. Technical Trade-offs: Feed Parsing
For the initial prototype, the application utilized the built-in `System.ServiceModel.Syndication` library to minimize external dependencies. However, real-world RSS feeds are highly fragmented. For the production-ready build, the architecture uses `CodeHollow.FeedReader`. This community-supported parser provides superior fault tolerance and a unified data model. The app intentionally acts as a summary aggregator (parsing descriptions rather than full HTML content) to bypass edge-case publisher lazy-loading hacks and maintain a stable, lightning-fast UI.

## 💻 Getting Started

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### Run Locally
1. Clone the repository.
2. Navigate to the root directory in your terminal.
3. Run the application:
   ```bash
   dotnet run