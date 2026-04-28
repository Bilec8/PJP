# PLC Project — Brief for Claude Code

## Context

This is a **semester project for the "Programming Languages and Compilers" (PLC)** course at VŠB-TUO, taught by Marek Běhálek. The student (Šimon) missed several lab sessions and needs to complete the full project. Official assignment: <http://behalek.cs.vsb.cz/wiki/index.php/PLC_Project>

The project is a **compiler for a small imperative language**, written in **C# (.NET) using ANTLR4**, with four phases:

1. **Parsing** (ANTLR-generated lexer + parser from a `.g4` grammar)
2. **Type checking** (walk the parse tree, verify types, report errors)
3. **Code generation** (walk the parse tree, emit stack-based instructions to a text file)
4. **Interpretation** (a separate VM reads the instruction file and executes it on a simulated stack)

## What the student already has

The student has **lab solutions** from Běhálek (Labs 8, 9, 10) which together cover ~30–40% of the project. These are in `original_labs/` and **must not be modified** — they are reference material. Specifically:

- `original_labs/Lab8/` — ANTLR parser + type checker for a *very* limited language: only `int`/`float` types, `+ - * / %` operators, `=` assignment, declarations, expression statements. **No code generation.**
- `original_labs/Lab9/` — Same as Lab 8 + **code generation** (both Listener and Visitor variants). Generates UPPERCASE stack instructions like `PUSH I 5`, `ADD`, `SAVE I a`.
- `original_labs/Lab10/` — A VM (`VirtualMachine.cs`) that reads UPPERCASE stack instructions and runs them. Supports only `int`/`float` and basic arithmetic (`PUSH`, `ADD`, `SUB`, `MUL`, `DIV`, `MOD`, `LOAD`, `SAVE`, `PRINT`).

## What needs to be built

A **single C# project** named `PLC_Project` (or similar) which:

- Takes a path to a source file (the language defined below) as a CLI argument
- Outputs the generated stack-based instructions
- Includes a working VM that can execute those instructions

The project is essentially **Lab 9 extended** with the full language spec, plus **Lab 10 extended** with the full instruction set, glued together.

## Language specification (from the wiki, verbatim-ish)

### Program format

- Sequence of statements
- Free formatting; whitespace, tabs, newlines are delimiters only
- Comments: `// to end of line`
- Keywords are reserved, identifiers are case-sensitive

### Literals

- `int` — sequence of digits
- `float` — sequence of digits containing `.`
- `bool` — `true` / `false`
- `string` — `"text"` in double quotes; escape sequences optional

### Variables

- Identifier: starts with a letter, followed by letters/digits
- Must be declared before use; redeclaration is an error
- Types: `int`, `float`, `bool`, `string`
- Initial values: `0`, `0.0`, `false`, `""`

### Statements

- `;` — empty statement
- `type variable, variable, ... ;` — declaration; type is one of `int`, `float`, `bool`, `string` (note: wiki spells it `String` once with capital — treat lowercase `string` as canonical based on the literal type table)
- `expression ;` — evaluate, discard result (assignments via this)
- `read variable, variable, ... ;` — read values from stdin into variables (one per line)
- `write expression, expression, ... ;` — print values to stdout, with `\n` after the last
- `{ statement statement ... }` — block
- `if (condition) statement [else statement]` — `condition` must be `bool`
- `while (condition) statement` — `condition` must be `bool`

### Expressions

Operator priority **lowest to highest**:

1. `=` (right-associative)
2. `||`
3. `&&`
4. `== !=`
5. `< >`
6. `+ - .`
7. `* / %`
8. `!`
9. unary `-`

All others are **left-associative**.

#### Operator signatures

| Description | Operator | Signature |
| --- | --- | --- |
| unary minus | `-` | `I → I` ∨ `F → F` |
| binary arithmetic | `+ - * /` | `I × I → I` ∨ `F × F → F` |
| modulo | `%` | `I × I → I` |
| string concat | `.` | `S × S → S` |
| relational | `< >` | `x × x → B` for `x ∈ {I, F}` |
| comparison | `== !=` | `x × x → B` for `x ∈ {I, F, S}` |
| logical and/or | `&& \|\|` | `B × B → B` |
| logical not | `!` | `B → B` |
| assignment | `=` | `x × x → x` for `x ∈ {I, F, S, B}` |

#### Type rules

- `int` is **automatically promoted to `float`** when needed (e.g. `5 + 5.5` is float; `5` becomes `5.0`)
- **No conversion from float to int**, ever — even in assignment
- In assignment `=`: left side is strictly a variable, right side is an expression; the variable's declared type wins, so e.g. assigning a `float` expression to an `int` variable is a type error
- `==` and `!=` work on `int`, `float`, `string` (not `bool` per the spec — but mixed `int`/`float` should be allowed via promotion)

#### Side effect of assignment

`=` is an expression — it returns the assigned value. So `a = b = 15` works and both vars become 15. Lab 9's existing code-gen handles this with a `SAVE` followed by `LOAD` to leave the value on the stack.

### Sample programs

Reference inputs/outputs (download these into `samples/`):

- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_t1.in>
- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_t1.out>
- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_t2.in>
- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_t2.out>
- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_t3.in>
- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_t3.out>
- <http://linedu.vsb.cz/~beh01/wiki_data/PLC_errors.in>

The `.out` files are the **expected generated instruction code**, not the program output. These are the ground truth — match them as closely as the spec allows. (Some superficial differences like extra blank lines might be OK; verify with the student.)

## Stack-based instruction set (target language)

**Important:** the wiki uses **lowercase** instructions (`push I 5`, `add I`, `load`, `save`, `label 1`, `jmp 1`, …) **but the lab solutions use UPPERCASE** (`PUSH I 5`, `ADD`, `LOAD`, …). Pick one and be consistent across code generator and VM.

**Recommendation: use lowercase to match the wiki specification** — that's what Běhálek will compare against. If the sample `.out` files use lowercase, this is confirmed.

| Instruction | Description |
| --- | --- |
| `add T` | binary `+`, two values of type `T` (`I` or `F`) |
| `sub T` | binary `-`, two values of type `T` (`I` or `F`) |
| `mul T` | binary `*`, two values of type `T` (`I` or `F`) |
| `div T` | binary `/`, two values of type `T` (`I` or `F`) |
| `mod` | binary `%`, two ints |
| `uminus T` | unary `-`, one value of type `T` (`I` or `F`) |
| `concat` | binary `.` for strings |
| `and` | binary `&&`, two bools |
| `or` | binary `\|\|`, two bools |
| `gt T` | `>`, two values of type `T` (`I` or `F`) |
| `lt T` | `<`, two values of type `T` (`I` or `F`) |
| `eq T` | `==`, two values of type `T` (`I`, `F`, or `S`) |
| `not` | unary `!`, one bool |
| `itof` | pop int, push float |
| `push T x` | push value `x` of type `T` (`I`, `F`, `S`, `B`); strings as `push S "text"` |
| `pop` | discard top of stack |
| `load id` | push variable `id` |
| `save id` | pop and store into variable `id` |
| `label n` | mark with unique number `n` |
| `jmp n` | unconditional jump to label `n` |
| `fjmp n` | pop bool; if false, jump to label `n` |
| `print n` | pop `n` values and print them to stdout |
| `read T` | read value of type `T` from stdin, push it |

**Note on `!=`:** there's no `neq` instruction. Generate `eq T` followed by `not`.

## Recommended project structure

```
PLC_Project/
├── PLC_Project.sln
├── PLC_Project/
│   ├── PLC_Project.csproj
│   ├── Program.cs              # Entry point: parse args, run pipeline
│   ├── Grammar/
│   │   └── PLC.g4              # ANTLR grammar
│   ├── Compiler/
│   │   ├── Type.cs             # enum: Int, Float, Bool, String, Error
│   │   ├── SymbolTable.cs
│   │   ├── Errors.cs
│   │   ├── VerboseErrorListener.cs
│   │   ├── TypeCheckingListener.cs
│   │   └── CodeGeneratorListener.cs
│   └── VM/
│       └── VirtualMachine.cs   # The interpreter from Lab 10, extended
├── samples/                    # PLC_t1.in, PLC_t1.out, ...
└── README.md
```

CLI usage:
```
PLC_Project --input path/to/source.plc                  # compile, print instructions to stdout
PLC_Project --input source.plc --output instructions.txt # write instructions to file
PLC_Project --run instructions.txt                       # run pre-generated instructions in VM
PLC_Project --input source.plc --run                     # compile and immediately run
```

The exact CLI shape can be simpler if it's easier — Běhálek typically wants a single argument pointing to the source file. Keep it ergonomic for the student to test.

## Implementation plan (incremental)

Do these phases in order. After each phase, build the project (`dotnet build`) and check it compiles. After phases 3, 5, 7, run against `samples/` to verify.

### Phase 1: Project scaffold
- Create solution and project (target `.NET 8.0` to match modern setup, unless `csproj` from labs uses something different — check)
- Add ANTLR4 NuGet packages: `Antlr4.Runtime.Standard` and `Antlr4BuildTasks` (for build-time grammar generation)
- Set up grammar build target so `.g4` files generate parser/lexer at build time
- Copy `Lab9_expr.g4`, `TypeCheckingListener.cs`, `CodeGeneratorListener.cs`, `SymbolTable.cs`, `Type.cs`, `Errors.cs`, `VerboseErrorListener.cs`, `Program.cs` from `original_labs/Lab9/` and `VirtualMachine.cs` from `original_labs/Lab10/`. Rename namespace to `PLC_Project`. Rename grammar to `PLC.g4`. Update parser/lexer class references.
- Verify it builds and runs the original Lab 9 input.

### Phase 2: Sync instruction case
- Either change Lab 9's code-gen to lowercase OR change Lab 10's VM to UPPERCASE — be consistent
- **Read one of the sample `.out` files first to confirm which case is correct**

### Phase 3: Extend types
- Add `Bool`, `String` to `Type` enum
- Extend `primitiveType` rule in grammar to include `bool` and `string`
- Add `BOOL_KEYWORD` and `STRING_KEYWORD` tokens
- Add literal alternatives in `expr`: `bool` (`true`/`false`), `string` (`"..."`)
- Update `TypeCheckingListener` to handle the new primitive types in declarations
- Update `CodeGeneratorListener` to push correct type tags (`B`, `S`) and initial values
- Test: declare bool/string variables, declare and use them in simple expressions

### Phase 4: New operators
For each, update grammar (with correct precedence!), type checker, code generator:
- Unary minus `-`, logical not `!` (highest precedence)
- Relational `< >` (signature `x × x → B`)
- Equality `== !=` (note: `!=` generates `eq` + `not`)
- Logical `&&`, `||`
- String concat `.`
- Int-to-float promotion: when one operand is `float` and other is `int`, emit `itof` for the int operand
- Update VM to support all new ops, including `bool` and `string` on stack

### Phase 5: Statements (excluding control flow)
- Empty `;`
- `read variable, variable, ... ;` — emits `read T` for each variable's type, then `save id` for each
- `write expression, expression, ... ;` — emits all expression code, then `print N`
- Block `{ statements }`
- Update VM: `read T`, `print n`, `pop`

### Phase 6: Control flow (the hardest part)
- `if (cond) stmt [else stmt]`:
  - emit cond code → `fjmp ELSE` → then-stmt → `jmp END` → `label ELSE` → else-stmt (or nothing) → `label END`
- `while (cond) stmt`:
  - emit `label START` → cond code → `fjmp END` → body → `jmp START` → `label END`
- Need a label counter (e.g. `private int _nextLabel = 0;` in code generator)
- **Key issue with listener pattern:** in pure listener (`enter`/`exit`), you don't know the label number when you enter `if` because the children haven't been walked yet. **Solution:** allocate labels in `EnterIfStatement` and stash them on the context (e.g. `ParseTreeProperty<int>`), or — easier — **switch from listener to visitor** for code generation, so you control traversal explicitly. Lab 9 already provides a visitor scaffold. **Recommendation: use the visitor for code generation; listener works fine for type checking.**
- Update VM: `jmp`, `fjmp`, `label`. Pre-scan code on load to build a `Dictionary<int, int>` mapping label → instruction index.

### Phase 7: Polish
- Comments `// ...` in grammar (skip)
- Fix `IDENTIFIER` to allow digits after first letter: `[a-zA-Z][a-zA-Z0-9]*`
- Error reporting: line/column info, all errors collected before exit
- Test full pipeline against `samples/`

### Phase 8: Verify
- Run `samples/PLC_t1.in` through the compiler, diff with `PLC_t1.out`
- Same for t2, t3
- Run `PLC_errors.in` and check the compiler reports errors instead of crashing
- Run generated instructions through VM with reasonable test input

## Working principles

- **Build often.** After every meaningful change, run `dotnet build` and address compile errors before adding more.
- **Test against samples after each phase.** Don't accumulate untested changes.
- **Don't modify `original_labs/`.** Treat as read-only reference.
- **When unsure about something the spec doesn't pin down**, prefer matching what the sample `.out` files do.
- **Comment intentions, not mechanics.** "Promote int to float for mixed arithmetic" not "emit itof".
- **Listener vs visitor:** type checking → listener (post-order is natural). Code gen → visitor for control flow, otherwise listener is fine.
- **Don't over-engineer.** This is a course project, not production code. The goal is correctness on sample inputs and code Šimon can defend orally.
- **Czech communication preferred.** Šimon's primary language is Czech; respond in Czech unless he switches to English. Code/comments in English is fine.

## Communication with the student

Šimon prefers direct, grounded communication. He's a competent fullstack developer (.NET / Vue / Python) who missed lectures, not someone learning to program. **Don't over-explain basics**. Focus explanations on the compiler-specific concepts (ANTLR parse tree walking, FIRST/FOLLOW intuition, listener vs visitor, label-based control flow generation, stack-based code shape).

When you make decisions on his behalf (e.g. picking visitor over listener for codegen), state the decision and the reason briefly — don't ask him to choose between two equally fine options unless they have meaningful consequences.

Pause and ask before:
- Adding NuGet packages beyond ANTLR
- Diverging from the wiki spec
- Sample `.out` files disagreeing with what your generated code produces (he should see the diff and decide)
