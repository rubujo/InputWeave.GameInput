using BenchmarkDotNet.Running;
using InputWeave.GameInput.Benchmarks;

BenchmarkRunner.Run<ReadingBenchmarks>(args: args);
