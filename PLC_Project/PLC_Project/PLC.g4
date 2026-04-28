grammar PLC;

program: statement+ ;

statement
    : primitiveType IDENTIFIER (',' IDENTIFIER)* ';'          # declaration
    | 'if' '(' expr ')' statement ('else' statement)?         # ifStatement
    | 'while' '(' expr ')' statement                          # whileStatement
    | 'read' IDENTIFIER (',' IDENTIFIER)* ';'                 # readStatement
    | 'write' expr (',' expr)* ';'                            # writeStatement
    | '{' statement* '}'                                       # block
    | expr ';'                                                 # exprStatement
    | ';'                                                      # emptyStatement
    ;

// Alternatives ordered HIGHEST to LOWEST precedence (ANTLR4: first = highest)
expr
    : '!' expr                                                # not
    | '-' expr                                                # unaryMinus
    | expr op=('*'|'/'|'%') expr                              # mulDivMod
    | expr op=('+'|'-'|'.') expr                              # addSubConcat
    | expr op=('<'|'>') expr                                  # relational
    | expr op=('=='|'!=') expr                                # equality
    | expr '&&' expr                                          # and
    | expr '||' expr                                          # or
    | <assoc=right> IDENTIFIER '=' expr                       # assignment
    | '(' expr ')'                                            # parens
    | INT                                                     # int
    | FLOAT                                                    # float
    | BOOL                                                     # bool
    | STRING                                                   # string
    | IDENTIFIER                                              # id
    ;

primitiveType
    : 'int'
    | 'float'
    | 'bool'
    | 'string'
    | 'String'
    ;

BOOL         : 'true' | 'false' ;
STRING       : '"' (~["\\\r\n] | '\\' .)* '"' ;
FLOAT        : [0-9]+ '.' [0-9]+ ;
INT          : [0-9]+ ;
IDENTIFIER   : [a-zA-Z][a-zA-Z0-9]* ;
LINE_COMMENT : '//' ~[\r\n]* -> skip ;
WS           : [ \t\r\n]+ -> skip ;
