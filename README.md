# Quality Control Dashboard

A WPF desktop application that hosts a Vue 3 single-page application inside a **Microsoft WebView2** control. The two sides communicate via a structured JSON messaging protocol over the WebView2 bridge.

---

## Solution structure

```
QualityControlDashboard/
├── QualityControl.WPF/                 # C# WPF host application
│   ├── Messenger/
│   │   ├── IWebViewMessenger.cs
│   │   └── WebViewMessenger.cs         # C# message router
│   ├── Models/                         # Shared DTOs (TypeGen source)
│   └── MainWindow.xaml.cs              # Handler registration
│
└── quality-control-vue-dashboard/      # Vue 3 front-end (Vite)
    └── src/
        ├── services/
        │   └── webviewMessenger.ts     # TS message router (singleton `bus`)
        ├── models/                     # Auto-generated TypeScript DTOs
        └── composables/
            ├── useFilesTable.ts
            └── useUserForm.ts
```

---

## Communication bridge

Both sides share one envelope type — `WebMessage` — which is serialized as JSON and passed through the WebView2 native bridge.

### `WebMessage` shape

| Field | Type | Description |
|---|---|---|
| `id` | `string` (UUID) | Unique ID of the outgoing message |
| `type` | `TypeEnum` | `Request = 0`, `Response = 1`, `Stream = 2`, `Event = 3` |
| `name` | `string` | Channel / handler name (e.g. `"files.get"`) |
| `payload` | `object?` | Arbitrary JSON payload |
| `correlationId` | `string?` | Links a response back to its originating request `id` |

### `TypeEnum`

```csharp
// C#
public enum TypeEnum : byte { Request = 0, Response = 1, Stream = 2, Event = 3 }
```

```typescript
// TypeScript (auto-generated)
export enum TypeEnum { Request = 0, Response = 1, Stream = 2, Event = 3 }
```

> **Note:** The TypeScript models under `src/models/` are auto-generated from the C# classes in `QualityControl.WPF/Models/` using **TypeGen** (`typegen.bat` runs `typegen generate`). Never edit those files by hand.

---

## Communication modes

### 1. Request / Response

The most common pattern. TypeScript sends a request and awaits a single reply.

```
TypeScript                             C#
──────────────────────────────────────────────────────
bus.request("files.get", null)
  → WebMessage { type: Request,
                 id: <uuid-A>,
                 name: "files.get",
                 payload: null }
                                ReceiveMessageAsync()
                                  → looks up handler "files.get"
                                  → executes handler
                                  → Publish(Request, result,
                                      name: "files.get",
                                      correlationId: <uuid-A>)
  ← WebMessage { type: Request,
                 correlationId: <uuid-A>,
                 payload: [...] }
  → handleRequest() resolves Promise
```

**TypeScript side** — `WebViewMessenger.request<T>()`

```typescript
// Stores a pending Promise keyed by the outgoing message id
const results = await bus.request<FileResponseDto[]>("files.get", null);
```

- A UUID is generated for each call and stored in `pendingRequests`.
- A 30 s timeout (configurable) automatically rejects the promise if no reply arrives.
- When the reply arrives with a matching `correlationId`, the promise is resolved.

**C# side** — `WebViewMessenger.RegisterHandler<TRequest, TResponse>()`

```csharp
_messenger.RegisterHandler<object, List<FileResponseDto>>(
    "files.get",
    async _ => await _context.Files.Select(...).ToListAsync()
);
```

- `ReceiveMessageAsync` deserializes the incoming JSON, finds the registered handler by `message.Name`, awaits it, then calls `Publish()` with `correlationId = message.Id`.
- All C# → TS messages go through `CoreWebView2.PostWebMessageAsJson()`.

---

### 2. Stream

Used for large result sets. C# pushes multiple `StreamChunk<T>` messages asynchronously; TypeScript reassembles them incrementally.

```
TypeScript                             C#
──────────────────────────────────────────────────────
bus.subscribeStream(subscription, req)
  → WebMessage { type: Stream,
                 name: "datasets.get",
                 payload: DataSetRequestDto }
                                ReceiveMessageAsync()
                                  → executes handler
                                  → iterates IAsyncEnumerable
                                  → for each chunk:
                                      Publish(Stream, chunk,
                                        name: "datasets.get")
  ← WebMessage { type: Stream,
                 name: "datasets.get",
                 payload: { items:[...],
                            chunkIndex: 0,
                            isLastChunk: false } }
  → subscription.next(items, chunkIndex)

  ← ... more chunks ...

  ← WebMessage { payload: { isLastChunk: true } }
  → subscription.completed()
  → stream handler removed from registry
```

**TypeScript side** — `WebViewMessenger.subscribeStream<T>()`

```typescript
const subscription: StreamSubscription<DataSetResponseDto> = {
  streamId: "datasets.get",
  next(chunk, chunkIndex) {
    dataSetRows.value = [...dataSetRows.value, ...chunk];
  },
  completed() { /* all chunks received */ },
  error(err)  { /* handle error */       },
};

bus.subscribeStream(subscription, requestPayload);
```

The `StreamSubscription<T>` interface:

```typescript
interface StreamSubscription<T> {
  streamId: string;
  next(chunk: T[], chunkIndex: number): void;
  completed(): void;
  error?(err: any): void;
}
```

**C# side** — handler returns the final `StreamChunk<T>`, intermediate chunks are published manually:

```csharp
_messenger.RegisterHandler<DataSetRequestDto, StreamChunk<DataSetResponseDto>>(
    "datasets.get",
    async request =>
    {
        int chunkIndex = 0;
        var buffer = new List<DataSetResponseDto>();

        await foreach (var item in _dataSetService.GetAsync(request, ct))
        {
            buffer.Add(item);
            if (buffer.Count >= 100)
            {
                // Intermediate chunk — pushed immediately
                _messenger.Publish(TypeEnum.Stream,
                    new StreamChunk<DataSetResponseDto>("datasets.get", chunkIndex, buffer, false),
                    "datasets.get");
                chunkIndex++;
                buffer.Clear();
            }
        }
        // Final chunk returned from the handler → sent by ReceiveMessageAsync
        return new StreamChunk<DataSetResponseDto>("datasets.get", chunkIndex, buffer, true);
    });
```

**`StreamChunk<T>` shape**

| Field | Description |
|---|---|
| `name` | Stream channel name |
| `chunkIndex` | Zero-based sequence number |
| `items` | Array of records in this chunk |
| `isLastChunk` | `true` on the final message; triggers `completed()` and removes the handler |

---

### 3. Event

Fire-and-forget from C# to TypeScript. No correlation, no reply expected. Used for real-time progress notifications.

```
C#                                     TypeScript
──────────────────────────────────────────────────────
_messenger.Publish(TypeEnum.Event,
    progressPayload,
    "csv.upload.progress")
  → WebMessage { type: Event,
                 name: "csv.upload.progress",
                 payload: ImportProgress }
                                bus.subscribe("csv.upload.progress", handler)
                                  → handler({ percentComplete, currentStatus, ... })
```

**TypeScript side** — `WebViewMessenger.subscribe<T>()`

```typescript
const unsubscribe = bus.subscribe<ImportProgress>(
    "csv.upload.progress",
    (msg) => {
        console.log(`${msg.percentComplete}% — ${msg.currentStatus}`);
    }
);

// Later, to clean up:
unsubscribe();
```

- Multiple handlers can be registered for the same event name.
- `subscribe()` returns an unsubscribe function that removes only that handler.

**C# side** — `IWebViewMessenger.Publish()`

```csharp
Progress<ImportProgress> progress = new Progress<ImportProgress>(p =>
{
    _messenger.Publish(TypeEnum.Event, p, "csv.upload.progress");
});

await _csvImportService.ImportCsvAsync(filePath, progress, cancellationToken);
```

---

## Message flow summary

| Mode | Initiator | Reply | TS API | C# API |
|---|---|---|---|---|
| Request | TypeScript | Single response | `bus.request<T>(name, payload)` | `RegisterHandler<TReq, TRes>(name, handler)` |
| Stream | TypeScript | N chunks + final | `bus.subscribeStream(subscription, payload)` | `RegisterHandler` + manual `Publish(Stream, ...)` |
| Event | C# | None | `bus.subscribe(name, handler)` | `messenger.Publish(Event, payload, name)` |

---

## Running the application

### Development

```bash
# 1. Start the Vue dev server
cd quality-control-vue-dashboard
npm install
npm run dev          # → http://localhost:5173

# 2. Start the WPF host
# Open QualityControlDashboard.sln in Visual Studio
# Set QualityControl.WPF as start-up project and run (F5)
# MainWindow navigates Browser.Source to http://localhost:5173
```

### Production

```bash
# Build the Vue app to dist/
cd quality-control-vue-dashboard
npm run build

# In MainWindow.xaml.cs, switch the source to the dist folder:
# Browser.Source = new Uri(distIndexPath);
```

### Regenerating TypeScript DTOs

```bash
cd QualityControl.WPF
typegen.bat          # runs: typegen generate
# Outputs auto-generated .ts files to ../quality-control-vue-dashboard/src/models/
```

---

## Technology stack

| Layer | Technology |
|---|---|
| Desktop host | WPF (.NET), Microsoft WebView2 |
| Browser engine | Chromium (embedded via WebView2) |
| Front-end framework | Vue 3 (Composition API, `<script setup>`) |
| Build tool | Vite |
| Messaging bridge | `window.chrome.webview` (WebView2 JS API) |
| DTO code generation | TypeGen |
| Database | SQLite via Entity Framework Core |
