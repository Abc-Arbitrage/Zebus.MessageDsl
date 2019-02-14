namespace Abc.Zebus.MessageDsl.Ast
{
    public class MemberOptions : OptionsBase
    {
        public bool Proto { get; set; }
        public bool Mutable { get; set; }

        public MemberOptions Clone() => (MemberOptions)MemberwiseClone();
    }
}
