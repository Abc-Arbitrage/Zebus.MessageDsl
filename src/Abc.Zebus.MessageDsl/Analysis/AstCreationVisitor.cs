﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Dsl;
using Abc.Zebus.MessageDsl.Support;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using static Abc.Zebus.MessageDsl.Dsl.MessageContractsParser;

namespace Abc.Zebus.MessageDsl.Analysis;

[SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable")]
internal class AstCreationVisitor : MessageContractsBaseVisitor<AstNode?>
{
    private readonly ParsedContracts _contracts;
    private readonly HashSet<string> _definedContractOptions = new(StringComparer.OrdinalIgnoreCase);

    private bool _hasDefinitions;
    private MessageDefinition? _currentMessage;
    private ParameterDefinition? _currentParameter;
    private AttributeSet? _currentAttributeSet;
    private AttributeTarget _currentAttributeTarget;
    private MemberOptions _currentMemberOptions;

    public AstCreationVisitor(ParsedContracts contracts)
    {
        _contracts = contracts;
        _currentMemberOptions = new MemberOptions();
    }

    public override AstNode? VisitPragmaDefinition(PragmaDefinitionContext context)
    {
        var pragmaName = context.name?.token?.Text;

        if (string.IsNullOrEmpty(pragmaName))
        {
            _contracts.AddError(context, "Missing pragma name");
            return null;
        }

        var optionDescriptor = _contracts.Options.GetOptionDescriptor(pragmaName);
        if (optionDescriptor != null)
        {
            if (!_definedContractOptions.Add(pragmaName))
            {
                _contracts.AddError(context.name, $"Duplicate file-level pragma: {pragmaName}");
                return null;
            }

            if (_hasDefinitions)
                _contracts.AddError(context, $"File-level pragma {pragmaName} should be set at the top of the file");
        }
        else
        {
            _currentMemberOptions = _currentMemberOptions.Clone();
            optionDescriptor = _currentMemberOptions.GetOptionDescriptor(pragmaName);

            if (optionDescriptor == null)
            {
                _contracts.AddError(context.name, $"Unknown pragma: '{pragmaName}'");
                return null;
            }
        }

        pragmaName = optionDescriptor.Name;

        var value = context._valueTokens.Count > 0
            ? context._valueTokens.First().token.GetFullTextUntil(context._valueTokens.Last().token)
            : null;

        if (value == null)
        {
            if (optionDescriptor.IsBoolean)
            {
                value = context.not == null ? "true" : "false";
            }
            else
            {
                _contracts.AddError(context, $"Pragma {pragmaName} expects a value");
                return null;
            }
        }

        if (!optionDescriptor.SetValue(value))
            _contracts.AddError(context, $"Invalid option value: '{value}' for {pragmaName}");

        return null;
    }

    public override AstNode? VisitUsingDefinition(UsingDefinitionContext context)
    {
        if (_hasDefinitions)
            _contracts.AddError(context, "using clauses should be set at the top of the file");

        var ns = context.@namespace()?.GetText();

        if (!string.IsNullOrEmpty(ns))
            _contracts.ImportedNamespaces.Add(ns);

        return null;
    }

    public override AstNode? VisitNamespaceDefinition(NamespaceDefinitionContext context)
    {
        if (_contracts.ExplicitNamespace)
        {
            _contracts.AddError(context, "The namespace has already been set");
            return null;
        }

        if (_hasDefinitions)
            _contracts.AddError(context, "The namespace should be set at the top of the file");

        var ns = context.name?.GetText();

        if (string.IsNullOrEmpty(ns))
        {
            _contracts.AddError(context, "Missing namespace name");
            return null;
        }

        _contracts.Namespace = ns;
        _contracts.ExplicitNamespace = true;
        return null;
    }

    public override AstNode? VisitEnumDefinition(EnumDefinitionContext context)
    {
        _hasDefinitions = true;

        var enumDef = new EnumDefinition
        {
            ParseContext = context,
            Name = GetId(context.name),
            Options = _currentMemberOptions
        };

        var accessModifier = context.accessModifier();
        if (accessModifier != null)
            ProcessTypeModifiers(enumDef, [accessModifier.type]);

        ProcessAttributes(enumDef.Attributes, context.attributes());

        if (context.underlyingType != null)
            enumDef.UnderlyingType = context.underlyingType.GetText();

        foreach (var enumMemberContext in context.enumMember())
        {
            if (enumMemberContext.discard is not null)
            {
                enumDef.Members.Add(
                    new EnumMemberDefinition
                    {
                        ParseContext = enumMemberContext,
                        Name = "_",
                        IsDiscarded = true
                    }
                );

                continue;
            }

            var memberDef = new EnumMemberDefinition
            {
                ParseContext = enumMemberContext,
                Name = GetId(enumMemberContext.name),
                Value = enumMemberContext.value?.GetFullText()
            };

            ProcessAttributes(memberDef.Attributes, enumMemberContext.attributes());

            enumDef.Members.Add(memberDef);
        }

        _contracts.Enums.Add(enumDef);
        return enumDef;
    }

    public override AstNode? VisitMessageDefinition(MessageDefinitionContext context)
    {
        var message = new MessageDefinition
        {
            IsCustom = context.customModifier != null
        };

        ProcessMessage(message, context);
        return message;
    }

    public override AstNode? VisitParameterList(ParameterListContext context)
    {
        foreach (var param in context.parameterDefinition().Select(Visit).OfType<ParameterDefinition>())
            _currentMessage!.Parameters.Add(param);

        return null;
    }

    public override AstNode? VisitParameterDefinition(ParameterDefinitionContext context)
    {
        try
        {
            if (context.discard is not null)
            {
                return new ParameterDefinition
                {
                    Name = "_",
                    Type = new TypeName(null),
                    IsDiscarded = true,
                    ParseContext = context
                };
            }

            _currentParameter = new ParameterDefinition
            {
                Name = GetId(context.paramName),
                Type = context.typeName()?.GetText(),
                IsMarkedOptional = context.optionalModifier != null,
                DefaultValue = context.defaultValue?.GetText(),
                ParseContext = context
            };

            ProcessAttributes(_currentParameter.Attributes, context.attributes());

            return _currentParameter;
        }
        finally
        {
            _currentParameter = null;
        }
    }

    public override AstNode? VisitAttributeBlock(AttributeBlockContext context)
    {
        var previousAttributeTarget = _currentAttributeTarget;

        try
        {
            _currentAttributeTarget = AttributeTarget.Default;

            if (context.attributeTarget() is { } targetContext)
            {
                var targetText = targetContext.target.GetText();

                if (Enum.TryParse(targetText, true, out AttributeTarget target) && target != AttributeTarget.Default && string.Equals(targetText, targetText.ToLowerInvariant(), StringComparison.Ordinal))
                    _currentAttributeTarget = target;
                else
                    _contracts.AddError(targetContext, $"Invalid attribute target: '{targetText}'");
            }

            return base.VisitAttributeBlock(context);
        }
        finally
        {
            _currentAttributeTarget = previousAttributeTarget;
        }
    }

    public override AstNode? VisitCustomAttribute(CustomAttributeContext context)
    {
        if (_currentAttributeSet == null)
            return null;

        var attrParametersText = context.attributeParameters()?.GetFullText();

        var attr = new AttributeDefinition(context.attributeType?.GetText(), attrParametersText)
        {
            Target = _currentAttributeTarget,
            ParseContext = context
        };

        _currentAttributeSet.Add(attr);
        return attr;
    }

    public override AstNode? VisitExplicitTag(ExplicitTagContext context)
    {
        if (_currentParameter == null)
        {
            _contracts.AddError(context, "Tags can be defined only inside parameters");
            return null;
        }

        if (!int.TryParse(context.tagNumber?.Text, out var tag))
        {
            _contracts.AddError(context, $"Invalid tag value for parameter '{_currentParameter.Name}': {context.tagNumber?.Text}");
            return null;
        }

        if (!AstValidator.IsValidTag(tag))
        {
            _contracts.AddError(context, $"Tag for parameter '{_currentParameter.Name}' is not within the valid range ({context.tagNumber?.Text})");
            return null;
        }

        if (_currentParameter.Tag != 0)
        {
            _contracts.AddError(context, $"The parameter '{_currentParameter.Name}' already has an explicit tag ({_currentParameter.Tag})");
            return null;
        }

        _currentParameter.Tag = tag;
        return null;
    }

    public override AstNode? VisitTypeParamConstraintList(TypeParamConstraintListContext context)
    {
        foreach (var constraintContext in context.typeParamConstraint())
        {
            if (Visit(constraintContext) is GenericConstraint constraint)
                _currentMessage!.GenericConstraints.Add(constraint);
        }

        return null;
    }

    public override AstNode? VisitBaseTypeList(BaseTypeListContext context)
    {
        _currentMessage!.BaseTypes.AddRange(
            context.GetRuleContexts<TypeNameContext>()
                   .Select(typeContext => new TypeName(typeContext.GetText()))
        );

        return null;
    }

    public override AstNode? VisitTypeParamConstraint(TypeParamConstraintContext context)
    {
        var constraint = new GenericConstraint
        {
            GenericParameterName = GetId(context.name),
            ParseContext = context
        };

        foreach (var clause in context.typeParamConstraintClause())
        {
            switch (clause)
            {
                case TypeParamConstraintClauseClassContext:
                {
                    if (constraint.IsClass)
                        _contracts.AddError(clause, "Duplicate class constraint");

                    constraint.IsClass = true;
                    break;
                }

                case TypeParamConstraintClauseStructContext:
                {
                    if (constraint.IsStruct)
                        _contracts.AddError(clause, "Duplicate struct constraint");

                    constraint.IsStruct = true;
                    break;
                }

                case TypeParamConstraintClauseNewContext:
                {
                    if (constraint.HasDefaultConstructor)
                        _contracts.AddError(clause, "Duplicate new() constraint");

                    constraint.HasDefaultConstructor = true;
                    break;
                }

                case TypeParamConstraintClauseTypeContext typeClause:
                {
                    var typeName = new TypeName(typeClause.typeName()?.GetText());
                    if (!constraint.Types.Add(typeName))
                        _contracts.AddError(clause, $"Duplicate type constraint: '{typeName}'");
                    break;
                }
            }
        }

        return constraint;
    }

    private void ProcessMessage(MessageDefinition message, MessageDefinitionContext context)
    {
        try
        {
            _hasDefinitions = true;
            _currentMessage = message;
            _currentMessage.ParseContext = context;
            _currentMessage.Options = _currentMemberOptions;

            var nameContext = context.GetRuleContext<MessageNameContext>(0);
            message.Name = GetId(nameContext?.name);

            message.ContainingClasses.AddRange(
                nameContext?._containingTypes.Select(name => new TypeName(name.GetText())) ?? []
            );

            ProcessTypeModifiers(message, context.typeModifier().Select(i => i.type));

            foreach (var typeParamToken in nameContext?._typeParams ?? [])
            {
                var paramId = GetId(typeParamToken);

                if (message.GenericParameters.Contains(paramId))
                    _contracts.AddError(typeParamToken, $"Duplicate generic parameter: '{paramId}'");

                message.GenericParameters.Add(paramId);
            }

            ProcessAttributes(message.Attributes, context.GetRuleContext<AttributesContext>(0));
            Visit(context.GetRuleContext<ParameterListContext>(0));
            Visit(context.GetRuleContext<BaseTypeListContext>(0));
            Visit(context.GetRuleContext<TypeParamConstraintListContext>(0));

            _contracts.Messages.Add(_currentMessage);
        }
        finally
        {
            _currentMessage = null;
        }
    }

    private void ProcessTypeModifiers(IMemberNode member, IEnumerable<IToken> modifiers)
    {
        AccessModifier? accessModifier = null;
        InheritanceModifier? inheritanceModifier = null;

        foreach (var modifier in modifiers)
        {
            switch (modifier.Type)
            {
                case MessageContractsLexer.KW_PUBLIC:
                case MessageContractsLexer.KW_INTERNAL:
                    if (accessModifier == null)
                    {
                        accessModifier = modifier.Type switch
                        {
                            MessageContractsLexer.KW_PUBLIC   => AccessModifier.Public,
                            MessageContractsLexer.KW_INTERNAL => AccessModifier.Internal,
                            _                                 => throw new InvalidOperationException($"Cannot map access modifier: {modifier.Text}")
                        };
                    }
                    else
                    {
                        _contracts.AddError(modifier, "An access modifier has already been provided");
                    }

                    break;

                case MessageContractsLexer.KW_SEALED:
                case MessageContractsLexer.KW_ABSTRACT:
                    if (inheritanceModifier == null)
                    {
                        if (member is not IClassNode)
                            _contracts.AddError(modifier, "Cannot apply inheritance modifier to a non-class type");

                        inheritanceModifier = modifier.Type switch
                        {
                            MessageContractsLexer.KW_SEALED   => InheritanceModifier.Sealed,
                            MessageContractsLexer.KW_ABSTRACT => InheritanceModifier.Abstract,
                            _                                 => throw new InvalidOperationException($"Cannot map inheritance modifier: {modifier.Text}")
                        };
                    }
                    else
                    {
                        _contracts.AddError(modifier, "An inheritance modifier has already been provided");
                    }

                    break;
            }
        }

        member.AccessModifier = accessModifier ?? member.Options.GetAccessModifier();

        if (inheritanceModifier != null && member is IClassNode classNode)
            classNode.InheritanceModifier = inheritanceModifier.Value;
    }

    private void ProcessAttributes(AttributeSet? attributeSet, AttributesContext? context)
    {
        if (context == null || attributeSet == null)
            return;

        var previousAttributeSet = _currentAttributeSet;

        try
        {
            _currentAttributeSet = attributeSet;
            _currentAttributeSet.ParseContext = context;

            Visit(context);
        }
        finally
        {
            _currentAttributeSet = previousAttributeSet;
        }
    }

    private string GetId(IdContext? context)
        => context?.GetValidatedId(_contracts) ?? string.Empty;

    private new AstNode? Visit(IParseTree? tree)
        => tree is not null ? base.Visit(tree) : null;
}
