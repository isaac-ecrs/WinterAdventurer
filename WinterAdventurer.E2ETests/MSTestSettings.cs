using Microsoft.VisualStudio.TestTools.UnitTesting;

// Configure test parallelization - disabled for E2E tests to avoid browser conflicts
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
