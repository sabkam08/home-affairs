using FluentAssertions;
using HomeAffairs.Domain.SmartId;

namespace HomeAffairs.UnitTests.SmartId;

public sealed class SmartIdApplicationTypeTests
{
    [Theory]
    [InlineData("SMART_ID_NEW")]
    [InlineData("SMART_ID_REPLACEMENT_LOST")]
    [InlineData("SMART_ID_REPLACEMENT_STOLEN")]
    [InlineData("SMART_ID_REPLACEMENT_DAMAGED")]
    [InlineData("SMART_ID_CORRECTION")]
    public void Smart_id_type_should_be_recognized(
        string applicationType)
    {
        SmartIdApplicationTypes
            .IsSmartId(applicationType)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData("BIRTH_REGISTRATION")]
    [InlineData("BIRTH_CERTIFICATE_REISSUE")]
    public void Birth_type_should_not_be_classified_as_smart_id(
        string applicationType)
    {
        SmartIdApplicationTypes
            .IsSmartId(applicationType)
            .Should()
            .BeFalse();
    }
}
