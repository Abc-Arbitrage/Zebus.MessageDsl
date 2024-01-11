using System.Linq;
using System.Text.RegularExpressions;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis;

internal class AttributeInterpreter
{
    private static readonly Regex _reProtoIncludeParams = new(@"^\s*(?<tag>[0-9]+)\s*,\s*typeof\s*\(\s*(?<typeName>.+)\s*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex _reProtoReservedParams = new(@"^\s*(?<startTag>[0-9]+)(?:\s*,\s*(?<endTag>[0-9]+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ParsedContracts _contracts;

    public AttributeInterpreter(ParsedContracts contracts)
    {
        _contracts = contracts;
    }

    public void InterpretAttributes()
    {
        foreach (var message in _contracts.Messages)
        {
            CheckIfTransient(message);
            CheckIfRoutable(message);
            ProcessProtoReservedAttributes(message);

            foreach (var parameterDefinition in message.Parameters)
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

                switch (routingAttr)
                {
                    case []:
                        continue;

                    case [var attr]:
                        if (int.TryParse(attr.Parameters, out var routingPosition))
                            param.RoutingPosition = routingPosition;
                        else
                            _contracts.AddError(attr.ParseContext, $"Invalid routing position: {attr.Parameters}");
                        break;

                    case [var first, ..]:
                        _contracts.AddError(first.ParseContext, "Multiple routing positions are not allowed");
                        break;
                }
            }

            var routableParamCount = message.Parameters.Count(p => p.RoutingPosition != null);
            var isValidSequence = message.Parameters
                                         .Where(p => p.RoutingPosition != null)
                                         .OrderBy(p => p.RoutingPosition)
                                         .Select(p => p.RoutingPosition.GetValueOrDefault())
                                         .SequenceEqual(Enumerable.Range(1, routableParamCount));

            if (routableParamCount == 0)
                _contracts.AddError(message.ParseContext, "A routable message must have arguments with routing positions");
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

    private void ProcessProtoReservedAttributes(MessageDefinition message)
    {
        foreach (var attr in message.Attributes.GetAttributes(KnownTypes.ProtoReservedAttribute))
        {
            var errorContext = attr.ParseContext ?? message.ParseContext;

            if (string.IsNullOrWhiteSpace(attr.Parameters))
            {
                _contracts.AddError(errorContext, $"The [{KnownTypes.ProtoReservedAttribute}] attribute must have arguments");
                return;
            }

            var match = _reProtoReservedParams.Match(attr.Parameters);
            if (!match.Success || !int.TryParse(match.Groups["startTag"].Value, out var startTag))
            {
                _contracts.AddError(errorContext, $"Invalid [{KnownTypes.ProtoReservedAttribute}] arguments");
                return;
            }

            var endTag = startTag;

            if (match.Groups["endTag"].Success && !int.TryParse(match.Groups["endTag"].Value, out endTag))
            {
                _contracts.AddError(errorContext, $"Invalid [{KnownTypes.ProtoReservedAttribute}] arguments");
                return;
            }

            if (startTag > endTag)
            {
                _contracts.AddError(errorContext, $"Invalid [{KnownTypes.ProtoReservedAttribute}] tag range");
                return;
            }

            message.ReservedRanges.Add(new ReservationRange(startTag, endTag));
        }
    }

    private void ProcessProtoMemberAttribute(ParameterDefinition param)
    {
        var attr = param.Attributes.GetAttribute(KnownTypes.ProtoMemberAttribute);
        if (attr == null)
            return;

        if (string.IsNullOrWhiteSpace(attr.Parameters))
        {
            _contracts.AddError(attr.ParseContext, $"The [{KnownTypes.ProtoMemberAttribute}] attribute must have arguments");
            return;
        }

        var match = Regex.Match(attr.Parameters, @"^\s*(?<nb>[0-9]+)\s*(?:,|$)");
        if (!match.Success || !int.TryParse(match.Groups["nb"].Value, out var tagNb))
        {
            _contracts.AddError(attr.ParseContext, $"Invalid [{KnownTypes.ProtoMemberAttribute}] arguments");
            return;
        }

        if (param.Tag != 0)
        {
            _contracts.AddError(attr.ParseContext, $"The parameter '{param.Name}' already has an explicit tag ({param.Tag})");
            return;
        }

        if (!AstValidator.IsValidTag(tagNb))
        {
            _contracts.AddError(attr.ParseContext, $"Tag for parameter '{param.Name}' is not within the valid range ({tagNb})");
            return;
        }

        param.Tag = tagNb;
    }

    public static bool TryParseProtoInclude(AttributeDefinition? attribute, out int tag, out TypeName messageType)
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
