# Personal RSS Reader

A lightweight, secure, and AI-enhanced web-based RSS/Atom feed reader. Built with a focus on modern UI/UX, concurrent processing, and AI utility.

Live Demo: https://feedrss.runasp.net/

## 🚀 Features

*   **River of News:** Aggregates multiple RSS and Atom feeds into a single, beautifully paginated timeline.
*   **Authentication (V4):** Google Sign-In and local Email/Password authentication supported.
*   **Guest Sandbox (V4):** Fully functional unauthenticated tryout experience utilizing database "Shadow Accounts."
*   **AI Integrations (V4):** Deep article summaries, automated Daily Briefings, and conversational AI chat with your feeds (via DeepSeek). Live usage quota tracking in the UI.
*   **Community & Social (V5):** A built-in community tab with a full social timeline. Share articles directly to the community, edit or delete your own posts, react with a categorized emoji picker, and comment on posts. Includes user blocking (mutual), dynamic RTL/text-direction support for Arabic content, and `@mention` autocomplete for subscribed feeds.
*   **Granular Control (V3/V4/V5):** Filter feeds in/out of the timeline instantly with hierarchical category checkboxes. Aggregate all feeds, view single subscriptions, or filter by playlist category. Refresh individual feeds on demand.
*   **Bookmarks & Favorites (V4):** Save individual articles as bookmarks and mark favorite feeds.
*   **Advanced UI/UX (V3/V4/V5):** Search bar, advanced pagination, relative timestamps (e.g., '2h ago'), high-quality image handling, podcast support, smooth custom CSS checkboxes, bulk multi-select feed adding from curated recommendations, and a 30-second automated onboarding prompt for new empty accounts.
*   **Dark Mode (V3):** Fully integrated, OS-respecting dark theme with a manual persistent toggle. SVG-based professional logo and favicon.
*   **Smart Performance:** Concurrent network requests, aggressive backend caching, and optimized frontend DOM batching (`DocumentFragment`).
*   **Bulletproof Security:** Backend HTML sanitization, URI scheme validation, and anti-XSS measures.

## 🛠️ Tech Stack

*   **Backend:** C# / .NET 8 (Minimal APIs)
*   **Frontend:** Vanilla HTML5, CSS3, ES6 JavaScript (No frameworks), `marked.js` for Markdown rendering.
*   **Data Storage:** SQL Database (Entity Framework Core)
*   **Libraries/Integrations:**
    *   `HtmlSanitizer` (XSS prevention)
    *   `CodeHollow.FeedReader` (RSS/Atom parsing)
    *   Entity Framework Core (Sqlite in development, MSSQL/PostgreSQL in production)
    *   DeepSeek AI API (Summaries, Briefings, Chat)

## 🏗️ Architecture & Design Decisions

### 1. Security First
RSS feeds are untrusted external input. Strict backend `HtmlSanitizer` scrubbing and URI scheme validation neutralized XSS risks.

### 2. The Shadow Account Architecture for Guests
To prevent duplicate code, the application uses a unified architecture for both guests and authenticated users. Guests are assigned a persistent `X-Guest-Session` UUID in `localStorage`. In the backend, data entities are marked with a nullable `UserId` and a nullable `GuestSessionId`. This allows the application to use the exact same SQL schema and API endpoints for all users, filtering data based on the presence of either an authenticated User ID or a Guest Session ID.

### 3. AI Service Unification and Rate Limiting
To keep costs low, AI usage is rate-limited on a daily basis. AI features require distinct logical branching for data access: the quota is tracked using a `guest-{IP}` key to prevent session-hopping abuse, while actual database queries utilize the `X-Guest-Session` key to retrieve the correct sandboxed articles.

### 4. Robust Fetch Engine & WAF Bypassing
The backend `HttpClient` is hardened with HTTP/2 prioritization, automatic GZIP/Brotli/Deflate decompression, and modern browser headers (including proper `User-Agent`, `Accept`, `Accept-Encoding`, and `Cache-Control`). This allows the reader to successfully bypass strict Web Application Firewalls — such as Cloudflare — and bot-protection on tech blogs that would otherwise reject standard HTTP requests.

## 💻 Getting Started

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### Run Locally
1. Clone the repository.
2. Navigate to the root directory in your terminal.
3. The database is initialized via `db.Database.EnsureCreated()` in `Program.cs`. No migrations are required for first run.
4. Run the application:
   ```bash
   dotnet run
