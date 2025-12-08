# WinterAdventurer Enhancement Recommendations

A comprehensive review of the WinterAdventurer codebase with suggested improvements for modern best practices, testing coverage, ALM (Application Lifecycle Management), and architecture.

---

## Executive Summary

**Overall Assessment: 4.0/5.0 - Good, Well-Maintained Codebase**

WinterAdventurer demonstrates solid software engineering fundamentals with a notable innovation: the **schema-driven Excel parsing architecture**. The codebase is well-organized, uses modern .NET 10 practices, and has comprehensive documentation.

### Strengths
- **Schema-driven design**: Adapts to different Excel formats without code changes
- **Clean separation of concerns**: Library, CLI, and Web layers are properly isolated
- **Excellent ALM practices**: Automated CI/CD, conventional commits, multi-platform releases
- **Comprehensive documentation**: README, CLAUDE.md, and inline documentation
- **Modern .NET stack**: Nullable reference types, async/await, EF Core

### Key Improvement Areas
- **Code organization**: ExcelUtilities class (1,731 LOC) needs splitting
- **Testing coverage**: Gaps in PDF validation, Blazor UI, and CLI testing
- **Static analysis**: No Roslyn analyzers or code coverage tracking
- **Error handling**: Generic exceptions, silent failures need addressing

---

## Table of Contents

1. [Architecture Improvements](#1-architecture-improvements)
2. [Testing Improvements](#2-testing-improvements)
3. [ALM and DevOps Improvements](#3-alm-and-devops-improvements)
4. [Code Quality Improvements](#4-code-quality-improvements)
5. [Security Considerations](#5-security-considerations)
6. [Documentation Improvements](#6-documentation-improvements)
7. [Implementation Roadmap](#7-implementation-roadmap)

---

## 1. Architecture Improvements

### 1.1 Split ExcelUtilities (High Priority)

**Current State**: `ExcelUtilities.cs` is 1,731 lines mixing Excel parsing, PDF generation, and business logic.

**Problem**: Violates Single Responsibility Principle, difficult to test and maintain.

**Recommended Refactoring**:

```
WinterAdventurer.Library/
├── Import/
│   ├── IExcelImporter.cs          # Interface for Excel import
│   ├── ExcelImporter.cs           # Excel file parsing
│   ├── AttendeeLoader.cs          # Attendee parsing from ClassSelection sheet
│   └── WorkshopCollector.cs       # Workshop aggregation from period sheets
├── Pdf/
│   ├── IPdfGenerator.cs           # Interface for PDF generation
│   ├── PdfGenerator.cs            # Main PDF orchestrator
│   ├── RosterRenderer.cs          # Workshop roster PDF sections
│   ├── ScheduleRenderer.cs        # Individual schedule PDF sections
│   └── MasterScheduleRenderer.cs  # Master schedule PDF sections
├── Configuration/
│   ├── PdfLayoutConfig.cs         # Configurable layout settings
│   └── PdfTheme.cs                # Font/color theming
└── ExcelUtilities.cs              # Facade for backward compatibility
```

**Benefits**:
- Each class has single responsibility (~200-400 LOC each)
- PDF generation testable in isolation
- Layout configuration injectable
- Cleaner dependency injection

### 1.2 Introduce Domain-Specific Exceptions (Medium Priority)

**Current State**: Uses generic `InvalidOperationException`, `InvalidDataException`.

**Recommended Exceptions**:

```csharp
// WinterAdventurer.Library/Exceptions/
public class SchemaValidationException : Exception
{
    public string SchemaPath { get; }
    public string? FieldName { get; }
}

public class ExcelParsingException : Exception
{
    public string SheetName { get; }
    public int? RowNumber { get; }
    public string? ColumnName { get; }
}

public class WorkshopAggregationException : Exception
{
    public string WorkshopKey { get; }
}

public class PdfGenerationException : Exception
{
    public string Section { get; } // "Roster", "Schedule", "MasterSchedule"
}
```

**Benefits**:
- More precise error handling in calling code
- Better logging with contextual information
- Clearer API contract

### 1.3 Add Repository Pattern for Data Access (Medium Priority)

**Current State**: `LocationService` accesses `DbContext` directly.

**Recommended Structure**:

```csharp
// Generic repository interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}

// Implementation with EF Core
public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;
    // ...
}

// Unit of Work for transactions
public interface IUnitOfWork : IDisposable
{
    IRepository<Location> Locations { get; }
    IRepository<Tag> Tags { get; }
    IRepository<WorkshopLocationMapping> WorkshopMappings { get; }
    Task<int> SaveChangesAsync();
}
```

**Benefits**:
- Consistent data access patterns
- Transaction support via Unit of Work
- Easier mocking in tests
- Separation of concerns

### 1.4 Make Library Fully DI-Ready (Medium Priority)

**Current State**: CLI uses manual instantiation; Library partially supports DI.

**Recommended Changes**:

```csharp
// WinterAdventurer.Library/DependencyInjection.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWinterAdventurerLibrary(
        this IServiceCollection services)
    {
        services.AddScoped<IExcelImporter, ExcelImporter>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();
        services.AddScoped<ISchemaProvider, EmbeddedSchemaProvider>();
        services.AddScoped<IRosterRenderer, RosterRenderer>();
        services.AddScoped<IScheduleRenderer, ScheduleRenderer>();
        return services;
    }
}

// CLI Program.cs
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddWinterAdventurerLibrary();
        services.AddLogging(builder => builder.AddConsole());
    })
    .Build();

var importer = host.Services.GetRequiredService<IExcelImporter>();
```

**Benefits**:
- Consistent DI across CLI, Web, and Tests
- Easier testing with mocked dependencies
- Configurable services per environment

### 1.5 Add Caching for Location Data (Low Priority)

**Current State**: Locations queried repeatedly without caching.

**Recommended Implementation**:

```csharp
public class CachedLocationService : ILocationService
{
    private readonly ILocationService _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IEnumerable<Location>> GetAllLocationsAsync()
    {
        return await _cache.GetOrCreateAsync("locations:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return await _inner.GetAllLocationsAsync();
        });
    }

    public async Task InvalidateCacheAsync()
    {
        _cache.Remove("locations:all");
    }
}
```

**Benefits**:
- Reduced database queries
- Faster UI response times
- Automatic invalidation on updates

---

## 2. Testing Improvements

### 2.1 Current Testing Maturity

| Aspect | Current Score | Notes |
|--------|---------------|-------|
| Unit Tests | 4/5 | Excellent coverage of core logic |
| Integration Tests | 2/5 | Limited workflow testing |
| E2E Tests | 3/5 | Playwright setup, but incomplete |
| Component Tests | 2/5 | Basic bUnit tests only |
| Performance Tests | 0/5 | None |

### 2.2 Priority 1: Critical Testing Gaps (High Priority)

#### A. Expand PDF Content Validation

```csharp
[TestClass]
public class PdfRosterTests
{
    [TestMethod]
    public void Roster_WithMultipleWorkshops_HasCorrectPageBreaks()
    {
        // Arrange: 50 workshops
        // Act: Generate PDF
        // Assert: Each workshop starts on new section
    }

    [TestMethod]
    public void Roster_WithLongNames_UsesSmallerFont()
    {
        // Test adaptive font sizing for names > 25 chars
    }

    [TestMethod]
    public void Roster_BackupParticipants_ShowChoiceNumbers()
    {
        // Assert backup section shows "(2nd choice)", "(3rd choice)"
    }
}
```

#### B. Add CLI End-to-End Tests

```csharp
[TestClass]
public class CliIntegrationTests
{
    [TestMethod]
    public async Task Cli_ValidExcel_GeneratesPdfOutput()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var excelPath = CreateTestExcelFile(tempDir);

        // Act
        var result = await RunCliAsync($"\"{excelPath}\"");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(File.Exists(Path.ChangeExtension(excelPath, ".pdf")));
    }

    [TestMethod]
    public async Task Cli_InvalidFile_ReturnsErrorCode()
    {
        var result = await RunCliAsync("nonexistent.xlsx");
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.StdErr, "File not found");
    }
}
```

#### C. Add Blazor Component Interaction Tests

```csharp
[TestClass]
public class WorkshopCardInteractionTests : BunitTestContext
{
    [TestMethod]
    public void WorkshopCard_ClickEdit_ShowsEditDialog()
    {
        // Arrange
        var workshop = CreateTestWorkshop();
        var cut = RenderComponent<WorkshopCard>(
            parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.IsFirstCard, true));

        // Act
        cut.Find(".edit-button").Click();

        // Assert
        Assert.IsTrue(cut.Instance.IsEditDialogOpen);
    }

    [TestMethod]
    public void WorkshopCard_SaveLocation_UpdatesDisplay()
    {
        // Test location assignment persists
    }
}
```

### 2.3 Priority 2: Testing Infrastructure (Medium Priority)

#### A. Introduce Test Builders

```csharp
public class WorkshopBuilder
{
    private string _name = "Test Workshop";
    private string _leader = "Test Leader";
    private Period _period = Period.MorningFirstPeriod;
    private List<WorkshopSelection> _selections = new();

    public WorkshopBuilder WithName(string name) { _name = name; return this; }
    public WorkshopBuilder WithLeader(string leader) { _leader = leader; return this; }
    public WorkshopBuilder WithPeriod(Period period) { _period = period; return this; }
    public WorkshopBuilder WithSelections(int count)
    {
        for (int i = 0; i < count; i++)
            _selections.Add(new WorkshopSelection
            {
                ClassSelectionId = $"SEL-{i:D4}",
                Attendee = new Attendee { FirstName = $"First{i}", LastName = $"Last{i}" },
                ChoiceNumber = 1
            });
        return this;
    }

    public Workshop Build() => new()
    {
        Name = _name,
        Leader = _leader,
        Period = _period,
        Selections = _selections
    };
}

// Usage
var workshop = new WorkshopBuilder()
    .WithName("Woodworking")
    .WithSelections(15)
    .Build();
```

#### B. Add Parameterized Tests

```csharp
[DataTestMethod]
[DataRow("Morning First Period", "Morning First Period")]
[DataRow("MorningFirstPeriod", "Morning First Period")]
[DataRow("EveningActivity", "Evening Activity")]
public void Period_DisplayName_FormatsCorrectly(string input, string expected)
{
    var period = Enum.Parse<Period>(input.Replace(" ", ""));
    Assert.AreEqual(expected, period.ToDisplayName());
}
```

#### C. Introduce Moq for Complex Mocking

```xml
<!-- WinterAdventurer.Test.csproj -->
<PackageReference Include="Moq" Version="4.20.70" />
```

```csharp
[TestMethod]
public async Task PdfGenerator_UsesLocationService()
{
    // Arrange
    var mockLocationService = new Mock<ILocationService>();
    mockLocationService
        .Setup(s => s.GetWorkshopLocationAsync("Woodworking"))
        .ReturnsAsync(new Location { Name = "Building A" });

    var generator = new PdfGenerator(mockLocationService.Object, ...);

    // Act
    await generator.GenerateRosterAsync(workshops);

    // Assert
    mockLocationService.Verify(s => s.GetWorkshopLocationAsync(It.IsAny<string>()), Times.AtLeastOnce);
}
```

### 2.4 Priority 3: Advanced Testing (Low Priority)

#### A. Performance Testing with BenchmarkDotNet

```csharp
[MemoryDiagnoser]
public class ExcelImportBenchmarks
{
    private Stream _smallExcel;   // 50 attendees
    private Stream _mediumExcel;  // 500 attendees
    private Stream _largeExcel;   // 2000 attendees

    [Benchmark]
    public List<Workshop> ImportSmall() => _importer.Import(_smallExcel);

    [Benchmark]
    public List<Workshop> ImportMedium() => _importer.Import(_mediumExcel);

    [Benchmark]
    public List<Workshop> ImportLarge() => _importer.Import(_largeExcel);
}
```

#### B. Accessibility Testing for PDFs

```csharp
[TestMethod]
public void Pdf_HasAccessibleTextContent()
{
    // Verify PDFs are screen-reader compatible
    // Check text extraction works for all content
}
```

---

## 3. ALM and DevOps Improvements

### 3.1 Current ALM Maturity: 4.2/5

| Aspect | Score | Status |
|--------|-------|--------|
| CI/CD Pipeline | 4/5 | Good |
| Version Control | 5/5 | Excellent |
| Release Management | 5/5 | Excellent |
| Code Coverage | 0/5 | Missing |
| Static Analysis | 1/5 | Minimal |
| Dependency Management | 3/5 | Manual |

### 3.2 Add Code Coverage Tracking (High Priority)

**Add to `ci.yml`:**

```yaml
- name: Run Tests with Coverage
  run: |
    dotnet test \
      --no-build \
      --configuration Release \
      --filter "FullyQualifiedName!~E2ETests" \
      /p:CollectCoverage=true \
      /p:CoverletOutputFormat=cobertura \
      /p:CoverletOutput=./coverage/

- name: Upload Coverage to Codecov
  uses: codecov/codecov-action@v4
  with:
    file: ./WinterAdventurer.Test/coverage/coverage.cobertura.xml
    fail_ci_if_error: false

- name: Add Coverage Badge
  uses: irongut/CodeCoverageSummary@v1.3.0
  with:
    filename: ./WinterAdventurer.Test/coverage/coverage.cobertura.xml
    badge: true
    output: both
```

**Target Coverage**: 70% minimum, 80% target

### 3.3 Add EditorConfig for Consistent Formatting (High Priority)

Create `.editorconfig` in project root:

```editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.cs]
# Naming conventions
dotnet_naming_rule.private_fields_should_be_camel_case.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style = camel_case_underscore

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_underscore.capitalization = camel_case
dotnet_naming_style.camel_case_underscore.required_prefix = _

# Code style
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_prefer_braces = true:warning
csharp_style_expression_bodied_methods = when_on_single_line:suggestion

# Formatting
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true

[*.{json,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

### 3.4 Add Roslyn Analyzers (Medium Priority)

**Add to all `.csproj` files:**

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

**Configure severity in `.editorconfig`:**

```editorconfig
# Security
dotnet_diagnostic.CA2100.severity = error   # SQL injection
dotnet_diagnostic.CA3001.severity = error   # SQL injection
dotnet_diagnostic.CA5350.severity = error   # Weak crypto

# Performance
dotnet_diagnostic.CA1802.severity = warning # Use const
dotnet_diagnostic.CA1805.severity = warning # Default initializers
dotnet_diagnostic.CA1822.severity = suggestion # Can be static

# Maintainability
dotnet_diagnostic.CA1501.severity = warning # Avoid excessive inheritance
dotnet_diagnostic.CA1502.severity = warning # Avoid excessive complexity
```

### 3.5 Add Dependabot for Dependency Updates (Medium Priority)

Create `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 5
    allow:
      - dependency-type: "all"
    ignore:
      - dependency-name: "*"
        update-types: ["version-update:semver-major"]
    commit-message:
      prefix: "chore(deps)"

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

### 3.6 Add Branch Protection Rules (Medium Priority)

Configure in GitHub repository settings:

- **Require pull request reviews**: 1 approval minimum
- **Require status checks**: CI must pass
- **Require branches to be up to date**: Before merging
- **Restrict force pushes**: Prevent history rewriting on master

### 3.7 Add NuGet Package Caching (Low Priority)

**Update `ci.yml`:**

```yaml
- name: Cache NuGet Packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

### 3.8 Add CodeQL Security Scanning (Low Priority)

Create `.github/workflows/codeql.yml`:

```yaml
name: CodeQL Analysis

on:
  push:
    branches: [master]
  pull_request:
    branches: [master]
  schedule:
    - cron: '0 6 * * 1'  # Weekly on Mondays

jobs:
  analyze:
    runs-on: ubuntu-latest
    permissions:
      security-events: write

    steps:
      - uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Build
        run: dotnet build --configuration Release

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
```

---

## 4. Code Quality Improvements

### 4.1 Improve Error Handling Strategy (High Priority)

**Current Issues**:
- Silent failures (logs warning, continues processing)
- Users may not notice incomplete imports

**Recommended Changes**:

```csharp
public class ImportResult
{
    public List<Workshop> Workshops { get; init; } = new();
    public List<ImportError> Errors { get; init; } = new();
    public bool HasErrors => Errors.Any();
    public int SkippedRows => Errors.Count(e => e.Severity == ErrorSeverity.Warning);
}

public class ImportError
{
    public string SheetName { get; init; }
    public int RowNumber { get; init; }
    public string Message { get; init; }
    public ErrorSeverity Severity { get; init; }
}

// Usage
var result = await importer.ImportAsync(stream);
if (result.HasErrors)
{
    _logger.LogWarning("Import completed with {ErrorCount} errors", result.Errors.Count);
    // Show user warning in UI
}
```

### 4.2 Add Schema Validation Before Processing (Medium Priority)

```csharp
public class SchemaValidator
{
    public ValidationResult ValidateExcelAgainstSchema(
        ExcelPackage package, EventSchema schema)
    {
        var errors = new List<string>();

        // Check required sheets exist
        foreach (var periodSheet in schema.PeriodSheets)
        {
            if (package.Workbook.Worksheets[periodSheet.SheetName] == null)
                errors.Add($"Missing required sheet: {periodSheet.SheetName}");
        }

        // Check required columns exist
        foreach (var column in schema.ClassSelectionSheet.Columns)
        {
            if (!SheetHasColumn(package, column))
                errors.Add($"Missing required column: {column.Key}");
        }

        return new ValidationResult(errors);
    }
}
```

### 4.3 Extract PDF Layout Configuration (Medium Priority)

```csharp
// Move from static constants to injectable configuration
public class PdfLayoutOptions
{
    public float PageMargin { get; set; } = 0.5f;
    public float HeaderFontSize { get; set; } = 16f;
    public float BodyFontSize { get; set; } = 10f;
    public string HeaderFont { get; set; } = "Oswald";
    public string BodyFont { get; set; } = "NotoSans";
    public int MaxNameLength { get; set; } = 25;
}

// Load from appsettings.json
builder.Services.Configure<PdfLayoutOptions>(
    builder.Configuration.GetSection("PdfLayout"));
```

### 4.4 Reduce Temp File Usage (Low Priority)

**Current Issue**: Logo and facility map written to temp files for MigraDoc.

**Recommended Approach**:
```csharp
// Use MigraDoc's stream-based image loading where possible
// Or implement proper cleanup in finally blocks
public void Dispose()
{
    if (File.Exists(_tempLogoPath))
        File.Delete(_tempLogoPath);
    if (File.Exists(_tempMapPath))
        File.Delete(_tempMapPath);
}
```

---

## 5. Security Considerations

### 5.1 Input Validation (Medium Priority)

Add validation for user-provided data:

```csharp
public static class InputValidator
{
    public static string SanitizeWorkshopName(string name)
    {
        // Remove potential HTML/script injection
        return HttpUtility.HtmlEncode(name?.Trim() ?? string.Empty);
    }

    public static bool IsValidFilePath(string path)
    {
        // Prevent path traversal attacks
        var fullPath = Path.GetFullPath(path);
        var baseDir = Path.GetFullPath(AppContext.BaseDirectory);
        return fullPath.StartsWith(baseDir);
    }
}
```

### 5.2 File Upload Security (Low Priority)

```csharp
// In Blazor file upload handler
private async Task HandleFileUpload(InputFileChangeEventArgs e)
{
    var file = e.File;

    // Validate file extension
    if (!file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
    {
        ShowError("Only .xlsx files are supported");
        return;
    }

    // Validate file size (e.g., max 10MB)
    if (file.Size > 10 * 1024 * 1024)
    {
        ShowError("File size exceeds 10MB limit");
        return;
    }

    // Validate content type
    if (file.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
    {
        ShowError("Invalid file format");
        return;
    }
}
```

### 5.3 Consider Authentication (Future Enhancement)

For multi-user scenarios:

```csharp
// Add to Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
```

---

## 6. Documentation Improvements

### 6.1 Add Architecture Decision Records (ADRs)

Create `docs/adr/` directory:

```markdown
# ADR-001: Schema-Driven Excel Parsing

## Status
Accepted

## Context
Excel files from event organizers vary year-to-year with different column names.

## Decision
Use JSON schema files to define column mappings with pattern matching support.

## Consequences
- Positive: New events can be supported without code changes
- Positive: Column name variations (2024 vs 2025) handled automatically
- Negative: Schema validation errors may be confusing for users
```

### 6.2 Add JSON Schema for Event Configuration

Create `EventSchemas/schema.json`:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "WinterAdventurer Event Schema",
  "type": "object",
  "required": ["classSelectionSheet", "periodSheets", "totalDays"],
  "properties": {
    "classSelectionSheet": {
      "type": "object",
      "properties": {
        "sheetName": { "type": "string" },
        "columns": { "$ref": "#/definitions/columnMap" }
      }
    },
    "periodSheets": {
      "type": "array",
      "items": { "$ref": "#/definitions/periodSheet" }
    },
    "totalDays": { "type": "integer", "minimum": 1 }
  }
}
```

### 6.3 Add Inline XML Documentation (Low Priority)

For public APIs that are currently undocumented:

```csharp
/// <summary>
/// Imports workshop and attendee data from an Excel file.
/// </summary>
/// <param name="stream">The Excel file stream (.xlsx format)</param>
/// <returns>
/// A list of <see cref="Workshop"/> objects with aggregated selections.
/// </returns>
/// <exception cref="SchemaValidationException">
/// Thrown when the Excel file doesn't match the expected schema.
/// </exception>
/// <exception cref="ExcelParsingException">
/// Thrown when a specific row or column cannot be parsed.
/// </exception>
public async Task<List<Workshop>> ImportAsync(Stream stream)
```

---

## 7. Implementation Roadmap

### Phase 1: Quick Wins (1-2 days effort)

| Item | Priority | Effort | Impact |
|------|----------|--------|--------|
| Add EditorConfig | High | Low | Medium |
| Add Roslyn Analyzers | Medium | Low | Medium |
| Add Dependabot | Medium | Low | Low |
| Add NuGet caching to CI | Low | Low | Low |

### Phase 2: Testing Foundation (3-5 days effort)

| Item | Priority | Effort | Impact |
|------|----------|--------|--------|
| Add code coverage to CI | High | Medium | High |
| Create test builders | Medium | Medium | Medium |
| Add parameterized tests | Medium | Low | Medium |
| Add CLI integration tests | High | Medium | High |

### Phase 3: Architecture Refactoring (5-10 days effort)

| Item | Priority | Effort | Impact |
|------|----------|--------|--------|
| Split ExcelUtilities | High | High | High |
| Add domain exceptions | Medium | Medium | Medium |
| Add schema validation | Medium | Medium | High |
| Extract PDF configuration | Medium | Medium | Medium |

### Phase 4: Advanced Improvements (Optional)

| Item | Priority | Effort | Impact |
|------|----------|--------|--------|
| Add Repository pattern | Low | High | Medium |
| Add caching layer | Low | Medium | Low |
| Performance testing | Low | Medium | Low |
| Security scanning | Low | Low | Medium |

---

## Summary of Key Recommendations

### Must-Have (Before Next Release)
1. **Add code coverage tracking** - Critical for maintaining quality
2. **Add EditorConfig** - Ensure consistent code style
3. **Expand PDF tests** - Core functionality needs better validation

### Should-Have (Next Development Cycle)
1. **Split ExcelUtilities** - Improve maintainability
2. **Add domain exceptions** - Better error handling
3. **Add CLI tests** - Complete test coverage
4. **Add Dependabot** - Automate dependency updates

### Nice-to-Have (Future Improvements)
1. **Repository pattern** - Cleaner data access
2. **Caching layer** - Performance optimization
3. **Performance tests** - Benchmarking
4. **CodeQL scanning** - Security analysis

---

*Generated: December 2024*
*Codebase Version: .NET 10.0*
