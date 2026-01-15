# Copilot / AI Agent Instructions for HMDClient

Purpose: concise guidance for an AI coding agent to be immediately productive in this Unity Magic Leap 2 client (NOODLES) repository.

- **Project type & intent:** Unity client for FLC Solar Lab (Magic Leap 2). Acts as a NOODLES protocol client that receives CBOR-over-WebSocket messages and populates the scene. See `Assets/NOODLES/NOODLESRoot.cs` (primary entrypoint).

- **Big picture architecture:**
  - Networking: WebSocket client lives in `NOODLESRoot` (field `ws`, `serverURI` default `ws://localhost:50000`). Incoming messages are CBOR arrays processed by `ReadMessagesTask` which enqueues to `incoming_messages`; `ParseMessageArray` runs on the main thread to mutate Unity objects.
  - Protocol: NOODLES component model (see `ComponentType` enum). Component messages with id <= 30 are dispatched to `ComponentPack.Handle(...)`. Non-component messages handled by `HandleNonComponentMessage` in the same file.
  - Data handling: CBOR via `PeterO.Cbor` (library referenced under `Assets/Packages/PeterO.Cbor.4.5.3`). Large asset/URL fetching is handled by `BufferCache` and async HTTP fetches in `NOODLESRoot`.
  - Threading model / conventions: background tasks perform network IO and HTTP fetches; all scene mutations and Unity API calls must happen on the main thread (the code enqueues messages and processes them in main-thread Update/Parse flow). Follow existing pattern: never call Unity API from background tasks.

- **Key files and symbols to reference:**
  - `Assets/NOODLES/NOODLESRoot.cs` — single-file hub: networking, parsing, `BufferCache`, `NooID` struct, invoke helpers, and component interaction.
  - `Assets/NOODLES/` — other NOODLES helpers and component implementations (inspect for `ComponentPack` related classes).
  - `ThirdParty.Bunny83.simpleJson.csproj` & `Assets/Packages/PeterO.Cbor.4.5.3` — third-party libs used; prefer existing assemblies rather than adding new versions.

- **Build / run basics (discoverable):**
  - This is a Unity project. Use the Unity Editor to open the project root. Scenes live under `Assets/Scenes` and `Assets/Test.unity` is present.
  - For automated/headless builds use Unity's CLI (`Unity -batchmode -projectPath <repo-path> -executeMethod <buildMethod>`). Do not assume a specific Unity version — verify with the developer or Unity Hub installed versions.

- **Patterns & idioms to preserve:**
  - Message flow: background read -> enqueue CBOR -> main-thread ParseMessageArray -> dispatch to `ComponentPack` or `HandleNonComponentMessage`.
  - Buffer caching: use `BufferCache` and `BufferCacheGet`/`BufferCacheRelease` helpers rather than direct HTTP downloads to avoid duplicate fetches / memory leaks.
  - Message replies: `invoke_method` and `invoke_method_by_name` use a `message_response` dictionary keyed by GUIDs — preserve this reply mapping when adding RPC-style messages.
  - Error handling: code logs exceptions with `Debug.LogException` and includes CBOR key previews — follow same style.

- **When changing code, pay attention to:**
  - Thread-safety: use the existing `ConcurrentQueue`, `SemaphoreSlim`, and `CancellationTokenSource` patterns. Avoid calling Unity API from background tasks.
  - Resource lifetime: `OnDestroy` cancels tasks and disposes sockets; ensure new async work is cancelled/disposed similarly.
  - CBOR formats: messages expect specific CBOR arrays and maps — inspect `ParseMessageArray` and `NooID.FromCBOR` for exact expectations.

- **Examples from codebase:**
  - Sending a message: `outgoing_messages.Enqueue(CBORObject.NewArray().Add(message.MessageId()).Add(message.ToCBOR())); writeSig.Release();`
  - Fetch helper: `FetchAsync(uri)` with limited `httpSlots` concurrency and retry logic.

- **What an AI agent should not change without confirmation:**
  - Unity project settings, package versions, and Magic Leap XR configuration — these affect developer environment and hardware testing.
  - Network protocol semantics (CBOR schema, message ids) — these are part of an external contract.

If any of these areas are unclear or you want the file to include build commands for a specific Unity version, tell me the Unity version and any preferred CI steps and I'll iterate.
