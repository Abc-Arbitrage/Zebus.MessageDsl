using System.Linq;
using System.Text.RegularExpressions;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis
{
    internal class AttributeInterpretor
    {
        private static readonly Regex _reProtoIncludeParams = new Regex(@"^\s*(?<tag>[0-9]+)\s*,\s*typeof\s*\(\s*(?<typeName>.+)\s*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ParsedContracts _contracts;

        public AttributeInterpretor(ParsedContracts contracts)
        {
            _contracts = contracts;
        }

        public void InterpretAttributes()
        {
            foreach (var messageDefinition in _contracts.Messages)
            {
                CheckIfTransient(messageDefinition);
                CheckIfRoutable(messageDefinition);

                foreach (var parameterDefinition in messageDefinition.Parameters)
                    ProcessProtoMemberAttribute(parameterDefinition);
            }
        }

        private void CheckIfRoutable(MessageDefinition message)
        {
            message.IsRoutable = !message.IsCustom && message.Attributes.HasAttribute(KnownTypes.RoutableAttribute);

            if (message.IsRoutable)
            {
                _contracts.ImportedNamespaces.Add("Abc.Zebus.Routing");

                foreach (var param in message.Parameters)
                {
                    var routingAttr = param.Attributes.Where(attr => Equals(attr.TypeName, KnownTypes.RoutingPositionAttribute)).ToList();

                    if (routingAttr.Count == 0)
                        continue;

                    if (routingAttr.Count == 1)
                    {
                        var attr = routingAttr.Single();

                        if (int.TryParse(attr.Parameters, out var routingPosition))
                            param.RoutingPosition = routingPosition;
                        else
                            _contracts.AddError(attr.ParseContext, "Invalid routing position: {0}", attr.Parameters);
                    }
                    else
                    {
                        _contracts.AddError(routingAttr.First().ParseContext, "Multiple routing positions are not allowed");
                    }
                }

                var routableParamCount = message.Parameters.Count(p => p.RoutingPosition != null);
                var isValidSequence = message.Parameters
                                             .Where(p => p.RoutingPosition != null)
                                             .OrderBy(p => p.RoutingPosition)
                                             .Select(p => p.RoutingPosition.GetValueOrDefault())
                                             .SequenceEqual(Enumerable.Range(1, routableParamCount));

                if (routableParamCount == 0)
                    _contracts.AddError(message.ParseContext, "A routable message must have parameters with routing positions");
                else if (!isValidSequence)
                    _contracts.AddError(message.ParseContext, "Routing positions must form a continuous sequence starting with 1");
            }
            else
            {
                var firstRoutingPositionAttr = message.Parameters
                                                      .SelectMany(p => p.Attributes)
                                                      .FirstOrDefault(attr => Equals(attr.TypeName, KnownTypes.RoutingPositionAttribute));

                if (firstRoutingPositionAttr != null)
                    _contracts.AddError(firstRoutingPositionAttr.ParseContext, "A non-routable message should not have RoutingPosition attributes");
            }
        }

        private static void CheckIfTransient(MessageDefinition message)
        {
            message.IsTransient = !message.IsCustom && message.Attributes.HasAttribute(KnownTypes.TransientAttribute);
        }

        private void ProcessProtoMemberAttribute(ParameterDefinition param)
        {
            var attr = param.Attributes.GetAttribute(KnownTypes.ProtoMemberAttribute);
            if (attr == null)
                return;

            if (string.IsNullOrWhiteSpace(attr.Parameters))
            {
                _contracts.AddError(attr.ParseContext, "The [{0}] attribute must have parameters", KnownTypes.ProtoMemberAttribute);
                return;
            }

            var match = Regex.Match(attr.Parameters, @"^\s*(?<nb>[0-9]+)\s*(?:,|$)");
            if (!match.Success || !int.TryParse(match.Groups["nb"].Value, out var tagNb))
            {
                _contracts.AddError(attr.ParseContext, "Invalid [{0}] parameters", KnownTypes.ProtoMemberAttribute);
                return;
            }

            if (param.Tag != 0)
            {
                _contracts.AddError(attr.ParseContext, "The parameter '{0}' already has an explicit tag ({1})", param.Name, param.Tag);
                return;
            }

            if (!AstValidator.IsValidTag(tagNb))
            {
                _contracts.AddError(attr.ParseContext, "Tag for parameter '{0}' is not within the valid range ({1})", param.Name, tagNb);
                return;
            }

            param.Tag = tagNb;
        }

        public static bool TryParseProtoInclude(AttributeDefinition attribute, out int tag, out TypeName messageType)
        {
            tag = 0;
            messageType = null!;

            if (!Equals(attribute?.TypeName, KnownTypes.ProtoIncludeAttribute))
                return false;

            var match = _reProtoIncludeParams.Match(attribute.Parameters ?? string.Empty);
            if (!match.Success)
                return false;

            if (!int.TryParse(match.Groups["tag"].Value, out tag))
                return false;

            messageType = match.Groups["typeName"].Value;
            return true;
        }
    }
}
