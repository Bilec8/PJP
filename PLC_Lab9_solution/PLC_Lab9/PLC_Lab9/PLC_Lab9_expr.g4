grammar PLC_Lab9_expr;

/** The start rule; begin parsing here. */
program: statement+ ;

statement
    : primitiveType IDENTIFIER (',' IDENTIFIER)* ';' # declaration
    | expr ';'                                       # printExpr
    ;

expr: expr op=(MUL|DIV | MOD) expr          # mulDivMod
    | expr op=(ADD|SUB) expr                # addSub
    | INT                                   # int
    | IDENTIFIER                            # id
    | FLOAT                                 # float
    | '(' expr ')'                          # parens
    | <assoc=right> IDENTIFIER '=' expr     # assignment
    ;

primitiveType
    : type=INT_KEYWORD
    | type=FLOAT_KEYWORD
    ;


INT_KEYWORD : 'int';
FLOAT_KEYWORD : 'float';
SEMI:               ';';
COMMA:              ',';
MUL : '*' ; 
DIV : '/' ;
ADD : '+' ;
SUB : '-' ;
MOD : '%' ;
IDENTIFIER : [a-zA-Z]+ ; 
FLOAT : [0-9]+'.'[0-9]+ ;
INT : [0-9]+ ; 
WS : [ \t\r\n]+ -> skip ; // toss out whitespace