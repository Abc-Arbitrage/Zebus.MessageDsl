using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis
{
    internal class ContractsEnhancer
    {
        private static readonly TypeName _protoMapAttributeType = new TypeName("ProtoMap");

        private readonly ParsedContracts _contracts;

        public ContractsEnhancer(ParsedContracts contracts)
        {
            _contracts = contracts;
        }

        public void Process()
        {
            foreach (var messageDefinition in _contracts.Messages)
                ProcessMessage(messageDefinition);
        }

        private static void ProcessMessage(MessageDefinition message)
        {
            foreach (var parameterDefinition in message.Parameters)
                ProcessParameter(parameterDefinition);
        }

        private static void ProcessParameter(ParameterDefinition parameter)
        {
            if (parameter.Type.IsDictionary && !parameter.Attributes.HasAttribute(_protoMapAttributeType))
                parameter.Attributes.Add(new AttributeDefinition(_protoMapAttributeType, "DisableMap = true")); // https://github.com/mgravell/protobuf-net/issues/379
        }
    }
}
