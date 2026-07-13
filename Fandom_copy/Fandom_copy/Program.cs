using Fandom_copy.Data;
using Fandom_copy.Services;
using Fandom_copy.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/StatusCode/403";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    })
    .AddCookie("ExternalOAuth", options =>
    {
        options.Cookie.Name = "Fandom.ExternalOAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

if (IsOAuthProviderConfigured("Google"))
{
    var google = builder.Configuration.GetSection("Authentication:Google");
    authenticationBuilder.AddGoogle("Google", options =>
    {
        options.ClientId = google["ClientId"]!;
        options.ClientSecret = google["ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = "ExternalOAuth";
        options.SaveTokens = false;
    });
}

if (IsOAuthProviderConfigured("Facebook"))
{
    var facebook = builder.Configuration.GetSection("Authentication:Facebook");
    authenticationBuilder.AddFacebook("Facebook", options =>
    {
        options.ClientId = facebook["AppId"]!;
        options.ClientSecret = facebook["AppSecret"]!;
        options.CallbackPath = "/signin-facebook";
        options.SignInScheme = "ExternalOAuth";
        options.Scope.Add("email");
        options.Fields.Add("email");
        options.SaveTokens = false;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPostSectionService, PostSectionService>();
builder.Services.AddScoped<IPostImageStorage, PostImageStorage>();
builder.Services.AddScoped<IPostFileStorage, PostFileStorage>();
builder.Services.AddScoped<IPostVersionService, PostVersionService>();
builder.Services.AddScoped<IProfileImageStorage, ProfileImageStorage>();

var app = builder.Build();

// The module is immediately usable on a fresh local database. Replace this with
// migrations before deploying to a production environment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    // EnsureCreated does not extend an already existing database when a new
    // entity is added. This small, idempotent upgrade keeps local projects
    // usable until the application is moved to EF migrations.
    db.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[PostContentBlocks] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [PostId] uniqueidentifier NOT NULL,
                [ContainerSectionId] uniqueidentifier NULL,
                [Type] int NOT NULL,
                [Text] nvarchar(max) NOT NULL,
                [ImagePath] nvarchar(max) NOT NULL DEFAULT N'',
                [ImageCaption] nvarchar(max) NOT NULL DEFAULT N'',
                [SectionId] uniqueidentifier NULL,
                [Order] int NOT NULL
            );
            CREATE INDEX [IX_PostContentBlocks_PostId_ContainerSectionId_Order]
                ON [dbo].[PostContentBlocks] ([PostId], [ContainerSectionId], [Order]);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'ImagePath') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [ImagePath] nvarchar(max) NOT NULL CONSTRAINT [DF_PostContentBlocks_ImagePath] DEFAULT N'';
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'ImageCaption') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [ImageCaption] nvarchar(max) NOT NULL CONSTRAINT [DF_PostContentBlocks_ImageCaption] DEFAULT N'';
        END
        IF OBJECT_ID(N'[dbo].[Posts]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[Posts]', N'IconPath') IS NULL
        BEGIN
            ALTER TABLE [dbo].[Posts]
                ADD [IconPath] nvarchar(max) NULL;
        END
        IF OBJECT_ID(N'[dbo].[PostSections]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostSections]', N'IconPath') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostSections]
                ADD [IconPath] nvarchar(max) NULL;
        END
        IF OBJECT_ID(N'[dbo].[SavedPosts]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[SavedPosts] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [UserId] uniqueidentifier NOT NULL,
                [PostId] uniqueidentifier NOT NULL,
                [SavedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME())
            );
            CREATE UNIQUE INDEX [IX_SavedPosts_UserId_PostId] ON [dbo].[SavedPosts] ([UserId], [PostId]);
        END
        IF OBJECT_ID(N'[dbo].[Attachments]', N'U') IS NULL
           AND OBJECT_ID(N'[dbo].[PostSections]', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE [dbo].[Attachments] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [FileName] nvarchar(max) NOT NULL,
                [Path] nvarchar(max) NOT NULL,
                [Size] bigint NOT NULL,
                [PostSectionId] uniqueidentifier NOT NULL,
                CONSTRAINT [FK_Attachments_PostSections_PostSectionId]
                    FOREIGN KEY ([PostSectionId]) REFERENCES [dbo].[PostSections] ([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_Attachments_PostSectionId] ON [dbo].[Attachments] ([PostSectionId]);
        END
        IF OBJECT_ID(N'[dbo].[PostVersions]', N'U') IS NULL
           AND OBJECT_ID(N'[dbo].[Posts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE [dbo].[PostVersions] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [PostId] uniqueidentifier NOT NULL,
                [UserId] uniqueidentifier NOT NULL,
                [Action] nvarchar(128) NOT NULL,
                [SnapshotJson] nvarchar(max) NOT NULL,
                [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT [FK_PostVersions_Posts_PostId]
                    FOREIGN KEY ([PostId]) REFERENCES [dbo].[Posts] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_PostVersions_Users_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
            );
            CREATE INDEX [IX_PostVersions_PostId_CreatedAt] ON [dbo].[PostVersions] ([PostId], [CreatedAt]);
            CREATE INDEX [IX_PostVersions_UserId] ON [dbo].[PostVersions] ([UserId]);
        END
        IF OBJECT_ID(N'[dbo].[Tags]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[Tags] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [Name] nvarchar(max) NOT NULL
            );
        END
        IF OBJECT_ID(N'[dbo].[PostTags]', N'U') IS NULL
           AND OBJECT_ID(N'[dbo].[Posts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[Tags]', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE [dbo].[PostTags] (
                [PostId] uniqueidentifier NOT NULL,
                [TagId] uniqueidentifier NOT NULL,
                CONSTRAINT [PK_PostTags] PRIMARY KEY ([PostId], [TagId]),
                CONSTRAINT [FK_PostTags_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [dbo].[Posts] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_PostTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [dbo].[Tags] ([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_PostTags_TagId] ON [dbo].[PostTags] ([TagId]);
        END
        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[Users]', N'GlobalRole') IS NULL
        BEGIN
            ALTER TABLE [dbo].[Users]
                ADD [GlobalRole] int NOT NULL CONSTRAINT [DF_Users_GlobalRole] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[Users]', N'IsBanned') IS NULL
        BEGIN
            ALTER TABLE [dbo].[Users]
                ADD [IsBanned] bit NOT NULL CONSTRAINT [DF_Users_IsBanned] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[Users]', N'AvatarUrl') IS NULL
        BEGIN
            ALTER TABLE [dbo].[Users]
                ADD [AvatarUrl] nvarchar(max) NULL;
        END
        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[Users]', N'BackgroundUrl') IS NULL
        BEGIN
            ALTER TABLE [dbo].[Users]
                ADD [BackgroundUrl] nvarchar(max) NULL;
        END
        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[Users]', N'ProfileFrame') IS NULL
        BEGIN
            ALTER TABLE [dbo].[Users]
                ADD [ProfileFrame] int NOT NULL CONSTRAINT [DF_Users_ProfileFrame] DEFAULT (0);
        END
        -- Extended text formatting columns for PostContentBlocks.
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextBold') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextBold] bit NOT NULL CONSTRAINT [DF_PostContentBlocks_TextBold] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextItalic') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextItalic] bit NOT NULL CONSTRAINT [DF_PostContentBlocks_TextItalic] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextUnderline') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextUnderline] bit NOT NULL CONSTRAINT [DF_PostContentBlocks_TextUnderline] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextStrike') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextStrike] bit NOT NULL CONSTRAINT [DF_PostContentBlocks_TextStrike] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextSize') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextSize] int NOT NULL CONSTRAINT [DF_PostContentBlocks_TextSize] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextAlign') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextAlign] int NOT NULL CONSTRAINT [DF_PostContentBlocks_TextAlign] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextStyle') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextStyle] int NOT NULL CONSTRAINT [DF_PostContentBlocks_TextStyle] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TextColor') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TextColor] nvarchar(32) NOT NULL CONSTRAINT [DF_PostContentBlocks_TextColor] DEFAULT (N'');
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'SectionDisplayStyle') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [SectionDisplayStyle] int NOT NULL CONSTRAINT [DF_PostContentBlocks_SectionDisplayStyle] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'SectionLinkText') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [SectionLinkText] nvarchar(240) NOT NULL CONSTRAINT [DF_PostContentBlocks_SectionLinkText] DEFAULT (N'');
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'TemplateType') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [TemplateType] int NOT NULL CONSTRAINT [DF_PostContentBlocks_TemplateType] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'GalleryStyle') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [GalleryStyle] int NOT NULL CONSTRAINT [DF_PostContentBlocks_GalleryStyle] DEFAULT (0);
        END
        IF OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
           AND COL_LENGTH(N'[dbo].[PostContentBlocks]', N'GalleryCaption') IS NULL
        BEGIN
            ALTER TABLE [dbo].[PostContentBlocks]
                ADD [GalleryCaption] nvarchar(240) NOT NULL CONSTRAINT [DF_PostContentBlocks_GalleryCaption] DEFAULT (N'');
        END
        IF OBJECT_ID(N'[dbo].[PostGalleryImages]', N'U') IS NULL
           AND OBJECT_ID(N'[dbo].[PostContentBlocks]', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE [dbo].[PostGalleryImages] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [PostContentBlockId] uniqueidentifier NOT NULL,
                [ImagePath] nvarchar(max) NOT NULL DEFAULT N'',
                [Caption] nvarchar(240) NOT NULL DEFAULT N'',
                [Order] int NOT NULL DEFAULT 0,
                CONSTRAINT [FK_PostGalleryImages_PostContentBlocks]
                    FOREIGN KEY ([PostContentBlockId]) REFERENCES [dbo].[PostContentBlocks] ([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_PostGalleryImages_PostContentBlockId_Order]
                ON [dbo].[PostGalleryImages] ([PostContentBlockId], [Order]);
        END
        """);
    if (!db.Categories.Any())
    {
        db.Categories.Add(new Fandom_copy.Models.Category
        {
            Id = Guid.NewGuid(),
            Name = "General",
            Description = "Default category for community posts."
        });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();

bool IsOAuthProviderConfigured(string provider)
{
    var section = builder.Configuration.GetSection($"Authentication:{provider}");
    var clientId = provider == "Facebook" ? section["AppId"] : section["ClientId"];
    var clientSecret = provider == "Facebook" ? section["AppSecret"] : section["ClientSecret"];

    return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
}
