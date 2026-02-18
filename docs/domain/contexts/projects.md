# Projects Context

> **Source**: Designed for v2 — see docs/product/modules/projects.md and docs/scenarios/project-management.md
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## 8.1 Design Principles

1. **Project owns registration and metadata, not git state** — The Project aggregate manages what LemonDo knows about a repository (path, name, tech stack, linked tasks, linked people, settings). Live git state (branch status, dirty files, ahead/behind counts) is fetched on demand via the `IGitService` ACL and never stored as durable domain state on the Project itself.

2. **Four separate aggregate roots, not one god aggregate** — `Project`, `Worktree`, `DevServer`, and `Tunnel` are each independent aggregate roots. They share only IDs across their boundaries. This separation reflects their independent lifecycles: a project outlives its worktrees; a worktree outlives its dev server; a dev server outlives its tunnel.

3. **This context does not own tasks or people** — `TaskId` and `PersonId` are stored as cross-context foreign references (weak links). The Projects context never creates, reads the state of, or manages those entities. It only records that a link exists.

4. **Process management is an ACL concern** — Starting a dev server process or creating an ngrok tunnel are infrastructure operations handled by `IProcessService` and `ITunnelService` respectively. The domain models the intent and outcome (Running, Failed, Active, Destroyed) — never the OS-level or HTTP-level details.

5. **Git operations are always ACL-mediated** — All git commands (worktree add/remove, commit, log, status) are issued through `IGitService`. The domain records the result (WorktreeCreated, GitCommitCreated) but never executes shell commands directly.

---

## 8.2 Entities

### Project (Aggregate Root)

```
Project
├── Id: ProjectId (value object)
├── OwnerId: UserId (from Identity context)
├── Name: ProjectName (value object)
├── Description: string? (auto-detected from README, max 2000 chars)
├── LocalPath: RepositoryPath (value object, absolute path on disk)
├── DefaultBranch: BranchName (value object)
├── TechStackTags: IReadOnlyList<TechStackTag> (auto-detected on registration)
├── DocFiles: IReadOnlyList<DocumentFile> (detected markdown file paths relative to root)
├── LinkedTaskIds: IReadOnlyList<TaskId> (cross-context foreign references)
├── LinkedPersonIds: IReadOnlyList<PersonId> (cross-context foreign references)
├── Status: ProjectStatus (Active, Archived)
├── LastSeenCommitSha: CommitSha? (snapshot at last scan or refresh)
├── LastSeenCommitMessage: string? (display only, max 500 chars)
├── LastSeenCommitAt: DateTimeOffset?
├── BranchCount: int? (snapshot at last scan or refresh)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Register(ownerId, name, localPath, defaultBranch, description?, techStackTags, docFiles,
│   │           lastSeenCommitSha?, lastSeenCommitMessage?, branchCount?)
│   │       -> ProjectRegisteredEvent
│   │          (validates all VOs, sets Status = Active)
│   ├── UpdateMetadata(name?, description?, defaultBranch?)
│   │       -> ProjectUpdatedEvent
│   ├── RefreshSnapshot(commitSha, commitMessage, commitAt, branchCount, techStackTags, docFiles)
│   │       -> ProjectSnapshotRefreshedEvent
│   │          (updates last-seen fields from a fresh scan)
│   ├── LinkTask(taskId)
│   │       -> TaskLinkedToProjectEvent
│   │          (idempotent: no-op if taskId already in list)
│   ├── UnlinkTask(taskId)
│   │       -> TaskUnlinkedFromProjectEvent
│   ├── LinkPerson(personId)
│   │       -> PersonLinkedToProjectEvent
│   │          (idempotent: no-op if personId already in list)
│   ├── UnlinkPerson(personId)
│   │       -> PersonUnlinkedFromProjectEvent
│   └── Archive()
│           -> ProjectArchivedEvent
│              (only allowed when Status = Active)
│
└── Invariants:
    ├── LocalPath must be an absolute path to an existing directory containing a .git folder
    ├── Name must be 1-200 characters, trimmed
    ├── DefaultBranch must be 1-250 characters, trimmed
    ├── TechStackTags may be empty but not null; each tag is 1-100 chars, trimmed, lowercase
    ├── A TaskId may only appear once in LinkedTaskIds (no duplicates)
    ├── A PersonId may only appear once in LinkedPersonIds (no duplicates)
    ├── Cannot archive an already-archived project
    ├── OwnerId cannot change after registration
    └── Description max 2000 characters
```

### Worktree (Aggregate Root)

```
Worktree
├── Id: WorktreeId (value object)
├── ProjectId: ProjectId (references Project aggregate)
├── OwnerId: UserId (denormalized from Project for auth checks)
├── BranchName: BranchName (value object)
├── LocalPath: RepositoryPath (absolute path to this worktree on disk)
├── BaseBranch: BranchName? (the branch it was created from)
├── Status: WorktreeStatus (Creating, Clean, Modified, Ahead, Behind, AheadBehind, Conflict, Deleting, Deleted)
├── AheadCount: int (commits ahead of tracking branch; 0 if unknown)
├── BehindCount: int (commits behind tracking branch; 0 if unknown)
├── ModifiedFileCount: int (unstaged/staged changes; 0 if unknown)
├── LastStatusRefreshedAt: DateTimeOffset?
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(projectId, ownerId, branchName, localPath, baseBranch?)
│   │       -> WorktreeCreatedEvent
│   │          (sets Status = Creating; git operation is ACL concern)
│   ├── MarkReady(initialStatus)
│   │       -> WorktreeStatusRefreshedEvent
│   │          (called after IGitService confirms worktree add succeeded)
│   ├── RefreshStatus(status, aheadCount, behindCount, modifiedFileCount)
│   │       -> WorktreeStatusRefreshedEvent
│   │          (polling result applied; updates LastStatusRefreshedAt)
│   ├── RecordCommit(sha, message, committedAt)
│   │       -> GitCommitCreatedEvent
│   │          (domain records that a commit was made through LemonDo)
│   ├── RecordDependencyInstall(packageManager, durationSeconds)
│   │       -> DependenciesInstalledEvent
│   └── MarkDeleted()
│           -> WorktreeDeletedEvent
│              (soft-delete; only allowed when no DevServer is Running on this worktree)
│
└── Invariants:
    ├── BranchName must be 1-250 characters, trimmed, no whitespace
    ├── LocalPath must be non-empty and formatted as an absolute path
    ├── AheadCount and BehindCount must be >= 0
    ├── ModifiedFileCount must be >= 0
    ├── Cannot transition from Deleted back to any other status
    ├── Cannot delete a worktree while a DevServer for it is in Running or Starting status
    └── ProjectId cannot change after creation
```

### DevServer (Aggregate Root)

```
DevServer
├── Id: DevServerId (value object)
├── ProjectId: ProjectId (references Project aggregate)
├── WorktreeId: WorktreeId (references Worktree aggregate)
├── OwnerId: UserId (denormalized for auth checks)
├── Command: string (e.g. "pnpm dev", max 500 chars)
├── Port: PortNumber (value object)
├── WorkingDirectory: RepositoryPath (directory where command is executed)
├── Status: DevServerStatus (Stopped, Starting, Running, Failed)
├── LastExitCode: int? (set on Stop or Fail)
├── StartedAt: DateTimeOffset? (set when status transitions to Running)
├── StoppedAt: DateTimeOffset? (set when status transitions to Stopped or Failed)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Define(projectId, worktreeId, ownerId, command, port, workingDirectory)
│   │       -> DevServerDefinedEvent
│   │          (creates the definition; does NOT start the process; Status = Stopped)
│   ├── Start()
│   │       -> DevServerStartedEvent
│   │          (Status: Stopped/Failed -> Starting; process launch is ACL concern)
│   ├── MarkRunning()
│   │       -> DevServerMarkedRunningEvent
│   │          (Status: Starting -> Running; called when health check confirms port open)
│   ├── Stop(exitCode?)
│   │       -> DevServerStoppedEvent
│   │          (Status: Running/Starting -> Stopped; records exit code)
│   ├── MarkFailed(exitCode?)
│   │       -> DevServerFailedEvent
│   │          (Status: Starting/Running -> Failed; records exit code)
│   └── Restart()
│           -> DevServerStartedEvent
│              (alias for Stop then Start in one domain operation)
│
└── Invariants:
    ├── Command must be 1-500 characters, trimmed
    ├── Port must be 1-65535
    ├── Cannot Start a server that is already Starting or Running
    ├── Cannot Stop a server that is already Stopped
    ├── StartedAt is set when status transitions to Running; cleared on Stop
    ├── StoppedAt is set when status transitions to Stopped or Failed
    ├── Only one DevServer per (WorktreeId + Port) combination may be in Running state
    └── WorktreeId and ProjectId cannot change after definition
```

### Tunnel (Aggregate Root)

```
Tunnel
├── Id: TunnelId (value object)
├── DevServerId: DevServerId (references DevServer aggregate)
├── ProjectId: ProjectId (denormalized for queries)
├── OwnerId: UserId (denormalized for auth checks)
├── PublicUrl: TunnelUrl (value object)
├── Status: TunnelStatus (Creating, Active, Destroying, Destroyed)
├── RequestsServed: int (updated by polling; starts at 0)
├── TunnelStartedAt: DateTimeOffset? (set when Status = Active)
├── TunnelEndedAt: DateTimeOffset? (set when Status = Destroyed)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(devServerId, projectId, ownerId)
│   │       -> TunnelCreatedEvent
│   │          (Status = Creating; actual ngrok API call is ACL concern)
│   ├── MarkActive(publicUrl)
│   │       -> TunnelMarkedActiveEvent
│   │          (Status: Creating -> Active; sets PublicUrl and TunnelStartedAt)
│   ├── RecordRequests(totalRequests)
│   │       -> (no event; in-place update for dashboard display)
│   └── Destroy()
│           -> TunnelDestroyedEvent
│              (Status: Active -> Destroying -> Destroyed; sets TunnelEndedAt)
│
└── Invariants:
    ├── Can only create a Tunnel for a DevServer that is in Running status
    ├── Only one Active tunnel per DevServerId at a time
    ├── PublicUrl must be a valid HTTPS URL
    ├── RequestsServed must be >= 0
    ├── Cannot transition out of Destroyed status
    └── TunnelEndedAt is only set when Status = Destroyed
```

---

## 8.3 Value Objects

```
ProjectId           -> Guid wrapper
WorktreeId          -> Guid wrapper
DevServerId         -> Guid wrapper
TunnelId            -> Guid wrapper
PersonId            -> Guid wrapper (cross-context reference to People context)
ProjectName         -> Non-empty string, 1-200 chars, trimmed
RepositoryPath      -> Non-empty string, absolute OS path, trimmed; must not be empty
BranchName          -> Non-empty string, 1-250 chars, trimmed, no internal whitespace
TechStackTag        -> Non-empty string, 1-100 chars, trimmed, lowercase (e.g. "dotnet", "react", "pnpm")
DocumentFile        -> Relative file path string, 1-500 chars (e.g. "README.md", "docs/architecture.md")
CommitSha           -> Non-empty string, 7-40 hex chars (short or full SHA)
PortNumber          -> int, 1-65535
TunnelUrl           -> HTTPS URL string, non-empty, max 500 chars
PackageManager      -> Enum: Npm, Pnpm, Yarn, Dotnet, Cargo, Make, Unknown
ProjectStatus       -> Enum: Active, Archived
WorktreeStatus      -> Enum: Creating, Clean, Modified, Ahead, Behind, AheadBehind, Conflict, Deleting, Deleted
DevServerStatus     -> Enum: Stopped, Starting, Running, Failed
TunnelStatus        -> Enum: Creating, Active, Destroying, Destroyed
GitAheadBehind      -> { AheadCount: int, BehindCount: int } (value object returned from IGitService)
```

---

## 8.4 Domain Events

```
ProjectRegisteredEvent          { ProjectId, OwnerId, Name, LocalPath }
ProjectUpdatedEvent             { ProjectId, OwnerId }
ProjectSnapshotRefreshedEvent   { ProjectId, CommitSha, BranchCount }
ProjectArchivedEvent            { ProjectId, OwnerId }
TaskLinkedToProjectEvent        { ProjectId, TaskId }
TaskUnlinkedFromProjectEvent    { ProjectId, TaskId }
PersonLinkedToProjectEvent      { ProjectId, PersonId }
PersonUnlinkedFromProjectEvent  { ProjectId, PersonId }

WorktreeCreatedEvent            { WorktreeId, ProjectId, BranchName, LocalPath }
WorktreeStatusRefreshedEvent    { WorktreeId, ProjectId, Status, AheadCount, BehindCount, ModifiedFileCount }
WorktreeDeletedEvent            { WorktreeId, ProjectId, BranchName }
DependenciesInstalledEvent      { WorktreeId, ProjectId, PackageManager, DurationSeconds }
GitCommitCreatedEvent           { WorktreeId, ProjectId, CommitSha, CommitMessage }

DevServerDefinedEvent           { DevServerId, ProjectId, WorktreeId, Port }
DevServerStartedEvent           { DevServerId, ProjectId, WorktreeId, Port }
DevServerMarkedRunningEvent     { DevServerId, ProjectId, Port }
DevServerStoppedEvent           { DevServerId, ProjectId, ExitCode? }
DevServerFailedEvent            { DevServerId, ProjectId, ExitCode? }

TunnelCreatedEvent              { TunnelId, DevServerId, ProjectId }
TunnelMarkedActiveEvent         { TunnelId, DevServerId, ProjectId, PublicUrl }
TunnelDestroyedEvent            { TunnelId, DevServerId, ProjectId, RequestsServed }
```

---

## 8.5 Use Cases

```
Commands:
├── RegisterProjectCommand          { Name, LocalPath }
│       → Invokes IGitService.ScanRepositoryAsync(localPath) to detect tech stack,
│         doc files, default branch, commit info, and branch count.
│         Creates Project aggregate, raises ProjectRegisteredEvent.
│         Returns ProjectDto with auto-detected metadata for confirmation preview.
│
├── UpdateProjectMetadataCommand    { ProjectId, Name?, Description?, DefaultBranch? }
│       → Loads Project, calls UpdateMetadata(), saves.
│
├── RefreshProjectSnapshotCommand   { ProjectId }
│       → Calls IGitService.ScanRepositoryAsync(), applies result via RefreshSnapshot().
│         Raises ProjectSnapshotRefreshedEvent.
│
├── ArchiveProjectCommand           { ProjectId }
│       → Loads Project, calls Archive(), saves. Raises ProjectArchivedEvent.
│
├── LinkTaskToProjectCommand        { ProjectId, TaskId }
│       → Loads Project, calls LinkTask(taskId). No cross-context validation —
│         the TaskId foreign reference is stored as-is.
│
├── UnlinkTaskFromProjectCommand    { ProjectId, TaskId }
│       → Loads Project, calls UnlinkTask(taskId).
│
├── LinkPersonToProjectCommand      { ProjectId, PersonId }
│       → Loads Project, calls LinkPerson(personId). PersonId cross-context reference
│         stored as-is; People context is notified via PersonLinkedToProjectEvent.
│
├── UnlinkPersonFromProjectCommand  { ProjectId, PersonId }
│       → Loads Project, calls UnlinkPerson(personId).
│
├── CreateWorktreeCommand           { ProjectId, BranchName, BaseBranch?, LocalPath? }
│       → Loads Project (to validate it exists and is Active).
│         Creates Worktree aggregate (Status = Creating).
│         Calls IGitService.AddWorktreeAsync(localPath, branchName, baseBranch).
│         On success: calls worktree.MarkReady(initialStatus), saves.
│         On failure: marks worktree with a Failed status and surface error.
│
├── DeleteWorktreeCommand           { WorktreeId }
│       → Loads Worktree, validates no Running DevServer on this worktree.
│         Calls IGitService.RemoveWorktreeAsync(localPath).
│         Calls worktree.MarkDeleted(), saves. Raises WorktreeDeletedEvent.
│
├── RefreshWorktreeStatusCommand    { WorktreeId }
│       → Calls IGitService.GetWorktreeStatusAsync(localPath).
│         Calls worktree.RefreshStatus(...), saves.
│
├── InstallDependenciesCommand      { WorktreeId }
│       → Loads Worktree, detects PackageManager via IProcessService.DetectPackageManager().
│         Runs install command via IProcessService.RunInstallAsync().
│         Calls worktree.RecordDependencyInstall(...), saves.
│
├── CommitChangesCommand            { WorktreeId, Message, FilePaths? }
│       → Calls IGitService.StageAndCommitAsync(worktreePath, message, filePaths?).
│         Calls worktree.RecordCommit(sha, message, committedAt), saves.
│
├── DefineDevServerCommand          { ProjectId, WorktreeId, Command, Port, WorkingDirectory? }
│       → Creates DevServer aggregate (Status = Stopped), saves.
│
├── StartDevServerCommand           { DevServerId }
│       → Loads DevServer, calls Start().
│         Launches process via IProcessService.StartProcessAsync(command, workingDirectory).
│         Polls port open via IProcessService.WaitForPortAsync(port).
│         On port open: calls MarkRunning(), saves.
│
├── StopDevServerCommand            { DevServerId }
│       → Loads DevServer, calls Stop(exitCode).
│         Terminates process via IProcessService.StopProcessAsync().
│
├── RestartDevServerCommand         { DevServerId }
│       → Loads DevServer, calls Restart() (Stop + Start).
│
├── CreateTunnelCommand             { DevServerId }
│       → Loads DevServer, validates Status = Running.
│         Validates no Active Tunnel already exists for this DevServerId.
│         Creates Tunnel aggregate (Status = Creating).
│         Calls ITunnelService.CreateTunnelAsync(port) to get public URL.
│         Calls tunnel.MarkActive(publicUrl), saves. Raises TunnelMarkedActiveEvent.
│
└── DestroyTunnelCommand            { TunnelId }
        → Loads Tunnel, calls Destroy().
          Calls ITunnelService.DestroyTunnelAsync(tunnelId), saves.
          Raises TunnelDestroyedEvent.

Queries:
├── ListProjectsQuery               { Status? } -> IReadOnlyList<ProjectSummaryDto>
│       (returns all projects for current user with snapshot metadata)
│
├── GetProjectByIdQuery             { ProjectId } -> ProjectDetailDto
│       (includes name, description, tech stack, linked task count, linked person count,
│        doc files, last commit info, branch count)
│
├── GetProjectDocFileQuery          { ProjectId, RelativePath } -> string (rendered markdown content)
│       (reads file from disk via IFileService; never stores file content in DB)
│
├── ListWorktreesQuery              { ProjectId } -> IReadOnlyList<WorktreeDto>
│       (returns all non-Deleted worktrees for the project with status, ahead/behind)
│
├── GetWorktreeByIdQuery            { WorktreeId } -> WorktreeDto
│
├── GetWorktreeDiffQuery            { WorktreeId } -> WorktreeDiffDto
│       (calls IGitService.GetDiffSummaryAsync(); returns changed files with +/- counts)
│
├── GetGitLogQuery                  { ProjectId, WorktreeId?, BranchName?, MaxCommits? }
│       -> IReadOnlyList<CommitSummaryDto>
│       (calls IGitService.GetLogAsync(); returns commits with sha, message, author, timestamp)
│
├── ListDevServersQuery             { ProjectId } -> IReadOnlyList<DevServerDto>
│       (returns all dev servers for the project with current status, port, uptime)
│
├── GetDevServerByIdQuery           { DevServerId } -> DevServerDto
│
├── ListTunnelsQuery                { ProjectId?, DevServerId? } -> IReadOnlyList<TunnelDto>
│       (returns Active and recent tunnels; DevServerId filter narrows to one server)
│
└── GetTunnelByIdQuery              { TunnelId } -> TunnelDto
```

---

## 8.6 Repository Interfaces

```csharp
/// <summary>
/// Repository for the Project aggregate root.
/// </summary>
public interface IProjectRepository
{
    /// <summary>Loads a project by ID. Returns null if not found.</summary>
    Task<Project?> GetByIdAsync(ProjectId id, CancellationToken ct);

    /// <summary>Returns all non-archived projects for the given owner, or all if status is provided.</summary>
    Task<IReadOnlyList<Project>> ListByOwnerAsync(
        UserId ownerId, ProjectStatus? status, CancellationToken ct);

    /// <summary>Persists a newly registered project.</summary>
    Task AddAsync(Project project, CancellationToken ct);

    /// <summary>Persists mutations to an existing project (metadata updates, links, archive).</summary>
    Task UpdateAsync(Project project, CancellationToken ct);
}

/// <summary>
/// Repository for the Worktree aggregate root.
/// </summary>
public interface IWorktreeRepository
{
    /// <summary>Loads a worktree by ID. Returns null if not found.</summary>
    Task<Worktree?> GetByIdAsync(WorktreeId id, CancellationToken ct);

    /// <summary>Returns all non-Deleted worktrees for the given project.</summary>
    Task<IReadOnlyList<Worktree>> ListByProjectAsync(ProjectId projectId, CancellationToken ct);

    /// <summary>Returns all worktrees in Running or Starting state across all projects for a user.
    /// Used for dashboard status aggregation.</summary>
    Task<IReadOnlyList<Worktree>> ListActiveByOwnerAsync(UserId ownerId, CancellationToken ct);

    /// <summary>Persists a newly created worktree.</summary>
    Task AddAsync(Worktree worktree, CancellationToken ct);

    /// <summary>Persists status refresh and mutation results.</summary>
    Task UpdateAsync(Worktree worktree, CancellationToken ct);
}

/// <summary>
/// Repository for the DevServer aggregate root.
/// </summary>
public interface IDevServerRepository
{
    /// <summary>Loads a dev server by ID. Returns null if not found.</summary>
    Task<DevServer?> GetByIdAsync(DevServerId id, CancellationToken ct);

    /// <summary>Returns all dev servers for the given project.</summary>
    Task<IReadOnlyList<DevServer>> ListByProjectAsync(ProjectId projectId, CancellationToken ct);

    /// <summary>Returns all Running dev servers for the given worktree.
    /// Used to enforce the "no delete while running" invariant.</summary>
    Task<IReadOnlyList<DevServer>> ListRunningByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>Persists a newly defined dev server.</summary>
    Task AddAsync(DevServer devServer, CancellationToken ct);

    /// <summary>Persists status transitions (Start, Stop, Fail, MarkRunning).</summary>
    Task UpdateAsync(DevServer devServer, CancellationToken ct);
}

/// <summary>
/// Repository for the Tunnel aggregate root.
/// </summary>
public interface ITunnelRepository
{
    /// <summary>Loads a tunnel by ID. Returns null if not found.</summary>
    Task<Tunnel?> GetByIdAsync(TunnelId id, CancellationToken ct);

    /// <summary>Returns all tunnels for the given dev server.</summary>
    Task<IReadOnlyList<Tunnel>> ListByDevServerAsync(DevServerId devServerId, CancellationToken ct);

    /// <summary>Returns the single Active tunnel for a dev server, or null if none.</summary>
    Task<Tunnel?> GetActiveTunnelForDevServerAsync(DevServerId devServerId, CancellationToken ct);

    /// <summary>Returns all tunnels for a project (Active and recent Destroyed).</summary>
    Task<IReadOnlyList<Tunnel>> ListByProjectAsync(ProjectId projectId, CancellationToken ct);

    /// <summary>Persists a newly created tunnel.</summary>
    Task AddAsync(Tunnel tunnel, CancellationToken ct);

    /// <summary>Persists status transitions (MarkActive, Destroy, request count updates).</summary>
    Task UpdateAsync(Tunnel tunnel, CancellationToken ct);
}
```

---

## 8.7 API Endpoints

```
Projects:
GET    /api/projects                               List user's projects (with status filter)     [Authenticated]
POST   /api/projects                               Register a project (triggers scan)            [Authenticated]
GET    /api/projects/{id}                          Get project detail                            [Authenticated]
PUT    /api/projects/{id}                          Update project metadata                       [Authenticated]
POST   /api/projects/{id}/refresh                  Refresh project snapshot from disk            [Authenticated]
POST   /api/projects/{id}/archive                  Archive project                               [Authenticated]
GET    /api/projects/{id}/docs/{*path}             Read a documentation file (rendered markdown) [Authenticated]
POST   /api/projects/{id}/tasks                    Link a task to this project                   [Authenticated]
DELETE /api/projects/{id}/tasks/{taskId}           Unlink a task from this project               [Authenticated]
POST   /api/projects/{id}/people                   Link a person to this project                 [Authenticated]
DELETE /api/projects/{id}/people/{personId}        Unlink a person from this project             [Authenticated]

Worktrees:
GET    /api/projects/{id}/worktrees                List worktrees for a project                  [Authenticated]
POST   /api/projects/{id}/worktrees                Create a worktree                             [Authenticated]
GET    /api/projects/{id}/worktrees/{wtId}         Get worktree detail                           [Authenticated]
DELETE /api/projects/{id}/worktrees/{wtId}         Delete a worktree                             [Authenticated]
POST   /api/projects/{id}/worktrees/{wtId}/refresh Refresh worktree status                       [Authenticated]
GET    /api/projects/{id}/worktrees/{wtId}/diff    Get worktree diff summary                     [Authenticated]
POST   /api/projects/{id}/worktrees/{wtId}/install Install dependencies                          [Authenticated]
POST   /api/projects/{id}/worktrees/{wtId}/commit  Commit staged/selected changes                [Authenticated]
GET    /api/projects/{id}/git/log                  Get git log (optionally filtered by worktree) [Authenticated]

Dev Servers:
GET    /api/projects/{id}/servers                  List dev servers for a project                [Authenticated]
POST   /api/projects/{id}/servers                  Define a dev server                           [Authenticated]
GET    /api/projects/{id}/servers/{srvId}          Get dev server detail                         [Authenticated]
POST   /api/projects/{id}/servers/{srvId}/start    Start dev server                              [Authenticated]
POST   /api/projects/{id}/servers/{srvId}/stop     Stop dev server                               [Authenticated]
POST   /api/projects/{id}/servers/{srvId}/restart  Restart dev server                            [Authenticated]

Tunnels:
GET    /api/projects/{id}/servers/{srvId}/tunnels  List tunnels for a dev server                 [Authenticated]
POST   /api/projects/{id}/servers/{srvId}/tunnels  Create ngrok tunnel for a running server      [Authenticated]
DELETE /api/projects/{id}/servers/{srvId}/tunnels/{tnlId}  Destroy tunnel                       [Authenticated]
```

---

## 8.8 Anti-Corruption Layer (Infrastructure Ports)

All external system interactions are isolated behind ports in the domain layer. No infrastructure concern crosses into the aggregates.

```csharp
/// <summary>
/// Port for all git operations. Shields the domain from shell execution details.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Scans a local git repository and returns auto-detected metadata.
    /// Detects tech stack tags, doc files, default branch, recent commit, branch count.
    /// Returns an error if the path does not contain a valid git repository.
    /// </summary>
    Task<Result<RepositoryScanResult, DomainError>> ScanRepositoryAsync(
        string localPath, CancellationToken ct);

    /// <summary>Creates a new git worktree at the specified path on the given branch.</summary>
    Task<Result<DomainError>> AddWorktreeAsync(
        string repoPath, string worktreePath, string branchName, string? baseBranch, CancellationToken ct);

    /// <summary>Removes a git worktree and optionally deletes the directory.</summary>
    Task<Result<DomainError>> RemoveWorktreeAsync(
        string worktreePath, bool deleteDirectory, CancellationToken ct);

    /// <summary>Returns current status of a worktree: dirty/clean, ahead/behind counts, modified file count.</summary>
    Task<Result<WorktreeStatusResult, DomainError>> GetWorktreeStatusAsync(
        string worktreePath, CancellationToken ct);

    /// <summary>Returns a diff summary for modified files in the given worktree.</summary>
    Task<Result<WorktreeDiffResult, DomainError>> GetDiffSummaryAsync(
        string worktreePath, CancellationToken ct);

    /// <summary>Stages specified files (or all) and creates a commit.</summary>
    Task<Result<CommitResult, DomainError>> StageAndCommitAsync(
        string worktreePath, string message, IEnumerable<string>? filePaths, CancellationToken ct);

    /// <summary>Returns recent commits for a repo or branch.</summary>
    Task<Result<IReadOnlyList<CommitSummary>, DomainError>> GetLogAsync(
        string repoPath, string? branchName, int maxCommits, CancellationToken ct);
}

/// <summary>
/// Port for process lifecycle management (dev server start/stop/health).
/// </summary>
public interface IProcessService
{
    /// <summary>Detects the package manager used by a project at the given path.</summary>
    Task<PackageManager> DetectPackageManagerAsync(string directoryPath, CancellationToken ct);

    /// <summary>Runs the appropriate install command for the detected package manager.</summary>
    Task<Result<DependencyInstallResult, DomainError>> RunInstallAsync(
        string directoryPath, PackageManager packageManager, CancellationToken ct);

    /// <summary>Starts a background process. Returns a process handle token.</summary>
    Task<Result<string, DomainError>> StartProcessAsync(
        string command, string workingDirectory, CancellationToken ct);

    /// <summary>Waits until the given port accepts connections or the timeout elapses.</summary>
    Task<Result<DomainError>> WaitForPortAsync(int port, TimeSpan timeout, CancellationToken ct);

    /// <summary>Terminates the process associated with the given handle token.</summary>
    Task<Result<int, DomainError>> StopProcessAsync(string processHandle, CancellationToken ct);
}

/// <summary>
/// Port for ngrok tunnel management.
/// </summary>
public interface ITunnelService
{
    /// <summary>Creates an HTTP tunnel to the given local port. Returns the public HTTPS URL.</summary>
    Task<Result<string, DomainError>> CreateTunnelAsync(int localPort, CancellationToken ct);

    /// <summary>Destroys an existing tunnel by its ngrok tunnel ID.</summary>
    Task<Result<DomainError>> DestroyTunnelAsync(string ngrokTunnelId, CancellationToken ct);

    /// <summary>Returns the total request count served by a tunnel (for polling the dashboard counter).</summary>
    Task<Result<int, DomainError>> GetRequestCountAsync(string ngrokTunnelId, CancellationToken ct);
}

/// <summary>
/// Port for reading documentation files from disk.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Reads and returns the raw content of a file within a project's local path.
    /// Enforces that the resolved path is within the project root (path traversal prevention).
    /// </summary>
    Task<Result<string, DomainError>> ReadProjectFileAsync(
        string projectRootPath, string relativeFilePath, CancellationToken ct);
}
```

---

## 8.9 Application Layer Coordination

Several use cases in the Projects context require multi-step coordination across aggregates and ACL ports. These are handled in application layer command handlers, never in domain objects.

| Operation | Step 1 — Domain | Step 2 — ACL / Infrastructure | Step 3 — Domain |
|-----------|-----------------|-------------------------------|-----------------|
| **Register Project** | None | `IGitService.ScanRepositoryAsync()` | `Project.Register(...)` |
| **Create Worktree** | `Worktree.Create(...)` (Status = Creating) | `IGitService.AddWorktreeAsync()` | `Worktree.MarkReady(status)` |
| **Delete Worktree** | Check no Running DevServer | `IGitService.RemoveWorktreeAsync()` | `Worktree.MarkDeleted()` |
| **Start Dev Server** | `DevServer.Start()` (Status = Starting) | `IProcessService.StartProcessAsync()` + `WaitForPortAsync()` | `DevServer.MarkRunning()` |
| **Stop Dev Server** | `DevServer.Stop(exitCode)` | `IProcessService.StopProcessAsync()` | Saved |
| **Create Tunnel** | `Tunnel.Create(...)` (Status = Creating) | `ITunnelService.CreateTunnelAsync(port)` | `Tunnel.MarkActive(publicUrl)` |
| **Destroy Tunnel** | `Tunnel.Destroy()` | `ITunnelService.DestroyTunnelAsync(id)` | Saved |
| **Install Dependencies** | None | `IProcessService.DetectPackageManager()` + `RunInstallAsync()` | `Worktree.RecordDependencyInstall(...)` |
| **Commit Changes** | None | `IGitService.StageAndCommitAsync()` | `Worktree.RecordCommit(sha, message, at)` |

---

## Design Notes

| Item | Type | Detail |
|------|------|--------|
| PM-011 | Partially covered | Project-level settings (environment vars, CI/CD status) are modelled by `UpdateProjectMetadataCommand` covering `DefaultBranch` but environment variables and CI/CD integration are deferred. No scenario drives them at this level of detail. A `ProjectSettings` value object or child entity should be added in a future iteration when CI/CD integration requirements are clearer. |
| S-PM-04 Step 5 | Cross-module | The GitHub notifications sidebar panel is a read-only widget drawing from the Comms context (CM-006, CM-008). The Projects context does not own GitHub notification data — it only provides a `ProjectId` filter passed to the Comms context query. No domain concept is needed here in the Projects context. |
| S-PM-04 Step 7 "Start Agent Session" | Cross-module | Starting an agent session from a task context menu is owned by the Agents context (AG-001, AG-004, AG-005). The Projects context contributes `ProjectId` and `WorktreeId` as context payload passed to the Agents context when launching a session. No use case is defined here — it is an outbound cross-context action. |
| Worktree process handle | Design gap | `IProcessService.StartProcessAsync` returns a string handle token. The DevServer aggregate needs to store this handle to enable `StopProcessAsync`. This requires a `ProcessHandle: string?` field on DevServer that is infrastructure-level data. Consider whether this belongs on the aggregate or in an infrastructure-side process registry keyed by DevServerId. Recommendation: store it on DevServer as an opaque string; treat it as infrastructure state that happens to be persisted. |
| Real-time log streaming | Out of scope | Dev server startup log streaming and ngrok request count polling require a real-time channel (SSE or WebSocket). This is a presentation/infrastructure concern. The domain models the state transitions only (Starting -> Running, request count as a polled integer). The streaming transport is not part of this bounded context design. |
