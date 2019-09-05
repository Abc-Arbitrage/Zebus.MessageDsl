namespace Abc.Zebus.MessageDsl.Ast
{
    public class MemberOptions : OptionsBase
    {
        public bool Proto { get; set; }
        public bool Mutable { get; set; }

        public bool Internal { get; set; }

        public bool Public
        {
            get => !Internal;
            set => Internal = !value;
        }

        public MemberOptions Clone() => (MemberOptions)MemberwiseClone();
    }
}
