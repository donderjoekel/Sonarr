using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.FakeTorrent;

public class FakeTorrentSettingsValidator : AbstractValidator<FakeTorrentSettings>
{
    public FakeTorrentSettingsValidator()
    {
    }
}

public class FakeTorrentSettings : IProviderConfig
{
    private static readonly FakeTorrentSettingsValidator Validator = new FakeTorrentSettingsValidator();

    [FieldDefinition(1, Label = "Dummy", Hidden = HiddenType.HiddenIfNotSet, Type = FieldType.Checkbox)]
    public bool Dummy { get; set; }

    public NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
