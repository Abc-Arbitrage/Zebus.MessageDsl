grammar MessageContracts;

// --- PARSER ---

compileUnit
	:	(definition separator)* EOF
	;

definition
	:	optionDefinition
	|	usingDefinition
	|	namespaceDefinition
	|	messageDefinition
	|	enumDefinition
	|	SEP
	;

optionDefinition
	:	{ IsAtStartOfPragma() }?
		'#' ID { !IsAtEndOfLine() }? pragmaDefinition endOfLine
	;

pragmaDefinition
	:	name=pragmaValueToken
	|	not='!' name=pragmaValueToken
	|	name=pragmaValueToken { !IsAtEndOfLine() }? '='? valueTokens+=pragmaValueToken*
	;

pragmaValueToken
	:	{ !IsAtEndOfLine() }? token=.
	;

usingDefinition
	:	'using' namespace
	;

namespaceDefinition
	:	'namespace' name=namespaceBase
	;

enumDefinition
	:	attributes accessModifier? 'enum' name=id (':' underlyingType=typeName)? '{' (enumMember (',' enumMember)* ','?)? '}'
	;

enumMember
	:	attributes name=id ('=' value=enumValue)?
	;

enumValue
	:	enumValueAtom ( enumValueBinaryOp enumValue )*
	|	'~' enumValue
	|	'(' enumValue ')'
	;

enumValueBinaryOp
	:	'|' | '&' | '^' | binaryShiftOp
	;

binaryShiftOp
	:	{ AreTwoNextTokensConsecutive() }? ( '<' '<' | '>' '>' )
	;

enumValueAtom
	:	id
	|	NUMBER
	;

messageDefinition
	:	attributes typeModifier* messageName customModifier='!'? parameterList baseTypeList typeParamConstraintList
	;

accessModifier
	:	type=KW_PUBLIC
	|	type=KW_INTERNAL
	;

typeModifier
	:	type=KW_PUBLIC
	|	type=KW_INTERNAL
	|	type=KW_SEALED
	|	type=KW_ABSTRACT
	;

messageName
	:	(containingTypes+=id '.')* name=id ('<' typeParams+=id (',' typeParams+=id)* '>')?
	;

baseTypeList
	:	(':' typeName (',' typeName)*)?
	;

parameterList
	:	'(' (parameterDefinition (',' parameterDefinition)*)? ')'
	;

parameterDefinition
	:	attributes typeName paramName=id optionalModifier='?'? ('=' defaultValue=literalValue)?
	;

typeParamConstraintList
	:	typeParamConstraint*
	;

typeParamConstraint
	:	whereKw='where' name=id ':' typeParamConstraintClause ( ',' typeParamConstraintClause )*
	;

typeParamConstraintClause
	:	typeName            # typeParamConstraintClauseType
	|	'class'             # typeParamConstraintClauseClass
	|	'struct'            # typeParamConstraintClauseStruct
	|	'new' '(' ')'       # typeParamConstraintClauseNew
	;

namespace
	:	namespaceBase
	|	id '::' namespaceBase
	;

namespaceBase
	:	id ( '.' id )*
	;

typeName
	:	typeNameBase
	|	typeName '?'
	|	typeName '[' ','* ']'
	|	typeNameBase '<' typeName (',' typeName)* '>'
	;

typeNameBase
	:	(namespace '.')? id
	|	typeKeyword
	;

literalValue
	:	'true'
	|	'false'
	|	'null'
	|	STRING
	|	CHAR
	|	NUMBER
	|	'typeof' '(' typeName ')'
	|	'default' ( '(' typeName ')' )?
	|	id ('.' id)+ // enum value
	;

attributes
	:	('[' attribute (',' attribute)* ']')*
	;

attribute
	:	attributeType=typeName                                    # customAttribute
	|	attributeType=typeName '(' attributeParameters ')'        # customAttribute
	|	tagNumber=NUMBER                                          # explicitTag
	;

attributeParameters
	:	(literalValue (',' literalValue)*)?
	|	attributeNamedParameter (',' attributeNamedParameter)*
	|	literalValue (',' literalValue)* ',' attributeNamedParameter (',' attributeNamedParameter)*
	;

attributeNamedParameter
	:	parameterName=id '=' literalValue
	;

separator
	:	';'
	|	{ IsAtImplicitSeparator() }?
	;

endOfLine
	:	{ IsAtEndOfLine() }?
	;

id
	:	escape='@'? nameId=ID { IsValidIdEscape($ctx.escape, $ctx.nameId) }?
	|	escape='@'? nameCtxKw=contextualKeyword { IsValidIdEscape($ctx.escape, $ctx.nameCtxKw) }?
	|	escape='@'  nameKw=keyword { IsValidIdEscape($ctx.escape, $ctx.nameKw) }?
	;

keyword
	:	'abstract' | 'as' | 'base' | 'bool'
	|	'break' | 'byte' | 'case' | 'catch'
	|	'char' | 'checked' | 'class' | 'const'
	|	'continue' | 'decimal' | 'default' | 'delegate'
	|	'do' | 'double' | 'else' | 'enum'
	|	'event' | 'explicit' | 'extern' | 'false'
	|	'finally' | 'fixed' | 'float' | 'for'
	|	'foreach' | 'goto' | 'if' | 'implicit'
	|	'in' | 'int' | 'interface' | 'internal'
	|	'is' | 'lock' | 'long' | 'namespace'
	|	'new' | 'null' | 'object' | 'operator'
	|	'out' | 'override' | 'params' | 'private'
	|	'protected' | 'public' | 'readonly' | 'ref'
	|	'return' | 'sbyte' | 'sealed' | 'short'
	|	'sizeof' | 'stackalloc' | 'static' | 'string'
	|	'struct' | 'switch' | 'this' | 'throw'
	|	'true' | 'try' | 'typeof' | 'uint'
	|	'ulong' | 'unchecked' | 'unsafe' | 'ushort'
	|	'using' | 'virtual' | 'void' | 'volatile'
	|	'while'
	;

contextualKeyword
	:	'add' | 'alias' | 'ascending' | 'async'
	|	'await' | 'by' | 'descending' | 'dynamic'
	|	'equals' | 'from' | 'get' | 'global'
	|	'group' | 'into' | 'join' | 'let'
	|	'nameof' | 'on' | 'orderby' | 'partial'
	|	'remove' | 'select' | 'set' | 'value'
	|	'var' | 'when' | 'where' | 'yield'
	;

typeKeyword
	:	'bool'
	|	'byte'
	|	'char'
	|	'decimal'
	|	'double'
	|	'float'
	|	'int'
	|	'long'
	|	'object'
	|	'sbyte'
	|	'short'
	|	'string'
	|	'uint'
	|	'ulong'
	|	'ushort'
	;

// --- LEXER ---

KW_PUBLIC : 'public';
KW_INTERNAL : 'internal';

KW_SEALED : 'sealed';
KW_ABSTRACT : 'abstract';

ID
	:	[a-zA-Z_][a-zA-Z0-9_]*
	;

STRING
	:	'"' (UNICODE_ESCAPE | '\\' ~[\r\n] | ~["\\\r\n])* '"'
	|	'@"' ('""' | ~'"')* '"'
	;

CHAR
	:	'\'' (UNICODE_ESCAPE | '\\' ~[\r\n] | ~['\\\r\n]) '\''
	;

NUMBER
	:	[-+]? [0-9]+ ('.' [0-9]+)? ([eE] [0-9]+)? ([UuLlFfDdMm]|[Uu][Ll])?
	|	'0' [xX] [0-9a-fA-F]+
	;

SEP
	:	';'
	;

fragment HEX
	:	[0-9A-Fa-f]
	;

fragment UNICODE_ESCAPE
	:	'\\u' HEX HEX HEX HEX
	|	'\\U' HEX HEX HEX HEX HEX HEX HEX HEX
	;

// --- LEXER - Ignored ---

COMMENT_LINE
	:	'//' .*? ('\n'|EOF) -> channel(HIDDEN)
	;

COMMENT_INLINE
	:	'/*' .*? '*/' -> channel(HIDDEN)
	;

WHITESPACE
	:	[ \t\r\n]+ -> channel(HIDDEN)
	;

UNKNOWN_CHAR
	:	.
	;
