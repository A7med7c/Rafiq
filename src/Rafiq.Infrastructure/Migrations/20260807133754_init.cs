using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiFlaggedRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserRequest = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AiResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFlaggedRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Feature = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRequestLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiUsageActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Stars = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminReply = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepliedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AppLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentUploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentUploadSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Otps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Otps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BodyAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiMessages_AiConversations_AiConversationId",
                        column: x => x.AiConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReactionType = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    TriageStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageReactions_AiMessages_AiMessageId",
                        column: x => x.AiMessageId,
                        principalTable: "AiMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentType = table.Column<int>(type: "int", nullable: false),
                    CustomType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppointmentDateTime = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ReminderOffsetMinutes = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HangfireJobId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsAiRestricted = table.Column<bool>(type: "bit", nullable: false),
                    IsRestricted = table.Column<bool>(type: "bit", nullable: false),
                    IsSuspended = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreviousConfirmedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ResendCount = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneVerifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccessTokenJti = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserHealthProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Height = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BloodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHealthProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserHealthProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChronicDiseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DiagnosedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicDiseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicDiseases_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    AiSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HospitalOrClinic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocumentDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OcrText = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MedicalAttentionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: true),
                    AnalysisStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessingStartedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralDocuments_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthProfileAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GranteeUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    Relationship = table.Column<int>(type: "int", nullable: true),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProfileAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthProfileAccesses_AspNetUsers_GranteeUserId",
                        column: x => x.GranteeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthProfileAccesses_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthProfileAccesses_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthSummaryCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    NeedsRefresh = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthSummaryCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthSummaryCaches_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagingReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BodyPart = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Impression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OCRText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MedicalAttentionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagingReports_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OCRText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MedicalAttentionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabReports_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrescriptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MedicalAttentionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMedicines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MedicalAttentionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMedicines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMedicines_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalRange = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_LabReports_LabReportId",
                        column: x => x.LabReportId,
                        principalTable: "LabReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionMedicines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionMedicines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionMedicines_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicineReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserMedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReminderTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RepeatType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineReminders_UserMedicines_UserMedicineId",
                        column: x => x.UserMedicineId,
                        principalTable: "UserMedicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineReminderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ReminderNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    NextJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationReminderLogs_MedicineReminders_MedicineReminderId",
                        column: x => x.MedicineReminderId,
                        principalTable: "MedicineReminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationReminderLogs_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_UserHealthProfileId",
                table: "AiConversations",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_UserId_LastMessageAt",
                table: "AiConversations",
                columns: new[] { "UserId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiFlaggedRequests_CreatedAt",
                table: "AiFlaggedRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiFlaggedRequests_UserId",
                table: "AiFlaggedRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiFlaggedRequests_UserId_CreatedAt",
                table: "AiFlaggedRequests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiMessages_AiConversationId_SequenceNumber",
                table: "AiMessages",
                columns: new[] { "AiConversationId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_CreatedAt",
                table: "AiRequestLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_Feature_CreatedAt",
                table: "AiRequestLogs",
                columns: new[] { "Feature", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_UserId",
                table: "AiRequestLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageActions_CreatedAt",
                table: "AiUsageActions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageActions_TargetUserId",
                table: "AiUsageActions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_UserHealthProfileId",
                table: "Allergies",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserHealthProfileId",
                table: "Appointments",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserHealthProfileId_AppointmentDateTime",
                table: "Appointments",
                columns: new[] { "UserHealthProfileId", "AppointmentDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserHealthProfileId_Status_AppointmentDateTime",
                table: "Appointments",
                columns: new[] { "UserHealthProfileId", "Status", "AppointmentDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_CreatedAt",
                table: "AppReviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_Status",
                table: "AppReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_UserId",
                table: "AppReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_HealthProfileId",
                table: "AspNetUsers",
                column: "HealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicDiseases_UserHealthProfileId",
                table: "ChronicDiseases",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUploadSessions_ExpiresAt",
                table: "DocumentUploadSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUploadSessions_ImageUrl",
                table: "DocumentUploadSessions",
                column: "ImageUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_UserId",
                table: "EmergencyContacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDocuments_AnalysisStatus_UpdatedAt",
                table: "GeneralDocuments",
                columns: new[] { "AnalysisStatus", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDocuments_UserHealthProfileId",
                table: "GeneralDocuments",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDocuments_UserHealthProfileId_FileHash",
                table: "GeneralDocuments",
                columns: new[] { "UserHealthProfileId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_GranteeUserId",
                table: "HealthProfileAccesses",
                column: "GranteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_InvitedByUserId",
                table: "HealthProfileAccesses",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_UserHealthProfileId",
                table: "HealthProfileAccesses",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_UserHealthProfileId_GranteeUserId",
                table: "HealthProfileAccesses",
                columns: new[] { "UserHealthProfileId", "GranteeUserId" },
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_HealthSummaryCaches_UserHealthProfileId_Language",
                table: "HealthSummaryCaches",
                columns: new[] { "UserHealthProfileId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagingReports_CreatedAt",
                table: "ImagingReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingReports_UserHealthProfileId",
                table: "ImagingReports",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingReports_UserHealthProfileId_FileHash",
                table: "ImagingReports",
                columns: new[] { "UserHealthProfileId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_CreatedAt",
                table: "LabReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_UserHealthProfileId",
                table: "LabReports",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_UserHealthProfileId_FileHash",
                table: "LabReports",
                columns: new[] { "UserHealthProfileId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_LabReportId",
                table: "LabResults",
                column: "LabReportId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderLogs_MedicineReminderId_ScheduledDate_ReminderNumber",
                table: "MedicationReminderLogs",
                columns: new[] { "MedicineReminderId", "ScheduledDate", "ReminderNumber" },
                unique: true,
                filter: "[Status] <> N'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderLogs_UserHealthProfileId_ScheduledDate",
                table: "MedicationReminderLogs",
                columns: new[] { "UserHealthProfileId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineReminders_UserMedicineId",
                table: "MedicineReminders",
                column: "UserMedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_AiMessageId_UserId_ReactionType",
                table: "MessageReactions",
                columns: new[] { "AiMessageId", "UserId", "ReactionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_UserId",
                table: "PhoneVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionMedicines_PrescriptionId",
                table: "PrescriptionMedicines",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_CreatedAt",
                table: "Prescriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_UserHealthProfileId",
                table: "Prescriptions",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_UserHealthProfileId_FileHash",
                table: "Prescriptions",
                columns: new[] { "UserHealthProfileId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AccessTokenJti",
                table: "RefreshTokens",
                column: "AccessTokenJti");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserHealthProfiles_UserId",
                table: "UserHealthProfiles",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMedicines_CreatedAt",
                table: "UserMedicines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserMedicines_UserHealthProfileId",
                table: "UserMedicines",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMedicines_UserHealthProfileId_FileHash",
                table: "UserMedicines",
                columns: new[] { "UserHealthProfileId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreatedAt",
                table: "UserNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.AddForeignKey(
                name: "FK_AiConversations_UserHealthProfiles_UserHealthProfileId",
                table: "AiConversations",
                column: "UserHealthProfileId",
                principalTable: "UserHealthProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Allergies_UserHealthProfiles_UserHealthProfileId",
                table: "Allergies",
                column: "UserHealthProfileId",
                principalTable: "UserHealthProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_UserHealthProfiles_UserHealthProfileId",
                table: "Appointments",
                column: "UserHealthProfileId",
                principalTable: "UserHealthProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserHealthProfiles_HealthProfileId",
                table: "AspNetUsers",
                column: "HealthProfileId",
                principalTable: "UserHealthProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserHealthProfiles_HealthProfileId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AiFlaggedRequests");

            migrationBuilder.DropTable(
                name: "AiRequestLogs");

            migrationBuilder.DropTable(
                name: "AiUsageActions");

            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AppReviews");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ChronicDiseases");

            migrationBuilder.DropTable(
                name: "DocumentUploadSessions");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "GeneralDocuments");

            migrationBuilder.DropTable(
                name: "HealthProfileAccesses");

            migrationBuilder.DropTable(
                name: "HealthSummaryCaches");

            migrationBuilder.DropTable(
                name: "ImagingReports");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "MedicationReminderLogs");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropTable(
                name: "Otps");

            migrationBuilder.DropTable(
                name: "PhoneVerifications");

            migrationBuilder.DropTable(
                name: "PrescriptionMedicines");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "LabReports");

            migrationBuilder.DropTable(
                name: "MedicineReminders");

            migrationBuilder.DropTable(
                name: "AiMessages");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "UserMedicines");

            migrationBuilder.DropTable(
                name: "AiConversations");

            migrationBuilder.DropTable(
                name: "UserHealthProfiles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
