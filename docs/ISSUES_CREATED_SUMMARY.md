# GitHub Issues Created - Planning to Tracking Migration

**Date:** December 13, 2025
**Migration Status:** Complete

All pending items from TEST_COVERAGE_PLAN.md and ENHANCEMENTS.md have been converted to GitHub issues for better project tracking and visibility.

## Summary Statistics

- **Total Issues Created:** 20
- **Test-Related Issues:** 11 (from TEST_COVERAGE_PLAN.md + testing enhancements)
- **Enhancement Issues:** 9 (architecture, ALM, code quality)

## Issues by Priority

### High Priority (8 issues)

| Issue | Title | Type |
|-------|-------|------|
| #47 | Add Home.razor File Upload Workflow Tests | Testing |
| #48 | Add Home.razor Timeslot Management Tests | Testing |
| #49 | Add Home.razor Location Management Tests | Testing |
| #50 | Add Home.razor PDF Generation Tests | Testing |
| #51 | Add Home.razor Initialization & Lifecycle Tests | Testing |
| #56 | Expand PDF Content Validation Tests | Testing |
| #57 | Add CLI Integration Tests | Testing |

### Medium Priority (10 issues)

| Issue | Title | Type |
|-------|-------|------|
| #52 | Add TimeslotEditor Component Tests | Testing |
| #53 | Add IndividualScheduleGenerator Edge Case Tests | Testing |
| #54 | Add End-to-End Integration Tests | Testing |
| #55 | Configure Roslyn Analyzer Custom Severity Rules | DevOps |
| #58 | Implement Test Builders Pattern | Enhancement |
| #59 | Add Excel Schema Validation | Architecture |
| #60 | Extract PDF Configuration to Settings | Architecture |
| #61 | Add Parameterized Tests | Testing |

### Low Priority (2 issues)

| Issue | Title | Type |
|-------|-------|------|
| #62 | Add NuGet Package Caching to CI | DevOps |
| #63 | Add Repository Pattern for Data Access | Architecture |
| #64 | Add Caching Layer for Location Data | Enhancement |
| #65 | Add Performance Testing with BenchmarkDotNet | Testing |
| #66 | Add CodeQL Security Scanning | DevOps |

## Issues by Type

### Testing (11 issues)
#47, #48, #49, #50, #51, #52, #53, #54, #56, #57, #61, #65

### Architecture & Code Quality (5 issues)
#58, #59, #60, #63, #64

### DevOps / ALM (4 issues)
#55, #62, #65, #66

## Next Steps

1. **Review Issues:** Visit the [Issues page](https://github.com/isaac-ecrs/WinterAdventurer/issues) to review all created issues
2. **Prioritize:** Sort by priority label to focus work
3. **Track Progress:** Use GitHub Projects for milestone/sprint tracking
4. **Close or Won't Fix:** Issues can be closed if work is completed or prioritized differently

## Archived Documents

Original planning documents have been moved to `docs/archive/`:
- `docs/archive/TEST_COVERAGE_PLAN.md`
- `docs/archive/ENHANCEMENTS.md`
- `docs/archive/README.md` (explains archived files)

These preserve the detailed context and analysis from the original planning documents.

## Original Plan Status

### TEST_COVERAGE_PLAN.md
- **Status:** 15% Complete (only ExcelParser error recovery tests finished)
- **Blockers:** Private field access in Home.razor, constructor mocking complexity
- **Now Tracked In:** Issues #47-54

### ENHANCEMENTS.md
- **Status:** 60% Complete (12 items done, 8 pending, 4 optional)
- **Completed:** Service architecture, domain exceptions, code coverage, EditorConfig, Dependabot, service tests
- **Now Tracked In:** Issues #55-66

## Reference

For detailed information about each issue, visit the specific issue on GitHub. Each issue contains:
- Detailed context and background
- Acceptance criteria
- Estimated effort/impact
- Related files
- Implementation notes

See `docs/archive/` for the original comprehensive planning documents with full context.
