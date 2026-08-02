using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;
using Pgvector;

#nullable disable

namespace TaskPilot.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeSemanticCopilotReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CopilotChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopilotChatSessions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CopilotChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CopilotChatSessions_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    ParentFolderId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedSlug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeFolders_KnowledgeFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "KnowledgeFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeFolders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeFolders_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedSlug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeTags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeTags_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SemanticDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    SourceText = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dimensions = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemanticDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SemanticDocuments_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReportCheckpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReportCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReportCheckpoints_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReports_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyReports_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReportSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Cron = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReportSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReportSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CopilotChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CitationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopilotChatMessages_CopilotChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CopilotChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    FolderId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedSlug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SearchDocument = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"Content\", ''))", stored: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeNotes_KnowledgeFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "KnowledgeFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeNotes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeNotes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeNotes_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNoteLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    SourceNoteId = table.Column<int>(type: "integer", nullable: false),
                    TargetNoteId = table.Column<int>(type: "integer", nullable: true),
                    TargetNormalizedSlug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNoteLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteLinks_KnowledgeNotes_SourceNoteId",
                        column: x => x.SourceNoteId,
                        principalTable: "KnowledgeNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteLinks_KnowledgeNotes_TargetNoteId",
                        column: x => x.TargetNoteId,
                        principalTable: "KnowledgeNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteLinks_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNoteRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NoteId = table.Column<int>(type: "integer", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedSlug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    RestoredFromRevisionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNoteRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteRevisions_KnowledgeNoteRevisions_RestoredFromR~",
                        column: x => x.RestoredFromRevisionId,
                        principalTable: "KnowledgeNoteRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteRevisions_KnowledgeNotes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "KnowledgeNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteRevisions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNoteTagAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NoteId = table.Column<int>(type: "integer", nullable: false),
                    KnowledgeTagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNoteTagAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteTagAssignments_KnowledgeNotes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "KnowledgeNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeNoteTagAssignments_KnowledgeTags_KnowledgeTagId",
                        column: x => x.KnowledgeTagId,
                        principalTable: "KnowledgeTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskNoteLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    TaskId = table.Column<int>(type: "integer", nullable: false),
                    NoteId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskNoteLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskNoteLinks_KnowledgeNotes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "KnowledgeNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskNoteLinks_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskNoteLinks_WorkSpaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "WorkSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeadLetteredAtUtc_LockedUntilUtc_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "DeadLetteredAtUtc", "LockedUntilUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_LockId",
                table: "OutboxMessages",
                column: "LockId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotChatMessages_SessionId_CreatedAtUtc",
                table: "CopilotChatMessages",
                columns: new[] { "SessionId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotChatSessions_ProjectId",
                table: "CopilotChatSessions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotChatSessions_UserId_WorkspaceId_ProjectId_UpdatedAtU~",
                table: "CopilotChatSessions",
                columns: new[] { "UserId", "WorkspaceId", "ProjectId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotChatSessions_WorkspaceId",
                table: "CopilotChatSessions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeFolders_ParentFolderId",
                table: "KnowledgeFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeFolders_ProjectId",
                table: "KnowledgeFolders",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeFolders_WorkspaceId_ParentFolderId",
                table: "KnowledgeFolders",
                columns: new[] { "WorkspaceId", "ParentFolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeFolders_WorkspaceId_ProjectId_NormalizedSlug",
                table: "KnowledgeFolders",
                columns: new[] { "WorkspaceId", "ProjectId", "NormalizedSlug" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteLinks_SourceNoteId_TargetNormalizedSlug",
                table: "KnowledgeNoteLinks",
                columns: new[] { "SourceNoteId", "TargetNormalizedSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteLinks_TargetNoteId",
                table: "KnowledgeNoteLinks",
                column: "TargetNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteLinks_WorkspaceId_TargetNoteId",
                table: "KnowledgeNoteLinks",
                columns: new[] { "WorkspaceId", "TargetNoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteRevisions_CreatedByUserId",
                table: "KnowledgeNoteRevisions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteRevisions_NoteId_RevisionNumber",
                table: "KnowledgeNoteRevisions",
                columns: new[] { "NoteId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteRevisions_RestoredFromRevisionId",
                table: "KnowledgeNoteRevisions",
                column: "RestoredFromRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNotes_CreatedByUserId",
                table: "KnowledgeNotes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNotes_FolderId",
                table: "KnowledgeNotes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNotes_ProjectId",
                table: "KnowledgeNotes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNotes_SearchDocument",
                table: "KnowledgeNotes",
                column: "SearchDocument")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNotes_WorkspaceId_NormalizedSlug",
                table: "KnowledgeNotes",
                columns: new[] { "WorkspaceId", "NormalizedSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNotes_WorkspaceId_ProjectId_UpdatedAt",
                table: "KnowledgeNotes",
                columns: new[] { "WorkspaceId", "ProjectId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteTagAssignments_KnowledgeTagId",
                table: "KnowledgeNoteTagAssignments",
                column: "KnowledgeTagId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNoteTagAssignments_NoteId_KnowledgeTagId",
                table: "KnowledgeNoteTagAssignments",
                columns: new[] { "NoteId", "KnowledgeTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTags_ProjectId",
                table: "KnowledgeTags",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTags_WorkspaceId_ProjectId_NormalizedSlug",
                table: "KnowledgeTags",
                columns: new[] { "WorkspaceId", "ProjectId", "NormalizedSlug" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SemanticDocuments_Embedding",
                table: "SemanticDocuments",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 64)
                .Annotation("Npgsql:StorageParameter:m", 16);

            migrationBuilder.CreateIndex(
                name: "IX_SemanticDocuments_ProjectId",
                table: "SemanticDocuments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SemanticDocuments_SourceType_SourceId_ChunkIndex_EmbeddingM~",
                table: "SemanticDocuments",
                columns: new[] { "SourceType", "SourceId", "ChunkIndex", "EmbeddingModel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemanticDocuments_WorkspaceId_ProjectId_IsActive_UpdatedAt",
                table: "SemanticDocuments",
                columns: new[] { "WorkspaceId", "ProjectId", "IsActive", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskNoteLinks_NoteId",
                table: "TaskNoteLinks",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNoteLinks_TaskId_NoteId",
                table: "TaskNoteLinks",
                columns: new[] { "TaskId", "NoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskNoteLinks_WorkspaceId_TaskId",
                table: "TaskNoteLinks",
                columns: new[] { "WorkspaceId", "TaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReportCheckpoints_ProjectId",
                table: "WeeklyReportCheckpoints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReportCheckpoints_SourceEventId_Stage",
                table: "WeeklyReportCheckpoints",
                columns: new[] { "SourceEventId", "Stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_ProjectId_CreatedAtUtc",
                table: "WeeklyReports",
                columns: new[] { "ProjectId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_RequestedByUserId",
                table: "WeeklyReports",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_SourceEventId",
                table: "WeeklyReports",
                column: "SourceEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReportSchedules_ProjectId",
                table: "WeeklyReportSchedules",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopilotChatMessages");

            migrationBuilder.DropTable(
                name: "KnowledgeNoteLinks");

            migrationBuilder.DropTable(
                name: "KnowledgeNoteRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeNoteTagAssignments");

            migrationBuilder.DropTable(
                name: "SemanticDocuments");

            migrationBuilder.DropTable(
                name: "TaskNoteLinks");

            migrationBuilder.DropTable(
                name: "WeeklyReportCheckpoints");

            migrationBuilder.DropTable(
                name: "WeeklyReports");

            migrationBuilder.DropTable(
                name: "WeeklyReportSchedules");

            migrationBuilder.DropTable(
                name: "CopilotChatSessions");

            migrationBuilder.DropTable(
                name: "KnowledgeTags");

            migrationBuilder.DropTable(
                name: "KnowledgeNotes");

            migrationBuilder.DropTable(
                name: "KnowledgeFolders");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_DeadLetteredAtUtc_LockedUntilUtc_CreatedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_LockId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "OutboxMessages");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
