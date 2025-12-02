using Microsoft.VisualStudio.TestTools.UnitTesting;

// Configure test parallelization at class level to avoid resource conflicts
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
