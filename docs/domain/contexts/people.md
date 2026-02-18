# People Context

> **Source**: Designed for v2 — see docs/product/modules/people.md and docs/scenarios/people-management.md
> **Status**: Draft
> **Last Updated**: 2026-02-18

---

## 8.1 Design Principles

1. **People context owns relationship knowledge, not communication infrastructure** — This context owns person and company records, notes, preferences, important dates, tags, and project links. It does NOT own messages, email threads, or communication channels; those belong to the Comms context. Cross-module timeline aggregation is assembled at the application layer by consuming Comms-published data.

2. **Person and Company are independent aggregate roots** — A Company does not contain Person objects. The link between a person and a company is represented as a `CompanyMembership` value object on the Person aggregate. This keeps both aggregates autonomously writable and avoids loading entire company rosters when editing a person record.

3. **Notes are owned by their subject aggregate** — A note belongs to either a `Person` or a `Company`. Notes attached to a person are fetched via `IPersonRepository`; notes attached to a company via `ICompanyRepository`. Notes are first-class entities with their own identity so they can be independently added, edited, or deleted without replacing the parent aggregate.

4. **Context filter (Professional / Personal) is a query-side concern** — The `InformationContext` field on notes and preferences records which context each piece of data belongs to. Filtering by context is applied at the query layer and never enforced as a domain invariant. The aggregate root never restricts what can be stored; it only stores the context label.

5. **This context uses `UserId` from Identity and `ProjectId` from Projects but knows nothing about user profiles or project internals** — All cross-module references are stored as opaque IDs. The People context is conformist to Identity (upstream) and acts as a customer to Projects (upstream). It publishes events that the Comms context subscribes to for auto-linking messages to people by email or handle.

---

## 8.2 Entities

### Person (Aggregate Root)

```
Person
├── Id: PersonId (value object)
├── OwnerId: UserId (from Identity context — the LemonDo user who owns this record)
├── Name: PersonName (value object)
├── Emails: IReadOnlyList<ContactHandle> (channel = Email)
├── Phones: IReadOnlyList<ContactHandle> (channel = Phone)
├── PhotoUrl: Uri?
├── Bio: PersonBio? (value object)
├── HowWeMet: HowWeMet? (value object)
├── RelationshipType: RelationshipType (Professional, Personal, Both)
├── Preferences: PersonPreferences? (value object)
├── Tags: IReadOnlyList<PersonTag>
├── CompanyMemberships: IReadOnlyList<CompanyMembership> (links to companies with role/dept)
├── ProjectLinks: IReadOnlyList<ProjectLink> (links to projects with role)
├── ImportantDates: IReadOnlyList<ImportantDate>
├── IsArchived: bool
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, name, emails?, phones?, photoUrl?, bio?, howWeMet?,
│   │         relationshipType, tags?) -> PersonCreatedEvent
│   │       (validates name and at least one contact handle or bio; defaults
│   │        RelationshipType to Professional)
│   ├── UpdateProfile(name?, photoUrl?, bio?, howWeMet?, relationshipType?)
│   │       -> PersonUpdatedEvent
│   ├── AddEmail(handle) -> PersonContactHandleAddedEvent
│   │       (no duplicate handles within the same channel)
│   ├── RemoveEmail(handle) -> PersonContactHandleRemovedEvent
│   ├── AddPhone(handle) -> PersonContactHandleAddedEvent
│   ├── RemovePhone(handle) -> PersonContactHandleRemovedEvent
│   ├── SetPreferences(preferences) -> PersonPreferencesUpdatedEvent
│   ├── AddTag(tag) -> PersonTagAddedEvent
│   │       (no duplicate tags)
│   ├── RemoveTag(tag) -> PersonTagRemovedEvent
│   ├── LinkToCompany(companyId, role, department?) -> PersonLinkedToCompanyEvent
│   │       (idempotent: if already linked to this company, updates role/dept)
│   ├── UnlinkFromCompany(companyId) -> PersonUnlinkedFromCompanyEvent
│   ├── LinkToProject(projectId, role) -> PersonLinkedToProjectEvent
│   │       (idempotent: if already linked to this project, updates role)
│   ├── UnlinkFromProject(projectId) -> PersonUnlinkedFromProjectEvent
│   ├── AddImportantDate(date) -> ImportantDateAddedEvent
│   ├── RemoveImportantDate(importantDateId) -> ImportantDateRemovedEvent
│   ├── Archive() -> PersonArchivedEvent
│   └── Unarchive() -> PersonUnarchivedEvent
│
└── Invariants:
    ├── Name must be 1-200 characters
    ├── Cannot have duplicate contact handles within the same channel (two identical emails)
    ├── Cannot have duplicate tags (case-insensitive)
    ├── Cannot link to the same company twice (LinkToCompany is idempotent — updates in place)
    ├── Cannot link to the same project twice (LinkToProject is idempotent — updates in place)
    ├── Cannot archive an already-archived person
    ├── Cannot unarchive a person that is not archived
    ├── OwnerId cannot change after creation
    └── At least Name must be non-empty; all other fields are optional at creation
```

### Note (Entity, owned by Person or Company aggregate)

```
Note
├── Id: NoteId (value object)
├── Content: NoteContent (value object)
├── InformationContext: InformationContext (All, Professional, Personal)
├── Tags: IReadOnlyList<NoteTag>
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
└── Invariants:
    ├── Content must be 1-10000 characters
    ├── Cannot have duplicate tags on the same note (case-insensitive)
    └── InformationContext cannot be changed after creation (context is set at capture time)
```

### Company (Aggregate Root)

```
Company
├── Id: CompanyId (value object)
├── OwnerId: UserId (from Identity context)
├── Name: CompanyName (value object)
├── Website: Uri?
├── Industry: Industry? (value object)
├── LogoUrl: Uri?
├── Tags: IReadOnlyList<CompanyTag>
├── ProjectLinks: IReadOnlyList<ProjectLink>
├── IsArchived: bool
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, name, website?, industry?, logoUrl?, tags?)
│   │       -> CompanyCreatedEvent
│   ├── UpdateProfile(name?, website?, industry?, logoUrl?)
│   │       -> CompanyUpdatedEvent
│   ├── AddTag(tag) -> CompanyTagAddedEvent
│   │       (no duplicate tags)
│   ├── RemoveTag(tag) -> CompanyTagRemovedEvent
│   ├── LinkToProject(projectId, role) -> CompanyLinkedToProjectEvent
│   │       (idempotent: if already linked to this project, updates role)
│   ├── UnlinkFromProject(projectId) -> CompanyUnlinkedFromProjectEvent
│   ├── Archive() -> CompanyArchivedEvent
│   └── Unarchive() -> CompanyUnarchivedEvent
│
└── Invariants:
    ├── Name must be 1-200 characters
    ├── Cannot have duplicate tags (case-insensitive)
    ├── Cannot link to the same project twice (LinkToProject is idempotent — updates in place)
    ├── Cannot archive an already-archived company
    ├── Cannot unarchive a company that is not archived
    └── OwnerId cannot change after creation
```

### ImportantDate (Entity, owned by Person aggregate)

```
ImportantDate
├── Id: ImportantDateId (value object)
├── Label: ImportantDateLabel (value object — e.g., "Birthday", "Work Anniversary")
├── Date: DateOnly (day and month always required; year is optional)
├── Year: int? (null if only month/day known, e.g., recurring birthday)
├── ReminderDaysBefore: int? (null = no reminder; positive int = days before)
│
└── Invariants:
    ├── Label must be 1-100 characters
    ├── ReminderDaysBefore must be > 0 if set
    └── Month and Day must form a valid calendar date
```

---

## 8.3 Value Objects

```
PersonId            -> Guid wrapper
CompanyId           -> Guid wrapper
NoteId              -> Guid wrapper
ImportantDateId     -> Guid wrapper
PersonName          -> Non-empty string, 1-200 chars, trimmed
CompanyName         -> Non-empty string, 1-200 chars, trimmed
PersonBio           -> String, 0-5000 chars (free-form background summary)
HowWeMet            -> String, 0-2000 chars (free-form first-meeting narrative)
NoteContent         -> Non-empty string, 1-10000 chars, trimmed
Industry            -> Non-empty string, 1-100 chars, trimmed (e.g., "Design Agency")
ImportantDateLabel  -> Non-empty string, 1-100 chars, trimmed (e.g., "Birthday")
ContactHandle       -> Channel (Email | Phone | Slack | WhatsApp | GitHub | LinkedIn | Discord | Other)
                       + Handle string (1-500 chars, trimmed)
                       Structural equality on Channel + Handle pair
CompanyMembership   -> CompanyId + Role (1-100 chars) + Department? (0-100 chars)
                       Structural equality on CompanyId (one record per company)
ProjectLink         -> ProjectId (Guid, opaque reference to Projects context)
                       + Role (1-100 chars)
                       Structural equality on ProjectId (one record per project)
PersonPreferences   -> Timezone (IANA string, e.g., "Europe/Lisbon", 1-50 chars)
                       + CommunicationStyle? (Async | SyncPreferred | NoPreference)
                       + PreferredTools: IReadOnlyList<string> (each 1-100 chars, max 20 tools)
PersonTag           -> Non-empty string, 1-50 chars, lowercase, trimmed
CompanyTag          -> Non-empty string, 1-50 chars, lowercase, trimmed
NoteTag             -> Non-empty string, 1-50 chars, lowercase, trimmed
RelationshipType    -> Enum: Professional, Personal, Both
InformationContext  -> Enum: All, Professional, Personal
CommunicationStyle  -> Enum: Async, SyncPreferred, NoPreference
```

---

## 8.4 Domain Events

```
PersonCreatedEvent              { PersonId, OwnerId, Name }
PersonUpdatedEvent              { PersonId, FieldName, OldValue, NewValue }
PersonContactHandleAddedEvent   { PersonId, Channel, Handle }
PersonContactHandleRemovedEvent { PersonId, Channel, Handle }
PersonPreferencesUpdatedEvent   { PersonId }
PersonTagAddedEvent             { PersonId, Tag }
PersonTagRemovedEvent           { PersonId, Tag }
PersonLinkedToCompanyEvent      { PersonId, CompanyId, Role, Department? }
PersonUnlinkedFromCompanyEvent  { PersonId, CompanyId }
PersonLinkedToProjectEvent      { PersonId, ProjectId, Role }
PersonUnlinkedFromProjectEvent  { PersonId, ProjectId }
PersonArchivedEvent             { PersonId }
PersonUnarchivedEvent           { PersonId }
NoteAddedToPersonEvent          { NoteId, PersonId, OwnerId, InformationContext }
NoteUpdatedEvent                { NoteId, SubjectType, SubjectId }
NoteDeletedEvent                { NoteId, SubjectType, SubjectId }
ImportantDateAddedEvent         { PersonId, ImportantDateId, Label, Month, Day }
ImportantDateRemovedEvent       { PersonId, ImportantDateId }
CompanyCreatedEvent             { CompanyId, OwnerId, Name }
CompanyUpdatedEvent             { CompanyId, FieldName, OldValue, NewValue }
CompanyTagAddedEvent            { CompanyId, Tag }
CompanyTagRemovedEvent          { CompanyId, Tag }
CompanyLinkedToProjectEvent     { CompanyId, ProjectId, Role }
CompanyUnlinkedFromProjectEvent { CompanyId, ProjectId }
CompanyArchivedEvent            { CompanyId }
CompanyUnarchivedEvent          { CompanyId }
NoteAddedToCompanyEvent         { NoteId, CompanyId, OwnerId, InformationContext }
```

---

## 8.5 Use Cases

```
Commands:
├── CreatePersonCommand          { Name, Emails?, Phones?, PhotoUrl?, Bio?, HowWeMet?,
│                                  RelationshipType?, Tags? }
│       → Creates Person aggregate, publishes PersonCreatedEvent.
│         Comms context subscribes to PersonCreatedEvent to register email handles
│         for auto-linking incoming messages.
├── UpdatePersonProfileCommand   { PersonId, Name?, PhotoUrl?, Bio?, HowWeMet?,
│                                  RelationshipType? }
├── AddPersonContactHandleCommand { PersonId, Channel, Handle }
│       → Adds handle; publishes PersonContactHandleAddedEvent.
│         Comms context uses this event to start auto-linking messages for the new handle.
├── RemovePersonContactHandleCommand { PersonId, Channel, Handle }
├── SetPersonPreferencesCommand  { PersonId, Timezone?, CommunicationStyle?,
│                                  PreferredTools? }
├── AddPersonTagCommand          { PersonId, Tag }
├── RemovePersonTagCommand       { PersonId, Tag }
├── LinkPersonToCompanyCommand   { PersonId, CompanyId, Role, Department? }
│       → Calls Person.LinkToCompany(); verifies CompanyId exists via
│         ICompanyRepository before linking (not a cross-context call — same module).
├── UnlinkPersonFromCompanyCommand { PersonId, CompanyId }
├── LinkPersonToProjectCommand   { PersonId, ProjectId, Role }
│       → Stores ProjectId as an opaque reference. No cross-context validation
│         at write time; ProjectId validity is verified on read.
├── UnlinkPersonFromProjectCommand { PersonId, ProjectId }
├── AddImportantDateCommand      { PersonId, Label, Month, Day, Year?, ReminderDaysBefore? }
├── RemoveImportantDateCommand   { PersonId, ImportantDateId }
├── ArchivePersonCommand         { PersonId }
├── UnarchivePersonCommand       { PersonId }
│
├── AddNoteToPersonCommand       { PersonId, Content, InformationContext, Tags? }
│       → Creates Note entity; persists via IPersonRepository.AddNoteAsync().
│         Publishes NoteAddedToPersonEvent.
├── UpdatePersonNoteCommand      { NoteId, PersonId, Content, Tags? }
├── DeletePersonNoteCommand      { NoteId, PersonId }
│
├── CreateCompanyCommand         { Name, Website?, Industry?, LogoUrl?, Tags? }
│       → Creates Company aggregate, publishes CompanyCreatedEvent.
├── UpdateCompanyProfileCommand  { CompanyId, Name?, Website?, Industry?, LogoUrl? }
├── AddCompanyTagCommand         { CompanyId, Tag }
├── RemoveCompanyTagCommand      { CompanyId, Tag }
├── LinkCompanyToProjectCommand  { CompanyId, ProjectId, Role }
├── UnlinkCompanyFromProjectCommand { CompanyId, ProjectId }
├── ArchiveCompanyCommand        { CompanyId }
├── UnarchiveCompanyCommand      { CompanyId }
│
└── AddNoteToCompanyCommand      { CompanyId, Content, InformationContext, Tags? }
        → Creates Note entity; persists via ICompanyRepository.AddNoteAsync().
          Publishes NoteAddedToCompanyEvent.
UpdateCompanyNoteCommand         { NoteId, CompanyId, Content, Tags? }
DeleteCompanyNoteCommand         { NoteId, CompanyId }

Queries:
├── GetPersonByIdQuery           { PersonId } -> PersonDetailDto
│       → Returns full person profile: handles, preferences, company memberships,
│         project links, important dates, tags. Does NOT include notes or timeline
│         (those are separate queries to keep response size manageable).
├── ListPeopleQuery              { Tags?, RelationshipType?, CompanyId?, ProjectId?,
│                                  IsArchived?, Search?, Page, PageSize }
│                                    -> PagedResult<PersonSummaryDto>
├── SearchPeopleAndCompaniesQuery { Term, Page, PageSize }
│                                    -> PagedResult<PeopleSearchResultDto>
│       → Full-text search across person names, company names, bios, how-we-met,
│         note content, tags, and email handles.
├── GetPersonNotesQuery          { PersonId, InformationContext?, Tag?, Page, PageSize }
│                                    -> PagedResult<NoteDto>
├── GetPersonTimelineQuery       { PersonId, Page, PageSize }
│                                    -> PagedResult<TimelineEntryDto>
│       → Assembles a chronological feed of: notes, important date events,
│         project link events, company link events. Communication entries from
│         the Comms context are fetched via ICommsReadService (ACL) and merged
│         at the application layer. This query is read-heavy and may be cached.
├── GetPersonMeetingBriefingQuery { PersonId }
│                                    -> MeetingBriefingDto
│       → Returns: most recent note, upcoming important dates (next 30 days),
│         preferences, linked projects with status, last communication summary.
│         Communication summary fetched via ICommsReadService (ACL).
├── GetCompanyByIdQuery          { CompanyId } -> CompanyDetailDto
│       → Returns full company profile: tags, project links, linked people
│         (list of PersonSummaryDto with role/department).
├── ListCompaniesQuery           { Tags?, Industry?, ProjectId?, IsArchived?,
│                                  Search?, Page, PageSize }
│                                    -> PagedResult<CompanySummaryDto>
├── GetCompanyNotesQuery         { CompanyId, InformationContext?, Tag?,
│                                  Page, PageSize }
│                                    -> PagedResult<NoteDto>
└── GetCompanyCommsQuery         { CompanyId, Page, PageSize }
        → Aggregated communication history for all people linked to the company.
          Assembled via ICommsReadService (ACL); this context does not own the data.
```

---

## 8.6 Repository Interfaces

```csharp
/// <summary>
/// Repository for the Person aggregate root. Manages person profiles,
/// contact handles, memberships, project links, important dates, and notes.
/// </summary>
public interface IPersonRepository
{
    /// <summary>Loads a person by ID. Returns null if not found.</summary>
    Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct);

    /// <summary>
    /// Returns a paged list of person summaries for the owner, with optional
    /// filtering by tags, relationship type, company, project, archived state,
    /// and a search term (matched against name, email handles, and bio).
    /// </summary>
    Task<PagedResult<Person>> ListAsync(
        UserId ownerId,
        IReadOnlyList<string>? tags,
        RelationshipType? relationshipType,
        CompanyId? companyId,
        Guid? projectId,
        bool includeArchived,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Full-text search across person name, bio, how-we-met, tags, and
    /// email handles for the given owner.
    /// </summary>
    Task<PagedResult<Person>> SearchAsync(
        UserId ownerId, string term, int page, int pageSize, CancellationToken ct);

    /// <summary>Persists a new Person aggregate.</summary>
    Task AddAsync(Person person, CancellationToken ct);

    /// <summary>Persists changes to an existing Person aggregate.</summary>
    Task UpdateAsync(Person person, CancellationToken ct);

    /// <summary>
    /// Appends a note to the person. Note is a child entity — this method
    /// persists the Note alongside any pending aggregate changes.
    /// </summary>
    Task AddNoteAsync(PersonId personId, Note note, CancellationToken ct);

    /// <summary>Updates the content or tags of an existing note on this person.</summary>
    Task UpdateNoteAsync(PersonId personId, Note note, CancellationToken ct);

    /// <summary>Removes a note from this person. Idempotent.</summary>
    Task DeleteNoteAsync(PersonId personId, NoteId noteId, CancellationToken ct);

    /// <summary>
    /// Returns paged notes for a person, optionally filtered by
    /// InformationContext and tag.
    /// </summary>
    Task<PagedResult<Note>> ListNotesAsync(
        PersonId personId,
        InformationContext? context,
        string? tag,
        int page,
        int pageSize,
        CancellationToken ct);
}

/// <summary>
/// Repository for the Company aggregate root. Manages company profiles,
/// project links, tags, and notes.
/// </summary>
public interface ICompanyRepository
{
    /// <summary>Loads a company by ID. Returns null if not found.</summary>
    Task<Company?> GetByIdAsync(CompanyId id, CancellationToken ct);

    /// <summary>
    /// Returns a paged list of company summaries for the owner, with optional
    /// filtering by tags, industry, project, archived state, and a search term
    /// (matched against company name, industry, and tags).
    /// </summary>
    Task<PagedResult<Company>> ListAsync(
        UserId ownerId,
        IReadOnlyList<string>? tags,
        string? industry,
        Guid? projectId,
        bool includeArchived,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Full-text search across company name, industry, and tags for the
    /// given owner.
    /// </summary>
    Task<PagedResult<Company>> SearchAsync(
        UserId ownerId, string term, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Returns all people linked to this company as PersonSummary projections.
    /// Implemented via a join on Person.CompanyMemberships — does NOT load full
    /// Person aggregates.
    /// </summary>
    Task<IReadOnlyList<PersonCompanyMemberDto>> GetLinkedPeopleAsync(
        CompanyId companyId, CancellationToken ct);

    /// <summary>Persists a new Company aggregate.</summary>
    Task AddAsync(Company company, CancellationToken ct);

    /// <summary>Persists changes to an existing Company aggregate.</summary>
    Task UpdateAsync(Company company, CancellationToken ct);

    /// <summary>Appends a note to the company.</summary>
    Task AddNoteAsync(CompanyId companyId, Note note, CancellationToken ct);

    /// <summary>Updates the content or tags of an existing note on this company.</summary>
    Task UpdateNoteAsync(CompanyId companyId, Note note, CancellationToken ct);

    /// <summary>Removes a note from this company. Idempotent.</summary>
    Task DeleteNoteAsync(CompanyId companyId, NoteId noteId, CancellationToken ct);

    /// <summary>
    /// Returns paged notes for a company, optionally filtered by
    /// InformationContext and tag.
    /// </summary>
    Task<PagedResult<Note>> ListNotesAsync(
        CompanyId companyId,
        InformationContext? context,
        string? tag,
        int page,
        int pageSize,
        CancellationToken ct);
}
```

---

## 8.7 API Endpoints

All endpoints require a valid JWT. Data is always scoped to the authenticated user's `OwnerId`.

```
People endpoints:
GET    /api/people                              List people (filters: tags, type, companyId, projectId, archived, search, page, pageSize)
POST   /api/people                              Create person
GET    /api/people/{id}                         Get person profile
PUT    /api/people/{id}                         Update person profile
DELETE /api/people/{id}                         Archive person (soft archive, not hard delete)
POST   /api/people/{id}/unarchive               Unarchive person
POST   /api/people/{id}/handles                 Add contact handle
DELETE /api/people/{id}/handles/{channel}/{handle}  Remove contact handle
PUT    /api/people/{id}/preferences             Set preferences (upsert)
POST   /api/people/{id}/tags                    Add tag
DELETE /api/people/{id}/tags/{tag}              Remove tag
POST   /api/people/{id}/companies               Link person to company
DELETE /api/people/{id}/companies/{companyId}   Unlink person from company
POST   /api/people/{id}/projects                Link person to project
DELETE /api/people/{id}/projects/{projectId}    Unlink person from project
POST   /api/people/{id}/important-dates         Add important date
DELETE /api/people/{id}/important-dates/{dateId} Remove important date

Notes on a person:
GET    /api/people/{id}/notes                   List notes (filters: context, tag, page, pageSize)
POST   /api/people/{id}/notes                   Add note
PUT    /api/people/{id}/notes/{noteId}          Update note
DELETE /api/people/{id}/notes/{noteId}          Delete note

Timeline and briefing:
GET    /api/people/{id}/timeline                Get chronological interaction timeline (page, pageSize)
GET    /api/people/{id}/briefing                Get meeting briefing card (pre-call context summary)

People search:
GET    /api/people/search?q={term}              Full-text search across people and companies

Company endpoints:
GET    /api/companies                           List companies (filters: tags, industry, projectId, archived, search, page, pageSize)
POST   /api/companies                           Create company
GET    /api/companies/{id}                      Get company profile (includes linked people)
PUT    /api/companies/{id}                      Update company profile
DELETE /api/companies/{id}                      Archive company
POST   /api/companies/{id}/unarchive            Unarchive company
POST   /api/companies/{id}/tags                 Add tag
DELETE /api/companies/{id}/tags/{tag}           Remove tag
POST   /api/companies/{id}/projects             Link company to project
DELETE /api/companies/{id}/projects/{projectId} Unlink company from project

Notes on a company:
GET    /api/companies/{id}/notes                List notes (filters: context, tag, page, pageSize)
POST   /api/companies/{id}/notes                Add note
PUT    /api/companies/{id}/notes/{noteId}       Update note
DELETE /api/companies/{id}/notes/{noteId}       Delete note

Company comms:
GET    /api/companies/{id}/comms                Aggregated comms for all people linked to company
```

---

## 8.8 Anti-Corruption Layer (ICommsReadService)

The People context assembles timeline and briefing data by reading from the Comms context. This integration is mediated by an ACL port that translates Comms-side concepts into the People context's own vocabulary. The People context never depends on Comms domain types directly.

```csharp
/// <summary>
/// ACL port for reading communication data from the Comms context.
/// The People context uses this interface to aggregate linked messages
/// into timelines and briefing cards. It does NOT mutate Comms data.
/// </summary>
public interface ICommsReadService
{
    /// <summary>
    /// Returns a summary of all messages linked to the given person,
    /// ordered newest-first. Channel icons and message snippets only —
    /// no full message body.
    /// </summary>
    Task<IReadOnlyList<LinkedMessageSummaryDto>> GetLinkedMessagesForPersonAsync(
        PersonId personId, UserId ownerId, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Returns a summary of all messages linked to any person who is a member
    /// of the given company. Messages are deduped across members.
    /// </summary>
    Task<IReadOnlyList<LinkedMessageSummaryDto>> GetLinkedMessagesForCompanyAsync(
        CompanyId companyId, UserId ownerId, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Returns the most recent linked message summary for a person —
    /// used as the "last contact" field in the meeting briefing card.
    /// </summary>
    Task<LinkedMessageSummaryDto?> GetMostRecentMessageForPersonAsync(
        PersonId personId, UserId ownerId, CancellationToken ct);
}
```

---

## 8.9 Application Layer Coordination

The People context has cross-context read-side workflows that are assembled at the application layer. Write-side operations are self-contained within the People context (Person aggregate and Company aggregate do not call other contexts' domain methods).

| Operation | People Context | Cross-Context |
|-----------|---------------|---------------|
| **Get Timeline** | Load person notes, project/company link events from IPersonRepository | Fetch linked messages via ICommsReadService; merge and sort chronologically |
| **Get Meeting Briefing** | Load person profile, notes, important dates, project links | Fetch most recent message via ICommsReadService for "last contact" field |
| **Get Company Comms** | Load company + linked people list via ICompanyRepository | Fetch messages for all linked persons via ICommsReadService; aggregate |
| **Create Person** | Person.Create(); publish PersonCreatedEvent | Comms context subscribes to PersonCreatedEvent to register email handles for auto-link |
| **Add Contact Handle** | Person.AddEmail/AddPhone(); publish PersonContactHandleAddedEvent | Comms context subscribes to PersonContactHandleAddedEvent to register new handle for auto-link |

---

## Design Notes

| Item | Type | Detail |
|------|------|--------|
| PP-007 (Personal vs Professional separation) | Partially covered | Modelled as `InformationContext` on Note and as a filter on `ListPeopleQuery`. The "Professional Only" toggle visible in S-PP-02 Step 6 is a query-side filter — no domain invariant needed. The UI presents this as a view mode toggle, not a data deletion. |
| PP-008 (Important dates with reminders) | Partially deferred | `ImportantDate` entity with `ReminderDaysBefore` is modelled here. Actual reminder dispatch (scheduling + sending) is a concern for the Notification context. A future `UpcomingDateReminderDueEvent` published by a background job would trigger Notification. The "yellow badge" in scenarios is a query-side calculation: `Date.DayOfYear - Today.DayOfYear <= 30`. |
| PP-012 (Import/export contacts) | Not modelled | The original product FR list in `people.md` does not include import/export; this appears to come from the task description summary only. No scenario covers it. Deferred pending explicit PRD clarification. |
| PP-013 (Timeline — Comms aggregation) | Dependency on Comms context | `GetPersonTimelineQuery` depends on `ICommsReadService`. If the Comms context is not yet implemented, the timeline will show only People-owned events (notes, dates, links). The ACL port is designed to fail gracefully (empty list) so the feature degrades without Comms. |
| PP-014 (Family/relationship tree) | Deferred P3 | Not modelled. The scenario file explicitly marks this as out of scope for v2 implementation. |
| Custom fields (PP-011 in original summary) | Not mapped | The original module summary listed custom fields as PP-011, but the authoritative `people.md` product file maps PP-011 to "Tag and categorize people/companies". Custom fields are not in the authoritative FRs. Deferred pending clarification. |
