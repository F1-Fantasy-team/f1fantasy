using Xunit;

namespace F1Fantasy.Tests;

/// <summary>
/// Collection definition to ensure tests run sequentially to avoid API rate limits
/// </summary>
[CollectionDefinition("Sequential Integration Tests", DisableParallelization = true)]
public class SequentialTestCollectionDefinition
{
}
