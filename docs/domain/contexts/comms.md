# Communications Context

> **Source**: Designed for v2 — see docs/product/modules/comms.md and docs/scenarios/communications.md
> **Status**: Draft
> **Last Updated**: 2026-02-18

---

## 9.1 Design Principles

1. **Comms owns channels and messages, not their external representations** — This context is responsible for storing normalized, channel-agnostic message records and managing channel lifecycle (connect, health, disconnect). It does NOT own raw platform payloads, OAuth flows, or bot protocol details — those belong to the infrastructure Anti-Corruption Layer.
2. **Thread is the consistency boundary for user interaction** — A user acts on a `Thread` (mark read, snooze, link, reply). Individual `Message` entities within a thread are immutable records of what arrived. Only `Thread` exposes mutating methods; `Message` is append-only.
3. **Channel credentials are encrypted at rest, never exposed on domain objects** — `ChannelCredential` stores encrypted tokens as shadow properties managed by the repository, mirroring the pattern from the Identity context. The domain `Channel` entity holds only a `CredentialStatus` flag — it never holds raw tokens.
4. **Cross-module linking is a reference, not a relationship** — When a thread is linked to a task, project, or person, the Comms context stores only the foreign IDs (`TaskId`, `ProjectId`, `PersonId`). It does not load or validate those entities — that is the responsibility of the application layer coordinating cross-context workflows.
5. **AI priority suggestion is advisory, not authoritative** — `AiSuggestedPriority` is a value surfaced on a `Thread` for display. The authoritative `Priority` field is always the user's explicit setting. The context never silently elevates or demotes user-assigned priorities based on AI output.

---

## 9.2 Entities

### Channel (Aggregate Root)

```
Channel
├── Id: ChannelId (value object)
├── OwnerId: UserId (from Identity context)
├── Type: ChannelType (Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub)
├── DisplayName: ChannelDisplayName (value object — user-assigned label)
├── Status: ChannelStatus (Disconnected, Connected, Degraded, Error)
├── FilterConfig: ChannelFilterConfig (value object — notification and sync preferences)
├── LastSyncedAt: DateTimeOffset?
├── LastHealthCheckAt: DateTimeOffset?
├── ErrorMessage: string? (last error from health check or sync, max 500 chars)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Shadow Properties (via IChannelRepository):
│   ├── EncryptedCredential: string (AES-256-GCM ciphertext of raw token/OAuth JSON)
│   └── CredentialIv: string (per-record initialization vector)
│
├── Methods:
│   ├── Connect(credential: ChannelCredentialInput) -> ChannelConnectedEvent
│   │       (transitions Status from Disconnected to Connected; credential stored by repository)
│   ├── Disconnect() -> ChannelDisconnectedEvent
│   │       (transitions Status to Disconnected; credential cleared by repository)
│   ├── RecordSyncSuccess(syncedAt: DateTimeOffset) -> (updates LastSyncedAt; no event)
│   ├── RecordHealthDegradation(reason: string) -> ChannelHealthDegradedEvent
│   │       (transitions Status from Connected to Degraded; stores ErrorMessage)
│   ├── RecordHealthRecovery() -> ChannelHealthRestoredEvent
│   │       (transitions Status from Degraded/Error to Connected; clears ErrorMessage)
│   ├── UpdateFilterConfig(config: ChannelFilterConfig) -> ChannelFilterUpdatedEvent
│   └── UpdateDisplayName(name: ChannelDisplayName) -> ChannelDisplayNameUpdatedEvent
│
└── Invariants:
    ├── DisplayName must be 1-100 characters
    ├── A user may have at most one connected Channel per ChannelType (no duplicate Gmail/Discord etc.)
    ├── Status must be Connected before the channel can sync messages
    ├── Connect() may only be called when Status is Disconnected or Error
    ├── Disconnect() may only be called when Status is Connected or Degraded
    ├── ErrorMessage must not exceed 500 characters
    └── Encrypted credential is managed exclusively by IChannelRepository; domain never reads it
```

### Thread (Aggregate Root)

```
Thread
├── Id: ThreadId (value object)
├── ChannelId: ChannelId (owning channel)
├── OwnerId: UserId (from Identity context)
├── ExternalId: ExternalThreadId (value object — platform-native thread/conversation ID)
├── Subject: ThreadSubject? (value object — email subject or conversation name, optional)
├── LastMessageAt: DateTimeOffset (updated on each new message)
├── LastMessagePreview: MessagePreview (value object — truncated preview of last message body)
├── SenderHandle: SenderHandle (value object — display name/username of the most recent sender)
├── IsRead: bool
├── IsReplied: bool
├── Priority: MessagePriority (None, Low, Medium, High, Critical — user-assigned)
├── AiSuggestedPriority: MessagePriority? (advisory only; null if AI has not scored it)
├── SnoozeUntil: DateTimeOffset? (null when not snoozed)
├── Messages: IReadOnlyList<Message>
├── Links: IReadOnlyList<CommLink> (value objects — cross-context references)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Receive(message: Message) -> MessageReceivedEvent
│   │       (appends Message; updates LastMessageAt, LastMessagePreview, SenderHandle;
│   │        sets IsRead = false if sender is external)
│   ├── MarkRead() -> ThreadReadEvent
│   │       (sets IsRead = true; no-op if already read)
│   ├── MarkUnread() -> ThreadMarkedUnreadEvent
│   │       (sets IsRead = false)
│   ├── SetPriority(priority: MessagePriority) -> MessagePrioritySetEvent
│   │       (sets Priority; always authoritative over AiSuggestedPriority)
│   ├── ApplyAiPrioritySuggestion(priority: MessagePriority) -> (updates AiSuggestedPriority only; no event)
│   ├── Snooze(until: DateTimeOffset) -> ThreadSnoozedEvent
│   │       (sets SnoozeUntil; thread hidden from inbox until that time)
│   ├── Unsnooze() -> ThreadUnsnoozedEvent
│   │       (clears SnoozeUntil; thread reappears in inbox)
│   ├── SendReply(body: MessageBody, outboundMessageId: MessageId) -> ThreadRepliedEvent
│   │       (appends an outbound Message; sets IsReplied = true)
│   ├── LinkToTask(taskId: TaskId) -> ThreadLinkedToTaskEvent
│   │       (adds CommLink of type Task; idempotent — no duplicate links)
│   ├── LinkToProject(projectId: ProjectId) -> ThreadLinkedToProjectEvent
│   │       (adds CommLink of type Project; idempotent)
│   ├── LinkToPerson(personId: PersonId) -> ThreadLinkedToPersonEvent
│   │       (adds CommLink of type Person; idempotent)
│   └── UnlinkFrom(linkId: CommLinkId) -> ThreadUnlinkedEvent
│           (removes a CommLink by ID)
│
└── Invariants:
    ├── ExternalId must be unique per ChannelId (no duplicate threads from same platform)
    ├── Messages list is append-only — individual messages are never mutated or deleted
    ├── SnoozeUntil, when set, must be a future timestamp at the time of the Snooze() call
    ├── Links are unique per (LinkType, LinkedId) — no duplicate task/project/person links on same thread
    ├── A Thread must belong to a valid Channel owned by the same OwnerId
    ├── SendReply() requires IsRead = true (cannot reply to an unread thread — enforced at application layer)
    └── AiSuggestedPriority never overrides the user-assigned Priority field
```

### Message (Entity, owned by Thread)

```
Message
├── Id: MessageId (value object)
├── ThreadId: ThreadId (owning thread)
├── ExternalId: ExternalMessageId (value object — platform-native message ID)
├── Direction: MessageDirection (Inbound, Outbound)
├── SenderHandle: SenderHandle (value object)
├── Body: MessageBody (value object — normalized plain-text/markdown body)
├── ReceivedAt: DateTimeOffset (when the message arrived or was sent)
├── IsRead: bool
│
└── Invariants:
    ├── ExternalId must be unique per ThreadId (no duplicate messages in a thread)
    ├── Body must be 1-50000 characters
    ├── Direction is immutable after creation
    └── ReceivedAt is immutable after creation
```

---

## 9.3 Value Objects

```
ChannelId               -> Guid wrapper
ThreadId                -> Guid wrapper
MessageId               -> Guid wrapper
CommLinkId              -> Guid wrapper

ChannelType             -> Enum: Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub
ChannelStatus           -> Enum: Disconnected, Connected, Degraded, Error
MessagePriority         -> Enum: None, Low, Medium, High, Critical
MessageDirection        -> Enum: Inbound, Outbound
CommLinkType            -> Enum: Task, Project, Person

ChannelDisplayName      -> Non-empty string, 1-100 chars, trimmed
ExternalThreadId        -> Non-empty string, 1-500 chars (platform-native ID, opaque)
ExternalMessageId       -> Non-empty string, 1-500 chars (platform-native ID, opaque)
ThreadSubject           -> String, 0-1000 chars, trimmed (email subject or conversation title)
MessagePreview          -> Non-empty string, 1-200 chars, trimmed
                           (truncated body of the last inbound message)
SenderHandle            -> Non-empty string, 1-200 chars, trimmed
                           (display name or @username of the sender)
MessageBody             -> Non-empty string, 1-50000 chars
                           (normalized body; raw platform payloads are never stored here)

ChannelFilterConfig     -> Immutable record:
                           { NotificationMode: NotificationMode, Keywords: IReadOnlyList<string>,
                             MonitoredSubChannels: IReadOnlyList<string> }
                           (NotificationMode: Enum: All, MentionsOnly, KeywordsOnly)
                           (Keywords: each 1-50 chars, max 50 keywords)
                           (MonitoredSubChannels: Discord server channels, Slack channels, etc.)

CommLink                -> Immutable record: { Id: CommLinkId, Type: CommLinkType, LinkedId: Guid }
                           (LinkedId references TaskId, ProjectId, or PersonId depending on Type)

ChannelCredentialInput  -> Transient value, never persisted on domain entity:
                           { CredentialType: CredentialType, TokenJson: string }
                           (CredentialType: Enum: OAuthToken, BotToken, ApiKey)
                           (Used only at connect time; immediately handed to repository for encryption)
```

---

## 9.4 Domain Events

```
ChannelConnectedEvent           { ChannelId, OwnerId, ChannelType }
ChannelDisconnectedEvent        { ChannelId, OwnerId, ChannelType }
ChannelHealthDegradedEvent      { ChannelId, OwnerId, ChannelType, Reason }
ChannelHealthRestoredEvent      { ChannelId, OwnerId, ChannelType }
ChannelFilterUpdatedEvent       { ChannelId, OwnerId, ChannelType }
ChannelDisplayNameUpdatedEvent  { ChannelId, OwnerId, NewDisplayName }

MessageReceivedEvent            { ThreadId, MessageId, ChannelId, ChannelType, OwnerId,
                                  SenderHandle, ReceivedAt, AiSuggestedPriority? }
                                (No body — consumers load message via repository if needed)
MessageSentEvent                { ThreadId, MessageId, ChannelId, ChannelType, OwnerId, SentAt }

ThreadReadEvent                 { ThreadId, ChannelId, OwnerId }
ThreadMarkedUnreadEvent         { ThreadId, ChannelId, OwnerId }
MessagePrioritySetEvent         { ThreadId, ChannelId, OwnerId, OldPriority, NewPriority }
ThreadSnoozedEvent              { ThreadId, ChannelId, OwnerId, SnoozeUntil }
ThreadUnsnoozedEvent            { ThreadId, ChannelId, OwnerId }

ThreadLinkedToTaskEvent         { ThreadId, ChannelId, OwnerId, TaskId }
ThreadLinkedToProjectEvent      { ThreadId, ChannelId, OwnerId, ProjectId }
ThreadLinkedToPersonEvent       { ThreadId, ChannelId, OwnerId, PersonId }
ThreadUnlinkedEvent             { ThreadId, CommLinkId, LinkType, LinkedId, OwnerId }
ThreadRepliedEvent              { ThreadId, ChannelId, OwnerId, OutboundMessageId }
```

---

## 9.5 Use Cases

```
Commands:
├── ConnectChannelCommand           { ChannelType, DisplayName, Credential: ChannelCredentialInput,
│                                     FilterConfig: ChannelFilterConfig }
│       -> Creates a Channel aggregate; stores encrypted credential via repository;
│          invokes IChannelAdapterFactory to verify the credential is valid before persisting;
│          raises ChannelConnectedEvent
├── DisconnectChannelCommand        { ChannelId }
│       -> Calls channel.Disconnect(); credential cleared by repository;
│          raises ChannelDisconnectedEvent
├── UpdateChannelFilterCommand      { ChannelId, FilterConfig: ChannelFilterConfig }
│       -> Updates ChannelFilterConfig on the Channel aggregate;
│          raises ChannelFilterUpdatedEvent; sync adapter respects new filter on next pull
├── UpdateChannelDisplayNameCommand { ChannelId, DisplayName }
│       -> Updates ChannelDisplayName; raises ChannelDisplayNameUpdatedEvent
├── IngestMessageCommand            { ChannelId, ExternalThreadId, ExternalMessageId,
│                                     SenderHandle, Body, ReceivedAt, Direction }
│       -> Idempotent (deduplicates by ExternalMessageId per thread);
│          creates Thread if it does not exist yet;
│          calls thread.Receive(message); runs AI prioritization if configured;
│          raises MessageReceivedEvent; notifies Notification context if priority >= High
├── SendReplyCommand                { ThreadId, Body }
│       -> Loads Thread and Channel; dispatches outbound message via IChannelAdapter;
│          calls thread.SendReply(); raises ThreadRepliedEvent;
│          raises MessageSentEvent; marks thread as read
├── MarkThreadReadCommand           { ThreadId }
│       -> Calls thread.MarkRead(); raises ThreadReadEvent
├── MarkThreadUnreadCommand         { ThreadId }
│       -> Calls thread.MarkUnread(); raises ThreadMarkedUnreadEvent
├── SetThreadPriorityCommand        { ThreadId, Priority: MessagePriority }
│       -> Calls thread.SetPriority(); raises MessagePrioritySetEvent
├── SnoozeThreadCommand             { ThreadId, SnoozeUntil: DateTimeOffset }
│       -> Calls thread.Snooze(); raises ThreadSnoozedEvent;
│          a background job is responsible for unsnoozing at the target time
├── UnsnoozeThreadCommand           { ThreadId }
│       -> Calls thread.Unsnooze(); raises ThreadUnsnoozedEvent
├── LinkThreadToTaskCommand         { ThreadId, TaskId }
│       -> Calls thread.LinkToTask(taskId); raises ThreadLinkedToTaskEvent;
│          [cross-context: Tasks] application layer verifies TaskId exists in Task context
│          before committing the link
├── LinkThreadToProjectCommand      { ThreadId, ProjectId }
│       -> Calls thread.LinkToProject(projectId); raises ThreadLinkedToProjectEvent;
│          [cross-context: Projects] application layer verifies ProjectId exists
├── LinkThreadToPersonCommand       { ThreadId, PersonId }
│       -> Calls thread.LinkToPerson(personId); raises ThreadLinkedToPersonEvent;
│          [cross-context: People] application layer verifies PersonId exists
├── UnlinkThreadCommand             { ThreadId, CommLinkId }
│       -> Calls thread.UnlinkFrom(commLinkId); raises ThreadUnlinkedEvent
└── ConvertThreadToTaskCommand      { ThreadId, Title?: string, ProjectId?: ProjectId,
                                      Priority?: Priority, DueDate?: DateTimeOffset }
        -> Application layer use case that spans Comms + Tasks:
           1. Creates task via Task context (CreateTaskCommand) — title defaults to ThreadSubject
           2. Links thread to the new task (LinkThreadToTaskCommand)
           3. Pre-sets project if provided — links task to ProjectId
           Raises both ThreadLinkedToTaskEvent and (via Task context) TaskCreatedEvent

Queries:
├── GetUnifiedInboxQuery            { Page, PageSize, ChannelType[]?, Priority?,
│                                     IsRead?, IsReplied?, IsSnoozed?,
│                                     SortBy?: (Chronological | Priority),
│                                     DateFrom?, DateTo? }
│                                     -> PagedResult<ThreadSummaryDto>
│       (ThreadSummaryDto includes: channel icon info, sender, subject/preview,
│        lastMessageAt, isRead, isReplied, priority, aiSuggestedPriority, snoozeUntil, links[])
├── GetThreadByIdQuery              { ThreadId } -> ThreadDetailDto
│       (includes all Messages, full body, CommLinks, and People match if auto-linked)
├── SearchMessagesQuery             { Query: string, ChannelType[]?, DateFrom?, DateTo?,
│                                     Page, PageSize }
│                                     -> PagedResult<MessageSearchResultDto>
│       (full-text search across Message.Body and Thread.Subject; highlights matched terms)
├── GetChannelsQuery                {} -> IReadOnlyList<ChannelDto>
│       (returns all channels for the current user with status and last sync time)
├── GetChannelByIdQuery             { ChannelId } -> ChannelDto
└── GetUnreadCountsQuery            {} -> UnreadCountsDto
        (per-channel unread thread counts, total unread, snoozed count)
```

---

## 9.6 Repository Interfaces

```csharp
/// <summary>
/// Repository for the Channel aggregate. Manages encrypted credential storage as shadow properties.
/// Credential plaintext is NEVER exposed on the Channel domain entity.
/// </summary>
public interface IChannelRepository
{
    /// <summary>Loads channel by ID. Returns null if not found.</summary>
    Task<Channel?> GetByIdAsync(ChannelId id, CancellationToken ct);

    /// <summary>
    /// Returns all channels owned by the specified user.
    /// </summary>
    Task<IReadOnlyList<Channel>> ListByOwnerAsync(UserId ownerId, CancellationToken ct);

    /// <summary>
    /// Returns the channel of the given type owned by the user, or null if not connected.
    /// Used to enforce the one-channel-per-type invariant before Connect().
    /// </summary>
    Task<Channel?> FindByOwnerAndTypeAsync(
        UserId ownerId, ChannelType type, CancellationToken ct);

    /// <summary>
    /// Persists a new channel with its encrypted credential shadow property.
    /// Repository computes encryption (AES-256-GCM) and stores ciphertext + IV as shadow fields.
    /// </summary>
    Task AddAsync(Channel channel, ChannelCredentialInput credential, CancellationToken ct);

    /// <summary>Updates channel status, filter config, health fields, and display name.</summary>
    Task UpdateAsync(Channel channel, CancellationToken ct);

    /// <summary>
    /// Clears the encrypted credential shadow property for the given channel (on disconnect).
    /// </summary>
    Task ClearCredentialAsync(ChannelId id, CancellationToken ct);

    /// <summary>
    /// Returns the decrypted credential for use by the adapter layer.
    /// This is the ONLY authorized path for reading channel credentials.
    /// Callers are exclusively IChannelAdapter implementations in the infrastructure layer.
    /// </summary>
    Task<Result<ChannelCredentialInput, DomainError>> GetDecryptedCredentialAsync(
        ChannelId id, CancellationToken ct);
}

/// <summary>
/// Repository for the Thread aggregate and its owned Messages.
/// Message ingestion is idempotent — duplicate ExternalMessageIds are ignored.
/// </summary>
public interface IThreadRepository
{
    /// <summary>Loads thread by internal ID with all messages. Returns null if not found.</summary>
    Task<Thread?> GetByIdAsync(ThreadId id, CancellationToken ct);

    /// <summary>
    /// Loads thread by external platform ID and channel. Returns null if not yet ingested.
    /// Used during message ingestion to detect whether to create or update the thread.
    /// </summary>
    Task<Thread?> FindByExternalIdAsync(
        ChannelId channelId, ExternalThreadId externalId, CancellationToken ct);

    /// <summary>
    /// Returns a paginated list of thread summaries for the unified inbox.
    /// Applies filter predicates: channel types, priority, read state, replied state,
    /// snoozed state, date range. Excludes threads where SnoozeUntil is in the future
    /// unless IsSnoozed filter is explicitly true.
    /// </summary>
    Task<PagedResult<Thread>> ListAsync(
        UserId ownerId,
        IReadOnlyList<ChannelType>? channelTypes,
        MessagePriority? priority,
        bool? isRead,
        bool? isReplied,
        bool? isSnoozed,
        InboxSortOrder sortOrder,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Full-text search across message bodies and thread subjects owned by the user.
    /// Returns matching message snippets with highlighted terms and their parent threads.
    /// </summary>
    Task<PagedResult<MessageSearchResult>> SearchAsync(
        UserId ownerId,
        string query,
        IReadOnlyList<ChannelType>? channelTypes,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Returns unread thread counts per channel and global total for the user.</summary>
    Task<UnreadCounts> GetUnreadCountsAsync(UserId ownerId, CancellationToken ct);

    /// <summary>Persists a new thread with its initial message.</summary>
    Task AddAsync(Thread thread, CancellationToken ct);

    /// <summary>
    /// Persists state changes to an existing thread (read state, priority, snooze,
    /// new messages appended, links added or removed, replied state).
    /// </summary>
    Task UpdateAsync(Thread thread, CancellationToken ct);

    /// <summary>
    /// Returns all threads owned by the user that are linked to a given TaskId, ProjectId, or PersonId.
    /// Used by cross-module queries (e.g., "show communications on a project page").
    /// </summary>
    Task<IReadOnlyList<Thread>> ListByLinkAsync(
        UserId ownerId, CommLinkType linkType, Guid linkedId, CancellationToken ct);
}
```

---

## 9.7 API Endpoints

```
Channels:
GET    /api/comms/channels                      List all connected channels for current user  [Auth]
POST   /api/comms/channels                      Connect a new channel (supply credentials)   [Auth]
GET    /api/comms/channels/{id}                 Get channel details and status                [Auth]
PUT    /api/comms/channels/{id}/filter          Update filter config for a channel            [Auth]
PUT    /api/comms/channels/{id}/display-name    Rename a channel                             [Auth]
DELETE /api/comms/channels/{id}                 Disconnect and remove channel                [Auth]

Inbox:
GET    /api/comms/inbox                         Get unified inbox (paginated, filterable)    [Auth]
                                                ?channelTypes=Gmail,Slack
                                                &priority=High
                                                &isRead=false
                                                &isSnoozed=false
                                                &sortBy=Chronological
                                                &dateFrom=2026-01-01
                                                &page=1&pageSize=25
GET    /api/comms/inbox/unread-counts           Per-channel unread counts + total            [Auth]

Threads:
GET    /api/comms/threads/{id}                  Get thread with all messages and links       [Auth]
POST   /api/comms/threads/{id}/read             Mark thread as read                          [Auth]
POST   /api/comms/threads/{id}/unread           Mark thread as unread                        [Auth]
POST   /api/comms/threads/{id}/priority         Set thread priority                          [Auth]
POST   /api/comms/threads/{id}/snooze           Snooze thread until a given time             [Auth]
POST   /api/comms/threads/{id}/unsnooze         Unsnooze a thread immediately                [Auth]
POST   /api/comms/threads/{id}/reply            Send a reply via the thread's channel        [Auth]
POST   /api/comms/threads/{id}/links            Add a link (to task, project, or person)     [Auth]
DELETE /api/comms/threads/{id}/links/{linkId}   Remove a link from the thread                [Auth]
POST   /api/comms/threads/{id}/convert-to-task  Create task from thread content              [Auth]

Search:
GET    /api/comms/search                        Full-text search across all channels         [Auth]
                                                ?q=AWS+cost&channelTypes=Slack&page=1
```

---

## 9.8 Anti-Corruption Layer (IChannelAdapter)

Each external platform (Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub) has a distinct adapter in the infrastructure layer. The ACL translates platform-native messages into the normalized `IngestMessageCommand` and dispatches outbound replies from `SendReplyCommand`.

```csharp
/// <summary>
/// Port for channel-specific communication platform integrations.
/// Implementations live in the infrastructure layer and handle OAuth refresh,
/// rate limiting, retry, and platform-specific payload mapping.
/// Domain and application layers NEVER call platform APIs directly.
/// </summary>
public interface IChannelAdapter
{
    /// <summary>
    /// The channel type this adapter handles. Used by IChannelAdapterFactory for dispatch.
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Verifies that the supplied credential is valid against the external platform.
    /// Called during ConnectChannelCommand before the channel is persisted.
    /// Returns a DomainError if the credential is rejected or unreachable.
    /// </summary>
    Task<Result<DomainError>> VerifyCredentialAsync(
        ChannelCredentialInput credential, CancellationToken ct);

    /// <summary>
    /// Fetches new messages from the external platform since the given cursor.
    /// Returns a list of normalized IngestMessageCommands to be dispatched to the application layer.
    /// Implementations handle deduplication at the platform level (e.g., Gmail history IDs).
    /// </summary>
    Task<IReadOnlyList<IngestMessageCommand>> FetchNewMessagesAsync(
        ChannelId channelId, ChannelCredentialInput credential,
        DateTimeOffset? since, CancellationToken ct);

    /// <summary>
    /// Dispatches an outbound reply via the external platform.
    /// Called by SendReplyCommand handler after the Thread aggregate is updated.
    /// </summary>
    Task<Result<ExternalMessageId, DomainError>> SendReplyAsync(
        ChannelCredentialInput credential,
        ExternalThreadId externalThreadId,
        string body,
        CancellationToken ct);
}

/// <summary>
/// Resolves the correct IChannelAdapter for a given ChannelType.
/// Registered in DI; all adapters are registered at startup.
/// </summary>
public interface IChannelAdapterFactory
{
    IChannelAdapter GetAdapter(ChannelType channelType);
}
```

---

## 9.9 Application Layer Coordination

The Communications context coordinates with three other v2 contexts at the application layer. No direct domain coupling exists — contexts communicate via IDs and events only.

| Operation | Comms Context | Other Context |
|-----------|---------------|---------------|
| **Convert thread to task** | `thread.LinkToTask(taskId)` → `ThreadLinkedToTaskEvent` | Task context: `CreateTaskCommand` (pre-filled from thread subject + priority) |
| **Auto-link sender to person** | On `MessageReceivedEvent`, application handler queries People context for a match on `SenderHandle` (email address or username) | People context: `FindPersonByContactHandleQuery` → if found, `LinkThreadToPersonCommand` |
| **Auto-link to project** | On `MessageReceivedEvent`, application handler queries Projects context using inferred domain/keyword from thread | Projects context: `FindProjectByKeywordQuery` → if found, `LinkThreadToProjectCommand` |
| **High-priority notification** | On `MessageReceivedEvent` where resolved priority >= High, raises event consumed by Notification context | Notification context: `UserNotificationCreatedEvent` → desktop/push notification |
| **Snooze expiry** | A background scheduler reads snoozed threads and dispatches `UnsnoozeThreadCommand` when `SnoozeUntil` is reached | No cross-context involvement — handled within Comms context |
| **Cross-module inbox on Projects page** | Projects page queries `IThreadRepository.ListByLinkAsync(userId, CommLinkType.Project, projectId)` | Read-only; no write coordination |
| **Cross-module timeline on People page** | People page queries `IThreadRepository.ListByLinkAsync(userId, CommLinkType.Person, personId)` | Read-only; no write coordination |

---

## Design Notes

| Item | Type | Detail |
|------|------|--------|
| CM-005 (LinkedIn) | Deferred | LinkedIn has no official messaging API; scenario file marks it as P2 and explicitly notes "open API question". No domain concept designed for LinkedIn-specific behavior — the `ChannelType.LinkedIn` enum value is reserved but the adapter is out of scope for the initial v2 build. |
| CM-014 (AI priority) | Advisory only | AI categorization and priority suggestion is surfaced as `AiSuggestedPriority` on `Thread` but never overwrites the user-assigned `Priority`. The prioritization logic (ML model, keyword matching, sender rules) is an infrastructure concern owned by an `IAiPrioritizationService` port — not modeled in this context document as it is implementation-specific. |
| Snooze expiry | Infrastructure concern | `UnsnoozeThreadCommand` must be dispatched by a background job (Hangfire, Quartz, or Aspire worker) when `SnoozeUntil` is reached. The scheduling mechanism is not modeled in the domain — only the command and event are defined here. |
| Message body storage | Scale consideration | Storing full message bodies for 50,000 characters per message at scale may require a separate blob/document store rather than a relational column. This is an infrastructure decision; the domain models `MessageBody` as a value object with no storage opinion. |
| CM-008 (search) | Full-text indexing | `SearchMessagesQuery` requires full-text indexing on `Message.Body` and `Thread.Subject`. The implementation (SQLite FTS5 for dev, SQL Server Full-Text Search or Elasticsearch for production) is an infrastructure decision. The repository interface `SearchAsync` is the abstraction boundary. |
