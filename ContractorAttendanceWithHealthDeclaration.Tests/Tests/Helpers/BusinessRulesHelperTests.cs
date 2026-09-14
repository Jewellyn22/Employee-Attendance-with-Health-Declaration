using ContractorAttendanceWithHealthDeclaration.Helpers;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Helpers
{
    public class BusinessRulesHelperTests
    {
        #region IsNotAllowedToEnter

        [Theory]
        [InlineData(HealthConstants.StatusUnfit, HealthConstants.WaiverUnderstood, true)]
        [InlineData(HealthConstants.StatusUnfit, HealthConstants.WaiverNotUnderstood, true)]
        [InlineData(HealthConstants.StatusUnfit, null, true)]
        [InlineData(HealthConstants.StatusFit, HealthConstants.WaiverNotUnderstood, true)]
        [InlineData(HealthConstants.StatusFit, HealthConstants.WaiverUnderstood, false)]
        [InlineData(HealthConstants.StatusFit, null, false)]
        [InlineData(null, HealthConstants.WaiverNotUnderstood, false)]
        [InlineData(null, null, false)]
        // Case-insensitive since the raw-DB-value hardening: any casing blocks entry.
        [InlineData("fit", HealthConstants.WaiverNotUnderstood, true)]
        [InlineData("unfit", HealthConstants.WaiverUnderstood, true)]
        [InlineData(HealthConstants.StatusFit, "not_understood", true)]
        [InlineData("", HealthConstants.WaiverNotUnderstood, false)]
        public void IsNotAllowedToEnter_truth_table(string? health_status, string? waiver_consent, bool expected)
        {
            Assert.Equal(expected, BusinessRulesHelper.IsNotAllowedToEnter(health_status, waiver_consent));
        }

        #endregion

        #region IsWithinThreshold

        [Fact]
        public void Null_timestamp_is_outside_the_threshold()
        {
            Assert.False(BusinessRulesHelper.IsWithinThreshold(null, 30));
        }

        [Fact]
        public void Recent_timestamp_is_within_the_threshold()
        {
            Assert.True(BusinessRulesHelper.IsWithinThreshold(DateTime.Now.AddSeconds(-5), 30));
        }

        [Fact]
        public void Old_timestamp_is_outside_the_threshold()
        {
            Assert.False(BusinessRulesHelper.IsWithinThreshold(DateTime.Now.AddSeconds(-60), 30));
        }

        [Fact]
        public void Just_past_the_boundary_is_outside()
        {
            // Strict '<' comparison; test a hair past the boundary rather than exactly
            // at it to avoid timing flakiness.
            Assert.False(BusinessRulesHelper.IsWithinThreshold(DateTime.Now.AddSeconds(-30.5), 30));
        }

        [Fact]
        public void Just_inside_the_boundary_is_inside()
        {
            Assert.True(BusinessRulesHelper.IsWithinThreshold(DateTime.Now.AddSeconds(-29.5), 30));
        }

        // Pins current behavior: a future timestamp yields a negative diff, which is
        // always < threshold, so it is treated as "within" the debounce window.
        [Fact]
        public void Future_timestamp_is_treated_as_within_the_threshold()
        {
            Assert.True(BusinessRulesHelper.IsWithinThreshold(DateTime.Now.AddMinutes(5), 30));
        }

        [Fact]
        public void Zero_threshold_blocks_nothing_but_the_present()
        {
            Assert.False(BusinessRulesHelper.IsWithinThreshold(DateTime.Now.AddSeconds(-1), 0));
        }

        #endregion
    }
}
