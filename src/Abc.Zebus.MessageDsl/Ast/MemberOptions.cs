using JetBrains.Annotations;

namespace Abc.Zebus.MessageDsl.Ast;

public class MemberOptions : OptionsBase
{
    public bool Proto { get; set; }
    public bool Mutable { get; set; }

    public bool Internal { get; set; }

    public bool Public
    {
        get => !Internal;
        [UsedImplicitly] set => Internal = !value;
    }

    public bool Nullable { get; set; }

    public AccessModifier GetAccessModifier()
        => Internal ? AccessModifier.Internal : AccessModifier.Public;

    public MemberOptions Clone()
        => (MemberOptions)MemberwiseClone();
}
