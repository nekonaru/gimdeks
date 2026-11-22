using GimDeks.Excel;
using Xunit;

namespace GimDeks.Tests.Excel;

public sealed class RingkasanComparisonTests
{
    [Fact]
    public void VerificationWithoutExpectedRowsDoesNotReportMatch()
    {
        var comparison = new RingkasanComparison
        {
            Rows =
            {
                new RingkasanRow
                {
                    Label = "HDD Eksternal 1",
                    StorageSheet = "HDD Eksternal 1",
                    ExpectedCount = null,
                    ActualCount = 12,
                    ActualGb = 100,
                },
            },
        };

        Assert.False(comparison.AllMatch);
    }

    [Fact]
    public void VerificationWithMatchingExpectedRowsReportsMatch()
    {
        var comparison = new RingkasanComparison
        {
            Rows =
            {
                new RingkasanRow
                {
                    Label = "HDD Eksternal 1",
                    StorageSheet = "HDD Eksternal 1",
                    ExpectedCount = 12,
                    ExpectedGb = 100,
                    ActualCount = 12,
                    ActualGb = 100,
                },
            },
        };

        Assert.True(comparison.AllMatch);
    }
}
