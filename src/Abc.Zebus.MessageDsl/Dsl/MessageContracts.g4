grammar MessageContracts;

// --- PARSER ---

compileUnit
	:	(definition separator)* EOF
	;

definition
	:	optionDefinition
	|	usingDefinition
	|	typeDefinition
	|	enumDefinition
	|	SEP
	;

optionDefinition
	:	{ IsAtStartOfPragma() }?
		'#' ID { !IsAtEndOfLine() }? pragmaDefinition endOfLine
	;

pragmaDefinition
	:	name=ID
	|	not='!' { !IsAtEndOfLine() }? name=ID
	|	name=ID { !IsAtEndOfLine() }? '='? valueTokens+=pragmaValueToken*
	;

pragmaValueToken
	:	{ !IsAtEndOfLine() }? token=.
	;

usingDefinition
	:	'using' namespace
	;

enumDefinition
	:	attributes 'public'? 'enum' name=id (':' underlyingType=typeName)? '{' (enumMember (',' enumMember)* ','?)? '}'
	;

enumMember
	:	attributes name=id ('=' value=enumValue)?
	;

enumValue
	:	enumValueAtom ( enumValueBinaryOp enumValue )*
	|	'~' enumValue
	|   '(' enumValue ')'
	;

enumValueBinaryOp
	:   '|' | '&' | '^' | binaryShiftOp
	;

binaryShiftOp
	:	{ AreTwoNextTokensConsecutive() }? ( '<' '<' | '>' '>' )
	;

enumValueAtom
	:   id
	|   NUMBER
	;

typeDefinition
	:	attributes messageName customModifier='!'?               parameterList interfaceList typeParamConstraintList  # messageDefinition
	;

messageName
	:	name=id ('<' typeParams+=id (',' typeParams+=id)* '>')?
	;

interfaceList
	:	(':' typeName (',' typeName)*)?
	;

parameterList
	:	'(' (parameter (',' parameter)*)? ')'
	;
	
parameter
	:	attributes typeName paramName=id optionalModifier='?'? ('=' defaultValue=literalValue)?   # parameterDefinition
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
	|	typeNameBase '?'
	|	typeName '[' ','* ']'
	;

typeNameBase
	:	(namespace '.')? id
	|	typeNameBase '<' typeName (',' typeName)* '>'
	;

literalValue
	:	'true'
	|	'false'
	|	'null'
	|	STRING
	|	CHAR
	|	NUMBER
	|	'typeof' '(' typeName ')'
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
	:	escape='@'? name=ID { IsValidIdEscape($ctx.escape, $ctx.name) }?
	|	escape='@'? name='where' { IsValidIdEscape($ctx.escape, $ctx.name) }?                                      // Contextual keywords
	|	escape='@'  name=('true' | 'false' | 'null' | 'typeof' | 'class' | 'struct' | 'new' | 'using' | 'public')  // Keywords
		{ IsValidIdEscape($ctx.escape, $ctx.name) }?     
	;

// --- LEXER ---

ID
	:	[a-zA-Z_][a-zA-Z0-9_]*
	;

STRING
	:	'"' (UNICODE_ESCAPE | '\\' ~[\r\n] | ~["\\\r\n])* '"'
	|	'@"' ('""' | ~'"')* '"'
	;

CHAR
	:	'\'' (UNICODE_ESCAPE | '\\' ~[\r\n] | ~[\'\\\r\n]) '\''
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
