using Xunit;

namespace Workflow.IntegrationTests
{
    public static class SomeRecordTests
    {
        public sealed class ConstructorMethod
        {
            [Fact]
            public void ReturnsInstance()
            {
                var instance = new Workflow.SomeRecord(42);
                Assert.NotNull(instance);
            }
        }
    }
}
